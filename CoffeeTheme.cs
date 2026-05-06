using MudBlazor;

namespace CoffeeTracker;

/// <summary>
/// Palette MudBlazor light + dark de l'app. Les couleurs sont alignées sur les variables CSS
/// <c>--coffee-*</c> définies dans <c>wwwroot/css/app.css</c> — toute modification ici doit
/// se refléter là-bas pour garder l'UI cohérente.
/// </summary>
public static class CoffeeTheme
{
    /// <summary>Thème unique de l'app, exploité par <c>App.razor</c>.</summary>
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
        PaletteDark = new PaletteDark
        {
            Primary = "#d4b487",
            Secondary = "#bd8d57",
            Tertiary = "#a06d3b",
            AppbarBackground = "#2a1812",
            AppbarText = "#fdfaf6",
            Background = "#1a100a",
            Surface = "#2a1812",
            DrawerBackground = "#2a1812",
            TextPrimary = "#fdfaf6",
            TextSecondary = "#c4ad8c",
            TextDisabled = "#7a6649",
            ActionDefault = "#c4ad8c",
            Divider = "#3b2418",
            DividerLight = "#2a1812",
            Dark = "#0e0805",
            DarkLighten = "#3b2418"
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
