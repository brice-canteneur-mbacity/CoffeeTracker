namespace CoffeeTracker.Lib;

/// <summary>
/// Statut de dégazage à un instant T pour un café donné.
/// </summary>
/// <param name="Days">Nombre de jours depuis la torréfaction.</param>
/// <param name="Zone">Identifiant CSS de la zone (too-fresh / filter-only / optimal / declining / past).</param>
/// <param name="Label">Description longue affichée sous la jauge.</param>
/// <param name="ShortLabel">Étiquette courte affichée à côté du compteur de jours.</param>
public record DegassingStatus(int Days, string Zone, string Label, string ShortLabel);

/// <summary>
/// Calcul de la fenêtre de dégazage d'un café après torréfaction.
/// Les seuils sont génériques (la vraie fenêtre dépend du niveau de torréfaction et de la
/// densité du café) mais collent à 80% des cas en specialty coffee.
/// </summary>
public static class Degassing
{
    // Generic degassing windows (varies in reality with roast level, density…)
    //   0-3 days  : trop frais (zone rouge)
    //   4-6 days  : filter ok, espresso encore jeune (zone jaune)
    //   7-21 days : fenêtre optimale (zone verte)
    //   22-35 days: encore bon, sur le déclin (zone jaune)
    //   36+ days  : au-delà du pic (zone rouge)

    /// <summary>Plage maximale affichée sur la timeline (utilisée pour calibrer la jauge UI).</summary>
    public const int MaxDays = 42;

    /// <summary>
    /// Calcule le statut de dégazage pour une <paramref name="roastDate"/> donnée.
    /// Renvoie null si la date est manquante ou dans le futur (clock skew, faute de saisie).
    /// </summary>
    public static DegassingStatus? Compute(DateOnly? roastDate)
    {
        if (roastDate is null) return null;
        var days = DateOnly.FromDateTime(DateTime.Today).DayNumber - roastDate.Value.DayNumber;
        if (days < 0) return null;
        return new DegassingStatus(days, ZoneFor(days), LongLabelFor(days), ShortLabelFor(days));
    }

    /// <summary>Zone catégorielle (utilisée pour piloter une couleur ou un style).</summary>
    public static string ZoneFor(int days) => days switch
    {
        <= 3 => "too-fresh",
        <= 6 => "filter-only",
        <= 21 => "optimal",
        <= 35 => "declining",
        _ => "past"
    };

    private static string LongLabelFor(int days) => days switch
    {
        <= 3 => "Trop frais — laisse dégazer encore quelques jours",
        <= 6 => "Filter ok, espresso encore un peu jeune",
        <= 21 => "Fenêtre optimale",
        <= 35 => "Encore bon, mais sur le déclin",
        _ => "Au-delà du pic — peut être plat"
    };

    private static string ShortLabelFor(int days) => days switch
    {
        <= 3 => "Trop frais",
        <= 6 => "Filter ok",
        <= 21 => "Optimal",
        <= 35 => "Décline",
        _ => "Trop vieux"
    };
}
