using System.Globalization;

namespace CoffeeTracker.Lib;

/// <summary>
/// Helpers d'affichage pour les types primitifs (dates, prix, ratios).
/// Suit la culture courante (settée par <see cref="LocalizationService.InitializeAsync"/>) :
/// fr-FR, en-GB ou it-IT selon la langue active.
/// </summary>
public static class Format
{
    private static CultureInfo Cul => CultureInfo.CurrentCulture;

    /// <summary>Format date courte localisée (ex : « 06 mai 2026 » en FR, « 06 May 2026 » en EN). null → « — ».</summary>
    public static string Date(DateOnly? d) =>
        d is null ? "—" : d.Value.ToString("dd MMM yyyy", Cul);

    /// <summary>Format date+heure courte, converti en heure locale.</summary>
    public static string DateTimeShort(DateTime? d) =>
        d is null ? "—" : d.Value.ToLocalTime().ToString("dd MMM, HH:mm", Cul);

    /// <summary>Nombre de jours écoulés depuis <paramref name="d"/> jusqu'à aujourd'hui (peut être négatif).</summary>
    public static int? DaysSince(DateOnly? d) =>
        d is null ? null : DateOnly.FromDateTime(DateTime.Today).DayNumber - d.Value.DayNumber;

    /// <summary>
    /// Prix formaté avec devise (utilise la culture courante pour la mise en forme du nombre,
    /// remplace le symbole monétaire par celui demandé). Tolère les currencies inconnues.
    /// </summary>
    public static string Price(decimal? p, string? currency)
    {
        if (p is null) return "—";
        try
        {
            var c = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency;
            var formatted = string.Format(Cul, "{0:C}", p.Value);
            // Remplace le symbole de la culture (€/$/£) par celui demandé si différent.
            var nativeSymbol = Cul.NumberFormat.CurrencySymbol;
            var targetSymbol = c == "EUR" ? "€" : (c == "USD" ? "$" : (c == "GBP" ? "£" : c));
            return formatted.Replace(nativeSymbol, targetSymbol);
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
