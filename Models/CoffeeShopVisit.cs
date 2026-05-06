using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

/// <summary>
/// Visite ponctuelle dans un coffee shop. Chaque visite référence un <see cref="Shop"/> par
/// <see cref="ShopId"/> (le shop porte les infos partagées : nom, adresse, coords) et capture
/// uniquement ce qui change d'une visite à l'autre : la boisson, le café dégusté, le score, le prix.
/// </summary>
/// <remarks>
/// Les champs <see cref="ShopName"/>, <see cref="City"/>, <see cref="Country"/>, <see cref="Address"/>,
/// <see cref="Latitude"/>, <see cref="Longitude"/> et <see cref="ExternalPlaceId"/> sont des résidus
/// du modèle pré-v5 (avant l'extraction de Shop). Ils sont conservés en lecture pour permettre
/// au <see cref="Lib.MigrationService"/> de reconstruire les Shops au démarrage. Une fois la migration
/// appliquée, la source de vérité est dans <see cref="Shop"/> ; ne jamais lire/écrire ces champs
/// depuis le code applicatif.
/// </remarks>
public class CoffeeShopVisit
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    /// <summary>FK vers <see cref="Shop"/>. <c>0</c> indique une visite legacy non encore migrée.</summary>
    public int ShopId { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Type de boisson commandée (champ libre avec autocomplete : « Espresso », « Cappuccino », « V60 »…).</summary>
    public string DrinkType { get; set; } = string.Empty;

    /// <summary>FK optionnelle vers <see cref="Coffee"/> (en mode dégusté, cf. <see cref="Coffee.IsOwned"/> = false).</summary>
    public int? CoffeeId { get; set; }

    /// <summary>Texte libre de fallback si on ne veut pas créer un <see cref="Coffee"/> (legacy/quickfill).</summary>
    public string? CoffeeOrigin { get; set; }

    public string? Notes { get; set; }
    public int Rating { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; } = "EUR";
    public string? PhotoDataUrl { get; set; }

    /// <summary>Type de lait dans la boisson (null = sans lait).</summary>
    public MilkType? Milk { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ─── Champs legacy (pré-v5) — utilisés UNIQUEMENT par MigrationService ───
    // Une fois migrée, la source canonique est dans Shop. Ne pas afficher / éditer.

    public string ShopName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ExternalPlaceId { get; set; }
}
