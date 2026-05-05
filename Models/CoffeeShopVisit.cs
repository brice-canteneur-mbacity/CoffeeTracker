using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

/// <summary>
/// Visite ponctuelle dans un coffee shop. Réfère un Shop par ShopId.
/// Les champs ShopName / City / Country / Address / Latitude / Longitude / ExternalPlaceId
/// sont conservés pour permettre la migration des données pré-v5 (cf. MigrationService) :
/// après migration, la source de vérité de ces infos est le Shop référencé.
/// </summary>
public class CoffeeShopVisit
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }
    /// <summary>FK vers Shop. 0 indique une visite legacy non encore migrée.</summary>
    public int ShopId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string DrinkType { get; set; } = string.Empty;
    public int? CoffeeId { get; set; }
    public string? CoffeeOrigin { get; set; }
    public string? Notes { get; set; }
    public int Rating { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; } = "EUR";
    public string? PhotoDataUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ─── Champs legacy (pré-v5) — utilisés UNIQUEMENT par la migration ───
    // Une fois migrée, la source canonique est dans Shop. Ne pas afficher / éditer.
    public string ShopName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ExternalPlaceId { get; set; }
}
