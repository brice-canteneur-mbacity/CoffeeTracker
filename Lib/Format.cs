using System.Globalization;

namespace CoffeeTracker.Lib;

/// <summary>
/// Helpers d'affichage pour les types primitifs (dates, prix, ratios).
/// Centralisé ici pour garantir une présentation cohérente dans toute l'UI
/// (locale française, fallback "—" pour les valeurs nulles).
/// </summary>
public static class Format
{
    private static readonly CultureInfo Fr = new("fr-FR");

    /// <summary>Format date courte localisée (ex : « 06 mai 2026 »). null → « — ».</summary>
    public static string Date(DateOnly? d) =>
        d is null ? "—" : d.Value.ToString("dd MMM yyyy", Fr);

    /// <summary>Format date+heure courte (ex : « 06 mai, 14:32 »), converti en heure locale.</summary>
    public static string DateTimeShort(DateTime? d) =>
        d is null ? "—" : d.Value.ToLocalTime().ToString("dd MMM, HH:mm", Fr);

    /// <summary>Nombre de jours écoulés depuis <paramref name="d"/> jusqu'à aujourd'hui (peut être négatif).</summary>
    public static int? DaysSince(DateOnly? d) =>
        d is null ? null : DateOnly.FromDateTime(DateTime.Today).DayNumber - d.Value.DayNumber;

    /// <summary>
    /// Prix formaté avec devise (€ pour EUR, sinon le code ISO tel quel).
    /// Tolère les currencies inconnues sans planter (fallback sur concat brute).
    /// </summary>
    public static string Price(decimal? p, string? currency)
    {
        if (p is null) return "—";
        try
        {
            var c = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency;
            var nfi = (NumberFormatInfo)Fr.NumberFormat.Clone();
            return string.Format(Fr, "{0:C}", p.Value).Replace("€", c == "EUR" ? "€" : c);
        }
        catch
        {
            return $"{p} {currency}";
        }
    }

    /// <summary>
    /// Ratio dose:yield au format texte « 1:X.X ». Renvoie null si une des valeurs est manquante,
    /// nulle, ou produit un ratio non fini.
    /// </summary>
    public static string? Ratio(decimal? dose, decimal? yieldG)
    {
        if (dose is null or 0 || yieldG is null or 0) return null;
        var r = (double)yieldG.Value / (double)dose.Value;
        if (!double.IsFinite(r)) return null;
        return $"1:{r.ToString("0.#", CultureInfo.InvariantCulture)}";
    }
}
