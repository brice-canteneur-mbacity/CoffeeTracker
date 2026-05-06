using System.Globalization;
using System.Net.Http.Json;
using CoffeeTracker.Models;
using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

/// <summary>
/// Service de localisation léger. Charge un dictionnaire JSON par langue depuis <c>wwwroot/i18n/</c>
/// et expose des helpers <see cref="T(string)"/>, <see cref="T(string, object[])"/> et
/// <see cref="TEnum(Enum)"/>. La langue active est déterminée dans cet ordre :
/// <list type="number">
/// <item>Préférence persistée en localStorage (si l'utilisateur a explicitement choisi).</item>
/// <item><c>navigator.language</c> du navigateur, mappée à <see cref="SupportedLanguages"/>.</item>
/// <item>Fallback : <see cref="DefaultLanguage"/>.</item>
/// </list>
/// </summary>
public class LocalizationService
{
    /// <summary>Langues supportées (code court ISO 639-1).</summary>
    public static readonly string[] SupportedLanguages = { "fr", "en", "it" };
    public const string DefaultLanguage = "fr";

    private readonly IJSRuntime _js;
    private readonly HttpClient _http;
    private readonly Dictionary<string, Dictionary<string, string>> _dicts = new();

    /// <summary>Code de la langue courante ("fr" / "en" / "it"). Lecture seule depuis l'extérieur.</summary>
    public string CurrentLanguage { get; private set; } = DefaultLanguage;

    /// <summary>True si l'utilisateur a explicitement défini une préférence (via Réglages).</summary>
    public bool HasUserPreference { get; private set; }

    /// <summary>Émis quand <see cref="SetLanguageAsync"/> change la langue active.</summary>
    public event Action? LanguageChanged;

    public LocalizationService(IJSRuntime js, HttpClient http)
    {
        _js = js;
        _http = http;
    }

    /// <summary>
    /// À appeler une fois au démarrage. Détecte la langue à utiliser, charge le dictionnaire
    /// correspondant, applique la culture .NET pour le formatage des dates/nombres, et installe
    /// la passerelle vers <see cref="EnumExtensions.GetLabel"/>.
    /// </summary>
    public async Task InitializeAsync()
    {
        var stored = await _js.InvokeAsync<string?>("coffeeI18n.getStoredPreference");
        HasUserPreference = !string.IsNullOrEmpty(stored);

        var detected = stored;
        if (string.IsNullOrEmpty(detected))
        {
            var browser = await _js.InvokeAsync<string>("coffeeI18n.getBrowserLanguage");
            detected = SupportedLanguages.Contains(browser) ? browser : DefaultLanguage;
        }
        else if (!SupportedLanguages.Contains(detected))
        {
            detected = DefaultLanguage;
        }

        await LoadAsync(detected!);
        CurrentLanguage = detected!;
        ApplyCulture(detected!);
        EnumExtensions.SetLocalizer(this);
    }

    /// <summary>Change la langue active, persiste la préférence, et recharge le dictionnaire.</summary>
    public async Task SetLanguageAsync(string lang)
    {
        if (!SupportedLanguages.Contains(lang)) lang = DefaultLanguage;
        if (!_dicts.ContainsKey(lang)) await LoadAsync(lang);
        CurrentLanguage = lang;
        HasUserPreference = true;
        await _js.InvokeVoidAsync("coffeeI18n.setStoredPreference", lang);
        ApplyCulture(lang);
        LanguageChanged?.Invoke();
    }

    /// <summary>Réinitialise la préférence : la langue redevient celle du navigateur au prochain reload.</summary>
    public async Task ResetToBrowserAsync()
    {
        await _js.InvokeVoidAsync("coffeeI18n.setStoredPreference", (string?)null);
        HasUserPreference = false;
        var browser = await _js.InvokeAsync<string>("coffeeI18n.getBrowserLanguage");
        var lang = SupportedLanguages.Contains(browser) ? browser : DefaultLanguage;
        if (!_dicts.ContainsKey(lang)) await LoadAsync(lang);
        CurrentLanguage = lang;
        ApplyCulture(lang);
        LanguageChanged?.Invoke();
    }

    /// <summary>
    /// Renvoie la traduction associée à la clé <paramref name="key"/>, ou la clé elle-même
    /// si elle est manquante (visibilité du bug en dev).
    /// </summary>
    public string T(string key)
    {
        if (_dicts.TryGetValue(CurrentLanguage, out var d) && d.TryGetValue(key, out var val))
            return val;
        // Fallback : tente la langue par défaut, sinon affiche la clé.
        if (CurrentLanguage != DefaultLanguage
            && _dicts.TryGetValue(DefaultLanguage, out var def)
            && def.TryGetValue(key, out var defVal))
            return defVal;
        return key;
    }

    /// <summary>Variante avec interpolation positionnelle <c>{0}</c>, <c>{1}</c>… via <see cref="string.Format(string, object[])"/>.</summary>
    public string T(string key, params object?[] args)
    {
        var template = T(key);
        try { return string.Format(template, args); }
        catch { return template; }
    }

    /// <summary>
    /// Renvoie le libellé localisé d'une valeur d'enum (clé : <c>enum.{type}.{name}</c> en lowercase).
    /// Fallback sur le <c>[Description]</c> de la valeur si la clé est absente.
    /// </summary>
    public string TEnum(Enum value)
    {
        var key = $"enum.{value.GetType().Name.ToLowerInvariant()}.{value.ToString().ToLowerInvariant()}";
        if (_dicts.TryGetValue(CurrentLanguage, out var d) && d.TryGetValue(key, out var val))
            return val;
        return EnumExtensions.GetDescriptionFallback(value);
    }

    private async Task LoadAsync(string lang)
    {
        // Cache busting pour ne pas se faire piéger par le service worker quand on update les JSON.
        var dict = await _http.GetFromJsonAsync<Dictionary<string, string>>(
            $"i18n/{lang}.json?v={DateTime.UtcNow.Ticks}");
        if (dict is not null) _dicts[lang] = dict;
    }

    private static void ApplyCulture(string lang)
    {
        // Pour le formatage des dates / nombres dans toute l'app (Format.cs lit la culture courante).
        var culture = lang switch
        {
            "en" => new CultureInfo("en-GB"),  // jj/mm/aaaa et symbole € si Currency=EUR
            "it" => new CultureInfo("it-IT"),
            _ => new CultureInfo("fr-FR")
        };
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
