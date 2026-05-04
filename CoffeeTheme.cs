using MudBlazor;

namespace CoffeeTracker;

public static class CoffeeTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#3b2418",
            Secondary = "#a06d3b",
            Tertiary = "#d4b487",
            AppbarBackground = "#3b2418",
            AppbarText = "#fdfaf6",
            Background = "#fdfaf6",
            Surface = "#ffffff",
            DrawerBackground = "#ffffff",
            TextPrimary = "#241510",
            TextSecondary = "#5a3a1d",
            ActionDefault = "#5a3a1d",
            Divider = "#f6efe4",
            DividerLight = "#f6efe4"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "system-ui", "sans-serif" }
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px"
        }
    };
}
