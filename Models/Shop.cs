using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

/// <summary>
/// Coffee shop visité (lieu réel). Une visite (<see cref="CoffeeShopVisit"/>) référence un Shop
/// par <c>ShopId</c>. Les coordonnées et l'adresse sont portées par le shop, pas par la visite :
/// si le shop déménage ou si tu corriges l'adresse, la modif s'applique à toutes les visites
/// passées et futures.
/// </summary>
/// <remarks>
/// Introduit en DB v5 : avant, ces champs étaient dénormalisés sur chaque visite. Le
/// <see cref="Lib.MigrationService"/> reconstruit les Shops depuis les visites legacy au démarrage.
/// </remarks>
public class Shop
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }

    /// <summary>Latitude WGS84. Pré-remplie via <see cref="Lib.PlaceSearchService"/> ou laissée null.</summary>
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Identifiant externe préfixé du provider, ex : <c>"google:ChIJ…"</c>, <c>"osm:12345"</c>.
    /// Permet de retrouver la fiche d'origine si on veut la rafraîchir plus tard.
    /// </summary>
    public string? ExternalPlaceId { get; set; }

    public string? PhotoDataUrl { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
