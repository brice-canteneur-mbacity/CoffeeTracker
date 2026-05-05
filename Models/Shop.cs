using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

/// <summary>
/// Coffee shop visité (lieu réel). Une visite (CoffeeShopVisit) référence un Shop par ShopId.
/// Les coordonnées et l'adresse sont portées par le shop, pas par la visite : si le shop déménage
/// ou si tu corriges l'adresse, la modif s'applique à toutes les visites passées et futures.
/// </summary>
public class Shop
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ExternalPlaceId { get; set; }
    public string? PhotoDataUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
