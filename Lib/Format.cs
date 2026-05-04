using System.Globalization;

namespace CoffeeTracker.Lib;

public static class Format
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public static string Date(DateOnly? d) =>
        d is null ? "—" : d.Value.ToString("dd MMM yyyy", Fr);

    public static string DateTimeShort(DateTime? d) =>
        d is null ? "—" : d.Value.ToLocalTime().ToString("dd MMM, HH:mm", Fr);

    public static int? DaysSince(DateOnly? d) =>
        d is null ? null : DateOnly.FromDateTime(DateTime.Today).DayNumber - d.Value.DayNumber;

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

    public static string? Ratio(decimal? dose, decimal? yieldG)
    {
        if (dose is null or 0 || yieldG is null or 0) return null;
        var r = (double)yieldG.Value / (double)dose.Value;
        if (!double.IsFinite(r)) return null;
        return $"1:{r.ToString("0.#", CultureInfo.InvariantCulture)}";
    }
}
