namespace CoffeeTracker.Lib;

public record DegassingStatus(int Days, string Zone, string Label, string ShortLabel);

public static class Degassing
{
    // Generic degassing windows (varies in reality with roast level, density…)
    //   0-3 days  : too fresh (red)
    //   4-6 days  : filter ok, espresso encore jeune (yellow)
    //   7-21 days : fenêtre optimale (green)
    //   22-35 days: encore bon, sur le déclin (yellow)
    //   36+ days  : au-delà du pic (red)
    public const int MaxDays = 42;

    public static DegassingStatus? Compute(DateOnly? roastDate)
    {
        if (roastDate is null) return null;
        var days = DateOnly.FromDateTime(DateTime.Today).DayNumber - roastDate.Value.DayNumber;
        if (days < 0) return null;
        return new DegassingStatus(days, ZoneFor(days), LongLabelFor(days), ShortLabelFor(days));
    }

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
