using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

public class ThemeService(IJSRuntime js)
{
    private readonly IJSRuntime _js = js;

    public string Preference { get; private set; } = "system";
    public bool SystemPrefersDark { get; private set; }
    public bool IsDarkMode => Preference switch
    {
        "dark" => true,
        "light" => false,
        _ => SystemPrefersDark
    };

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        Preference = await _js.InvokeAsync<string>("coffeeTheme.getPreference");
        SystemPrefersDark = await _js.InvokeAsync<bool>("coffeeTheme.prefersDark");
        await _js.InvokeVoidAsync("coffeeTheme.applyBodyClass", IsDarkMode);
        Changed?.Invoke();
    }

    public async Task SetPreferenceAsync(string pref)
    {
        Preference = pref switch { "light" or "dark" or "system" => pref, _ => "system" };
        await _js.InvokeVoidAsync("coffeeTheme.setPreference", Preference);
        await _js.InvokeVoidAsync("coffeeTheme.applyBodyClass", IsDarkMode);
        Changed?.Invoke();
    }
}
