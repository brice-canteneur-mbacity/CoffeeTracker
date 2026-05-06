using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

/// <summary>
/// Gère la préférence de thème (light/dark/system) avec persistance en localStorage et
/// application live de la classe CSS sur &lt;body&gt; via <c>coffeeTheme</c> JS.
/// L'écoute des changements système (media query) est faite côté JS.
/// </summary>
public class ThemeService(IJSRuntime js)
{
    private readonly IJSRuntime _js = js;

    /// <summary>Préférence stockée : "system" (par défaut), "light", ou "dark".</summary>
    public string Preference { get; private set; } = "system";
    /// <summary>État actuel du média OS (lu une fois à l'init).</summary>
    public bool SystemPrefersDark { get; private set; }
    /// <summary>État final résolu : combine <see cref="Preference"/> et <see cref="SystemPrefersDark"/>.</summary>
    public bool IsDarkMode => Preference switch
    {
        "dark" => true,
        "light" => false,
        _ => SystemPrefersDark
    };

    /// <summary>Émis quand <see cref="SetPreferenceAsync"/> est appelé.</summary>
    public event Action? Changed;

    /// <summary>
    /// À appeler une fois au démarrage de l'app : charge la préférence persistée, lit
    /// le média OS, et applique la classe `theme-dark` sur &lt;body&gt; en conséquence.
    /// </summary>
    public async Task InitializeAsync()
    {
        Preference = await _js.InvokeAsync<string>("coffeeTheme.getPreference");
        SystemPrefersDark = await _js.InvokeAsync<bool>("coffeeTheme.prefersDark");
        await _js.InvokeVoidAsync("coffeeTheme.applyBodyClass", IsDarkMode);
        Changed?.Invoke();
    }

    /// <summary>Met à jour la préférence et reflète le changement sur la classe body. Valeurs invalides → "system".</summary>
    public async Task SetPreferenceAsync(string pref)
    {
        Preference = pref switch { "light" or "dark" or "system" => pref, _ => "system" };
        await _js.InvokeVoidAsync("coffeeTheme.setPreference", Preference);
        await _js.InvokeVoidAsync("coffeeTheme.applyBodyClass", IsDarkMode);
        Changed?.Invoke();
    }
}
