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

    private static string MakeKey(string? name, string? city)
        => $"{(name ?? string.Empty).Trim()}|{(city ?? string.Empty).Trim()}".ToLowerInvariant();
}

public class MigrationReport
{
    public int ShopsCreated { get; set; }
    public int VisitsLinked { get; set; }
    public bool HasChanges => ShopsCreated > 0 || VisitsLinked > 0;
}
