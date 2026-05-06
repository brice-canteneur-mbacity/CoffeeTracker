using CoffeeTracker.Data;
using CoffeeTracker.Models;

namespace CoffeeTracker.Lib;

/// <summary>
/// Migrations idempotentes appliquées au démarrage de l'app. Chaque migration s'exécute
/// au plus une fois par utilisateur — la condition d'entrée est conçue pour devenir fausse
/// dès que la migration a tourné, donc relancer l'app n'a aucun effet.
/// </summary>
public class MigrationService(CoffeeDb db, SyncService sync)
{
    private readonly CoffeeDb _db = db;
    private readonly SyncService _sync = sync;

    /// <summary>
    /// Point d'entrée appelé au démarrage par <see cref="Layout.MainLayout"/>. Applique toutes
    /// les migrations en attente et déclenche un push vers le Gist si quelque chose a changé.
    /// Toute exception remonte au caller (qui l'affichera via snackbar).
    /// </summary>
    public async Task<MigrationReport> RunIfNeededAsync()
    {
        var report = new MigrationReport();

        // ─── Migration v4 → v5 : extraction des Shops depuis les visites ───
        // Critère : visite avec ShopId == 0 (jamais reliée à un Shop). Une fois reliées,
        // ShopId != 0 → la condition est fausse au prochain run.
        await MigrateVisitsToShopsAsync(report);

        // Si on a touché à des données, déclenche un push vers le Gist.
        if (report.HasChanges) _sync.RequestPush();

        return report;
    }

    /// <summary>
    /// Renvoie un état diagnostic sans modifier les données. Utilisé par Réglages.
    /// </summary>
    public async Task<MigrationDiagnostic> DiagnoseAsync()
    {
        var visits = await _db.ShopVisits.ToCollection().ToList();
        var shops = await _db.Shops.ToCollection().ToList();
        var pendingVisits = visits.Where(v => v.ShopId == 0).ToList();
        var orphanVisits = visits
            .Where(v => v.ShopId != 0 && !shops.Any(s => s.Id == v.ShopId))
            .ToList();
        return new MigrationDiagnostic
        {
            VisitCount = visits.Count,
            ShopCount = shops.Count,
            PendingVisitCount = pendingVisits.Count,
            OrphanVisitCount = orphanVisits.Count,
            PendingPreview = pendingVisits.Take(5)
                .Select(v => $"#{v.Id} {v.ShopName ?? "(sans nom)"} {(v.City is null ? "" : $"· {v.City}")}")
                .ToList()
        };
    }

    /// <summary>
    /// Déclenchement manuel : tente la migration et lève l'exception en cas d'échec
    /// (utilisé depuis Réglages avec affichage du message complet à l'utilisateur).
    /// </summary>
    public async Task<MigrationReport> RunManualAsync()
    {
        var report = new MigrationReport();
        await MigrateVisitsToShopsAsync(report);
        if (report.HasChanges) _sync.RequestPush();
        return report;
    }

    /// <summary>
    /// Migration v4→v5 : pour chaque visite legacy (<see cref="CoffeeShopVisit.ShopId"/> = 0),
    /// retrouve ou crée le <see cref="Shop"/> correspondant en regroupant par (Name + City)
    /// case-insensitive, puis lie la visite au shop. Idempotente : ne touche pas aux visites
    /// déjà migrées.
    /// </summary>
    private async Task MigrateVisitsToShopsAsync(MigrationReport report)
    {
        var visits = await _db.ShopVisits.ToCollection().ToList();
        var pending = visits.Where(v => v.ShopId == 0).ToList();
        if (pending.Count == 0) return;

        // Indexe les Shops déjà existants pour éviter les doublons si on reload une 2e fois
        // (rare, mais sûreté).
        var existingShops = await _db.Shops.ToCollection().ToList();
        var byKey = new Dictionary<string, Shop>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in existingShops)
        {
            byKey.TryAdd(MakeKey(s.Name, s.City), s);
        }

        // Regroupe les visites pending par (nom + ville) — une seule entité Shop par groupe.
        var groups = pending
            .GroupBy(v => MakeKey(v.ShopName, v.City), StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var g in groups)
        {
            // Source canonique : la visite la plus récente (les coords/adresse les plus à jour).
            var canonical = g.OrderByDescending(v => v.Date).First();

            int shopId;
            if (byKey.TryGetValue(g.Key, out var existing))
            {
                shopId = existing.Id;
            }
            else
            {
                var displayName = string.IsNullOrWhiteSpace(canonical.ShopName)
                    ? "(sans nom)"
                    : canonical.ShopName.Trim();

                var shop = new Shop
                {
                    Name = displayName,
                    City = canonical.City,
                    Country = canonical.Country,
                    Address = canonical.Address,
                    Latitude = canonical.Latitude,
                    Longitude = canonical.Longitude,
                    ExternalPlaceId = canonical.ExternalPlaceId,
                    PhotoDataUrl = canonical.PhotoDataUrl,
                    CreatedAt = canonical.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                shopId = await _db.Shops.Add(shop);
                shop.Id = shopId;
                byKey[g.Key] = shop;
                report.ShopsCreated++;
            }

            foreach (var v in g)
            {
                v.ShopId = shopId;
                await _db.ShopVisits.Put(v);
                report.VisitsLinked++;
            }
        }
    }

    /// <summary>Clé de regroupement « name|city » insensible à la casse, espaces normalisés.</summary>
    private static string MakeKey(string? name, string? city)
        => $"{(name ?? string.Empty).Trim()}|{(city ?? string.Empty).Trim()}".ToLowerInvariant();
}

/// <summary>Compte rendu d'une migration : combien de Shops créés, combien de visites liées.</summary>
public class MigrationReport
{
    public int ShopsCreated { get; set; }
    public int VisitsLinked { get; set; }
    public bool HasChanges => ShopsCreated > 0 || VisitsLinked > 0;
}

/// <summary>État courant des données vis-à-vis des migrations — affiché dans Réglages.</summary>
public class MigrationDiagnostic
{
    /// <summary>Nombre total de visites en base.</summary>
    public int VisitCount { get; set; }
    /// <summary>Nombre total de shops en base.</summary>
    public int ShopCount { get; set; }
    /// <summary>Visites avec ShopId=0 (jamais migrées).</summary>
    public int PendingVisitCount { get; set; }
    /// <summary>Visites avec un ShopId pointant vers un shop inexistant (anomalie).</summary>
    public int OrphanVisitCount { get; set; }
    /// <summary>Aperçu des 5 premières visites en attente (pour debug).</summary>
    public List<string> PendingPreview { get; set; } = new();
}
