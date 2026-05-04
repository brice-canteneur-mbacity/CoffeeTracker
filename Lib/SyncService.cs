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

        if (!IsConfigured) return;

        // Si on n'a pas de GistId localement, on cherche un Gist existant dans le compte
        // (cas typique : nouveau navigateur où on vient de saisir le PAT).
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

        // Auto-pull si on a maintenant un GistId.
        if (!string.IsNullOrEmpty(GistId))
        {
            _ = PullAsync();
        }
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
            LastInfo = $"Push OK ({backup.Coffees?.Count ?? 0} cafés, {backup.Brews?.Count ?? 0} brews, {backup.ShopVisits?.Count ?? 0} visites, {backup.Machines?.Count ?? 0} machines)";
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

            await ApplyBackupAsync(data);

            LastSync = DateTime.UtcNow;
            await SetItem(LastSyncKey, LastSync.Value.ToString("O"));
            LastInfo = $"Pull OK ({data.Coffees?.Count ?? 0} cafés, {data.Brews?.Count ?? 0} brews, {data.ShopVisits?.Count ?? 0} visites, {data.Machines?.Count ?? 0} machines)";
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

    /// <summary>
    /// Demande un push différé (debounced). Plusieurs appels rapprochés sont coalescés
    /// en un seul push après <paramref name="delayMs"/> ms d'inactivité. Fire-and-forget.
    /// </summary>
    public void RequestPush(int delayMs = 3000)
    {
        if (!IsConfigured) return;
        _pendingPush?.Cancel();
        var cts = new CancellationTokenSource();
        _pendingPush = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, cts.Token);
                await PushAsync();
            }
            catch (OperationCanceledException) { /* coalescé */ }
            catch (Exception ex)
            {
                LastError = $"Auto-push : {ex.Message}";
                StateChanged?.Invoke();
            }
        });
    }

    private CancellationTokenSource? _pendingPush;

    private async Task<CoffeeBackup> BuildBackupAsync() => new()
    {
        Version = 3,
        ExportedAt = DateTime.UtcNow,
        Coffees = await _db.Coffees.ToCollection().ToList(),
        Brews = await _db.Brews.ToCollection().ToList(),
        ShopVisits = await _db.ShopVisits.ToCollection().ToList(),
        Machines = await _db.Machines.ToCollection().ToList()
    };

    private async Task ApplyBackupAsync(CoffeeBackup data)
    {
        await _db.Coffees.Clear();
        await _db.Brews.Clear();
        await _db.ShopVisits.Clear();
        await _db.Machines.Clear();

        foreach (var c in data.Coffees ?? new()) await _db.Coffees.Put(c);
        foreach (var b in data.Brews ?? new()) await _db.Brews.Put(b);
        foreach (var v in data.ShopVisits ?? new()) await _db.ShopVisits.Put(v);
        foreach (var m in data.Machines ?? new()) await _db.Machines.Put(m);
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
