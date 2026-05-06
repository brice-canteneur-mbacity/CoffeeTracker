using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

/// <summary>
/// Représente un café dans la bibliothèque de l'utilisateur. Couvre deux usages :
/// <list type="bullet">
/// <item><b>Possédé</b> (<see cref="IsOwned"/> = true) : sac acheté avec poids/prix/date/dégazage,
/// brassé à la maison via les <see cref="Brew"/>.</item>
/// <item><b>Dégusté</b> (<see cref="IsOwned"/> = false) : café bu en boutique, créé automatiquement
/// depuis une visite (<see cref="CoffeeShopVisit"/>) si le nom n'existait pas en bibliothèque.</item>
/// </list>
/// </summary>
public class Coffee
{
    /// <summary>
    /// Auto-incrémenté par Dexie. Sérialisé sans la valeur 0 pour permettre l'auto-increment
    /// sur Add (cf. <see cref="JsonIgnoreCondition.WhenWritingDefault"/>).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    public string Roaster { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // ─── Origine (mono-origine ; non rempli si IsBlend) ───
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? Farm { get; set; }
    public string? Producer { get; set; }
    public string? Altitude { get; set; }
    public string? Variety { get; set; }

    public Process Process { get; set; } = Process.Washed;
    public string? ProcessNotes { get; set; }

    public bool IsDecaf { get; set; }
    /// <summary>Méthode de décaféination si <see cref="IsDecaf"/> est true (sinon ignoré).</summary>
    public DecafProcess? DecafMethod { get; set; }

    /// <summary>True = sac physique avec stock ; false = café simplement dégusté en boutique.</summary>
    public bool IsOwned { get; set; } = true;

    /// <summary>True = mélange de plusieurs origines ; remplit <see cref="Composition"/> et ignore les champs Country/Region/Farm.</summary>
    public bool IsBlend { get; set; }
    /// <summary>Description libre du blend (ex : « 60% Brésil natural, 40% Éthiopie washed »).</summary>
    public string? Composition { get; set; }

    public DateOnly? RoastDate { get; set; }
    public RoastLevel? RoastLevel { get; set; }

    // ─── Achat (uniquement si IsOwned) ───
    public int? WeightGrams { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; } = "EUR";

    /// <summary>
    /// Ajustement manuel du stock en grammes (positif = ajouté, négatif = retiré).
    /// Permet à l'utilisateur de recaler le stock après pesée du sac
    /// (compense oublis de brews, mauvais brews jetés, café offert, etc.).
    /// Stock effectif = WeightGrams + StockAdjustmentG - somme(Brew.DoseG).
    /// </summary>
    public decimal StockAdjustmentG { get; set; }

    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Notes du torréfacteur (sucre, fruit, fleur, finale…).</summary>
    public string? TastingNotes { get; set; }

    /// <summary>Photo encodée en data URL (jpeg/png compressé via canvas — cf. imageHelper.js).</summary>
    public string? PhotoDataUrl { get; set; }

    /// <summary>Date de fin du sac (set quand l'utilisateur clique « Marquer terminé »). null = en cours.</summary>
    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
