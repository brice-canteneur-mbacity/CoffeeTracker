using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

public class SyncService(IJSRuntime js, CoffeeDb db)
{
    private readonly IJSRuntime _js = js;
    private readonly CoffeeDb _db = db;
    private readonly HttpClient _http = new();

    private const string PatKey = "coffee.sync.pat";
    private const string GistIdKey = "coffee.sync.gistId";
    private const string LastSyncKey = "coffee.sync.lastSync";
    private const string GistFilename = "coffee-tracker.json";
    private const string GistDescription = "Coffee Tracker — données perso (sauvegarde sync)";

    public string? Pat { get; private set; }
    public string? GistId { get; private set; }
    public DateTime? LastSync { get; private set; }
    public bool IsConfigured => !string.IsNullOrEmpty(Pat);
    public bool IsBusy { get; private set; }
    public string? LastError { get; private set; }
    public string? LastInfo { get; private set; }

    public event Action? StateChanged;

    private static readonly JsonSerializerOptions BackupOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ImportOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task InitializeAsync()
    {
        Pat = await GetItem(PatKey);
        GistId = await GetItem(GistIdKey);
        var lastStr = await GetItem(LastSyncKey);
        if (DateTime.TryParse(lastStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var ls))
            LastSync = ls;
        StateChanged?.Invoke();

        // Aucune synchro automatique au démarrage : ni pull (qui pourrait modifier le local),
        // ni découverte réseau du Gist. Tout passe par le bouton « Synchroniser » (manuel),
        // qui retrouve/crée le Gist au besoin via SyncAsync.
    }

    /// <summary>
    /// Liste les gists du compte et retourne l'ID du premier qui contient notre fichier.
    /// </summary>
    private async Task<string?> FindExistingGistAsync()
    {
        if (!IsConfigured) return null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/gists?per_page=100");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Pat);
            request.Headers.UserAgent.ParseAdd("CoffeeTracker");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            var body = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<GistResponse>>(body, ImportOptions);
            // On prend le plus récemment mis à jour parmi ceux qui contiennent notre fichier
            var match = list?
                .Where(g => g.Files != null && g.Files.ContainsKey(GistFilename))
                .OrderByDescending(g => g.UpdatedAt)
                .FirstOrDefault();
            return match?.Id;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetPatAsync(string? pat)
    {
        Pat = string.IsNullOrWhiteSpace(pat) ? null : pat.Trim();
        if (Pat is null)
        {
            await RemoveItem(PatKey);
        }
        else
        {
            await SetItem(PatKey, Pat);
        }
        StateChanged?.Invoke();
    }

    public async Task DisconnectAsync()
    {
        Pat = null;
        GistId = null;
        LastSync = null;
        await RemoveItem(PatKey);
        await RemoveItem(GistIdKey);
        await RemoveItem(LastSyncKey);
        StateChanged?.Invoke();
    }

    /// <summary>Push : sérialise toutes les données et update/crée le Gist.</summary>
    public async Task<bool> PushAsync()
    {
        if (!IsConfigured) return false;
        IsBusy = true;
        LastError = null;
        LastInfo = null;
        StateChanged?.Invoke();
        try
        {
            var backup = await BuildBackupAsync();
            var content = JsonSerializer.Serialize(backup, BackupOptions);

            var fileObj = new Dictionary<string, object>
            {
                [GistFilename] = new { content }
            };
            var payload = new Dictionary<string, object>
            {
                ["description"] = GistDescription,
                ["files"] = fileObj
            };

            HttpRequestMessage request;
            if (string.IsNullOrEmpty(GistId))
            {
                payload["public"] = false;
                request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/gists");
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Patch, $"https://api.github.com/gists/{GistId}");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Pat);
            request.Headers.UserAgent.ParseAdd("CoffeeTracker");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var gist = JsonSerializer.Deserialize<GistResponse>(body, ImportOptions);
            if (gist?.Id is not null && gist.Id != GistId)
            {
                GistId = gist.Id;
                await SetItem(GistIdKey, GistId);
            }

            LastSync = DateTime.UtcNow;
            await SetItem(LastSyncKey, LastSync.Value.ToString("O"));
            LastInfo = $"Push OK ({backup.Coffees?.Count ?? 0} cafés, {backup.Brews?.Count ?? 0} brews, {backup.Shops?.Count ?? 0} shops, {backup.ShopVisits?.Count ?? 0} visites, {backup.Machines?.Count ?? 0} machines)";
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Push : {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke();
        }
    }

    /// <summary>Pull : récupère le Gist et écrase les données locales.
    /// Si le Gist stocké localement n'existe plus, on cherche automatiquement un autre Gist
    /// du compte avant d'abandonner.</summary>
    public async Task<bool> PullAsync()
    {
        if (!IsConfigured) return false;
        if (string.IsNullOrEmpty(GistId)) return false;

        IsBusy = true;
        LastError = null;
        LastInfo = null;
        StateChanged?.Invoke();
        try
        {
            var data = await TryFetchGistAsync(GistId);

            if (data is null)
            {
                // Gist actuel inaccessible (404, supprimé, ou PAT changé) :
                // on tente la découverte automatique d'un autre Gist du compte.
                var previousId = GistId;
                GistId = null;
                await RemoveItem(GistIdKey);

                var found = await FindExistingGistAsync();
                if (!string.IsNullOrEmpty(found) && found != previousId)
                {
                    GistId = found;
                    await SetItem(GistIdKey, found);
                    data = await TryFetchGistAsync(found);
                }
            }

            if (data is null)
            {
                LastError = "Aucun Gist exploitable — un nouveau sera créé au prochain push.";
                return false;
            }

            // Garde anti-perte : on ne laisse JAMAIS une sauvegarde distante vide écraser des
            // données locales. Si le distant est vide alors qu'on a des données ici, on ignore le
            // pull (le prochain push restaurera le cloud à partir du local).
            var local = await BuildBackupAsync();
            var localCount = CountRecords(local);
            if (CountRecords(data) == 0 && localCount > 0)
            {
                LastInfo = "Sauvegarde distante vide ignorée — données locales préservées.";
                return true;
            }

            // Cliché de sécurité du local AVANT d'appliquer le distant (réutilise le backup déjà construit).
            await CaptureSnapshotAsync("avant pull", local);

            var merged = await MergeBackupAsync(data);

            LastSync = DateTime.UtcNow;
            await SetItem(LastSyncKey, LastSync.Value.ToString("O"));
            LastInfo = $"Pull OK ({merged} enreg. fusionnés sans perte ; local : {localCount})";
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Pull : {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke();
        }
    }

    /// <summary>Tente de récupérer le contenu d'un Gist. Retourne null si 404 / vide / erreur réseau.</summary>
    private async Task<CoffeeBackup?> TryFetchGistAsync(string id)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/gists/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Pat);
            request.Headers.UserAgent.ParseAdd("CoffeeTracker");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            var response = await _http.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync();
            var gist = JsonSerializer.Deserialize<GistResponse>(body, ImportOptions);
            var content = gist?.Files?.GetValueOrDefault(GistFilename)?.Content;
            if (string.IsNullOrEmpty(content)) return null;

            return JsonSerializer.Deserialize<CoffeeBackup>(content, ImportOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Sync = pull + push (le pull en premier, le push ensuite si pull a réussi).</summary>
    public async Task<bool> SyncAsync()
    {
        if (!IsConfigured) return false;

        // Si pas de GistId localement, on tente d'en retrouver un sur GitHub avant de créer.
        if (string.IsNullOrEmpty(GistId))
        {
            var found = await FindExistingGistAsync();
            if (!string.IsNullOrEmpty(found))
            {
                GistId = found;
                await SetItem(GistIdKey, found);
                StateChanged?.Invoke();
            }
        }

        // Toujours pas de Gist : c'est une première synchro → push direct (crée le Gist).
        if (string.IsNullOrEmpty(GistId)) return await PushAsync();

        var pulled = await PullAsync();
        if (!pulled) return false;
        return await PushAsync();
    }

    private async Task<CoffeeBackup> BuildBackupAsync() => new()
    {
        Version = 4,
        ExportedAt = DateTime.UtcNow,
        Coffees = await _db.Coffees.ToCollection().ToList(),
        Brews = await _db.Brews.ToCollection().ToList(),
        Shops = await _db.Shops.ToCollection().ToList(),
        ShopVisits = await _db.ShopVisits.ToCollection().ToList(),
        Machines = await _db.Machines.ToCollection().ToList()
    };

    /// <summary>
    /// Fusionne un backup distant dans le stockage local SANS jamais effacer d'enregistrements
    /// locaux. Chaque enregistrement distant est inséré ou met à jour celui de même Id ; un
    /// enregistrement local absent du distant est conservé. Sur conflit (même Id des deux côtés),
    /// le plus récent gagne (horodatage UpdatedAt si présent, sinon CreatedAt).
    /// Retourne le nombre d'enregistrements distants pris en compte.
    /// </summary>
    private async Task<int> MergeBackupAsync(CoffeeBackup data)
    {
        var n = 0;
        n += await MergeStoreAsync(_db.Coffees, data.Coffees, c => c.UpdatedAt);
        n += await MergeStoreAsync(_db.Brews, data.Brews, b => b.CreatedAt);
        n += await MergeStoreAsync(_db.Shops, data.Shops, s => s.UpdatedAt);
        n += await MergeStoreAsync(_db.ShopVisits, data.ShopVisits, v => v.CreatedAt);
        n += await MergeStoreAsync(_db.Machines, data.Machines, m => m.UpdatedAt);
        return n;
        // Pour un backup pré-v4 (Shops null), la migration au démarrage suivant
        // reconstruira les Shops à partir des visites legacy (cf. MigrationService).
    }

    private static async Task<int> MergeStoreAsync<T>(
        BlazorDexie.Database.Store<T, int> store, List<T>? incoming, Func<T, DateTime> stamp)
        where T : class
    {
        if (incoming is null) return 0;
        var n = 0;
        foreach (var item in incoming)
        {
            var key = GetId(item);
            // Enregistrement neuf (Id 0/absent) ou inconnu localement : on insère.
            // Sinon on n'écrase que si le distant est au moins aussi récent que le local.
            if (key == 0)
            {
                await store.Put(item);
            }
            else
            {
                var existing = await store.Get(key);
                if (existing is null || stamp(item) >= stamp(existing))
                    await store.Put(item);
            }
            n++;
        }
        return n;
    }

    private static int GetId<T>(T item) => item switch
    {
        Coffee c => c.Id,
        Brew b => b.Id,
        Shop s => s.Id,
        CoffeeShopVisit v => v.Id,
        Machine m => m.Id,
        BackupSnapshot bs => bs.Id,
        _ => 0
    };

    private static int CountRecords(CoffeeBackup b)
        => (b.Coffees?.Count ?? 0) + (b.Brews?.Count ?? 0) + (b.Shops?.Count ?? 0)
         + (b.ShopVisits?.Count ?? 0) + (b.Machines?.Count ?? 0);

    // ─── Clichés locaux de sauvegarde (filet de sécurité) ───

    public const int MaxSnapshots = 10;

    /// <summary>Capture un cliché complet du local. <paramref name="prebuilt"/> évite un rechargement
    /// si le backup a déjà été construit par l'appelant. Best-effort : n'échoue jamais la synchro.</summary>
    private async Task CaptureSnapshotAsync(string reason, CoffeeBackup? prebuilt = null)
    {
        try
        {
            var backup = prebuilt ?? await BuildBackupAsync();
            var snap = new BackupSnapshot
            {
                CreatedAt = DateTime.UtcNow,
                Reason = reason,
                RecordCount = CountRecords(backup),
                Json = JsonSerializer.Serialize(backup, BackupOptions)
            };
            await _db.Snapshots.Add(snap);
            await PruneSnapshotsAsync();
        }
        catch (Exception ex)
        {
            LastError = $"Cliché de sauvegarde : {ex.Message}";
        }
    }

    private async Task PruneSnapshotsAsync()
    {
        var all = await _db.Snapshots.OrderBy(nameof(BackupSnapshot.CreatedAt)).ToList();
        if (all.Count <= MaxSnapshots) return;
        foreach (var s in all.Take(all.Count - MaxSnapshots))
            await _db.Snapshots.Delete(s.Id);
    }

    /// <summary>Liste les clichés disponibles, du plus récent au plus ancien.</summary>
    public async Task<List<BackupSnapshot>> GetSnapshotsAsync()
    {
        var all = await _db.Snapshots.OrderBy(nameof(BackupSnapshot.CreatedAt)).ToList();
        all.Reverse();
        return all;
    }

    /// <summary>Restaure un cliché : remplace intégralement le local par son contenu.
    /// Un cliché de l'état courant est pris au préalable (la restauration est elle-même annulable).</summary>
    public async Task<bool> RestoreSnapshotAsync(int snapshotId)
    {
        var snap = await _db.Snapshots.Get(snapshotId);
        if (snap is null || string.IsNullOrEmpty(snap.Json)) return false;

        CoffeeBackup? backup;
        try { backup = JsonSerializer.Deserialize<CoffeeBackup>(snap.Json, ImportOptions); }
        catch { return false; }
        if (backup is null) return false;

        IsBusy = true;
        LastError = null;
        LastInfo = null;
        StateChanged?.Invoke();
        try
        {
            await CaptureSnapshotAsync("avant restauration");

            await _db.Coffees.Clear();
            await _db.Brews.Clear();
            await _db.ShopVisits.Clear();
            await _db.Shops.Clear();
            await _db.Machines.Clear();

            foreach (var c in backup.Coffees ?? new()) await _db.Coffees.Put(c);
            foreach (var b in backup.Brews ?? new()) await _db.Brews.Put(b);
            foreach (var s in backup.Shops ?? new()) await _db.Shops.Put(s);
            foreach (var v in backup.ShopVisits ?? new()) await _db.ShopVisits.Put(v);
            foreach (var m in backup.Machines ?? new()) await _db.Machines.Put(m);

            LastInfo = $"Restauration OK ({CountRecords(backup)} enreg.)";
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Restauration : {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke();
        }
    }

    private async Task<string?> GetItem(string key)
        => await _js.InvokeAsync<string?>("localStorage.getItem", key);

    private async Task SetItem(string key, string value)
        => await _js.InvokeVoidAsync("localStorage.setItem", key, value);

    private async Task RemoveItem(string key)
        => await _js.InvokeVoidAsync("localStorage.removeItem", key);

    private class GistResponse
    {
        public string? Id { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Dictionary<string, GistFile>? Files { get; set; }
    }

    private class GistFile
    {
        public string? Filename { get; set; }
        public string? Content { get; set; }
    }
}
