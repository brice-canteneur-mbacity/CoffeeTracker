using CoffeeTracker.Models;

namespace CoffeeTracker.Lib;

/// <summary>
/// Format de sauvegarde JSON utilisé par l'export manuel et la sync GitHub Gist.
/// </summary>
public class CoffeeBackup
{
    public int Version { get; set; } = 3;
    public DateTime ExportedAt { get; set; }
    public List<Coffee>? Coffees { get; set; }
    public List<Brew>? Brews { get; set; }
    public List<CoffeeShopVisit>? ShopVisits { get; set; }
    public List<Machine>? Machines { get; set; }
}
