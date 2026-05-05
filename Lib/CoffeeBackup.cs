using CoffeeTracker.Models;

namespace CoffeeTracker.Lib;

/// <summary>
/// Format de sauvegarde JSON utilisé par l'export manuel et la sync GitHub Gist.
/// </summary>
public class CoffeeBackup
{
    /// <summary>
    /// Version 4 : ajout de la liste Shops (entité distincte). Les versions ≤ 3 ne contenaient
    /// que ShopVisits dénormalisées — l'import d'un backup v ≤ 3 sera rattrapé par la migration
    /// au démarrage suivant (MigrationService).
    /// </summary>
    public int Version { get; set; } = 4;
    public DateTime ExportedAt { get; set; }
    public List<Coffee>? Coffees { get; set; }
    public List<Brew>? Brews { get; set; }
    public List<Shop>? Shops { get; set; }
    public List<CoffeeShopVisit>? ShopVisits { get; set; }
    public List<Machine>? Machines { get; set; }
}
