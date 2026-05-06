using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

/// <summary>
/// Préparation maison d'un café (espresso, V60, AeroPress, etc.). Une brew réfère un
/// <see cref="Coffee"/> par <see cref="CoffeeId"/> et déduit du stock du sac via <see cref="DoseG"/>.
/// </summary>
public class Brew
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    /// <summary>FK obligatoire vers <see cref="Coffee"/> (uniquement les cafés <see cref="Coffee.IsOwned"/>).</summary>
    public int CoffeeId { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;
    public BrewMethod Method { get; set; } = BrewMethod.Espresso;

    // ─── Recette (tout est optionnel : on peut tracker juste un score) ───
    /// <summary>Dose de café moulu (g). Utilisée pour calculer le stock restant.</summary>
    public decimal? DoseG { get; set; }
    /// <summary>Yield (poids final de boisson en g). Calculé auto si Dose × Ratio est connu.</summary>
    public decimal? YieldG { get; set; }
    /// <summary>Ratio cible sous forme texte (« 1:2 », « 1:16 »…). Le formulaire propose des chips presets.</summary>
    public string? Ratio { get; set; }
    public string? GrindSize { get; set; }
    public string? Notes { get; set; }

    /// <summary>0–5 ⭐.</summary>
    public int Rating { get; set; }
    public bool IsFavorite { get; set; }

    /// <summary>FK optionnelle vers la <see cref="Machine"/> ayant produit le brew (filtrée par méthode).</summary>
    public int? BrewerMachineId { get; set; }
    /// <summary>FK optionnelle vers la <see cref="Machine"/> de type Grinder.</summary>
    public int? GrinderId { get; set; }

    /// <summary>Type de lait ajouté (null = sans lait).</summary>
    public MilkType? Milk { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
