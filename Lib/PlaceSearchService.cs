using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

/// <summary>
/// Résultat unitaire d'une recherche de coffee shop.
/// </summary>
/// <param name="Provider">"google" ou "osm" — détermine le préfixe de <paramref name="ExternalId"/>.</param>
/// <param name="ExternalId">Identifiant préfixé du provider (ex : "google:ChIJ…", "osm:12345").</param>
/// <param name="Name">Nom du commerce.</param>
/// <param name="Address">Adresse formatée complète (peut être null si le provider ne la fournit pas).</param>
/// <param name="City">Ville extraite des composants d'adresse.</param>
/// <param name="Country">Pays extrait des composants d'adresse.</param>
/// <param name="Latitude">WGS84.</param>
/// <param name="Longitude">WGS84.</param>
public record PlaceResult(
    string Provider,
    string ExternalId,
    string Name,
    string? Address,
    string? City,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);

/// <summary>Réponse de <see cref="PlaceSearchService.SearchAsync"/>.</summary>
public record PlaceSearchResponse(string Provider, List<PlaceResult> Results);

/// <summary>
/// Wrapper C# autour de l'interop JS <c>coffeeShopSearch</c> (cf. <c>wwwroot/js/shopSearch.js</c>).
/// La logique de provider (Google Places New si une clé API est configurée en localStorage,
/// sinon Photon/OpenStreetMap par défaut) est entièrement côté JS — ce service ne fait que router.
/// </summary>
public class PlaceSearchService(IJSRuntime js)
{
    private readonly IJSRuntime _js = js;

    /// <summary>
    /// Recherche un coffee shop par nom. Retourne une réponse vide si la requête fait moins
    /// de 2 caractères (pour éviter de spammer les APIs externes).
    /// </summary>
    /// <param name="query">Texte saisi par l'utilisateur.</param>
    /// <param name="forceProvider">"google" ou "osm" pour court-circuiter la sélection auto. null = auto.</param>
    public async Task<PlaceSearchResponse> SearchAsync(string query, string? forceProvider = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new PlaceSearchResponse("none", new List<PlaceResult>());
        return await _js.InvokeAsync<PlaceSearchResponse>("coffeeShopSearch.search", ct, query, forceProvider);
    }

    /// <summary>Lit la clé Google API stockée en localStorage (null si non configurée).</summary>
    public async Task<string?> GetGoogleKeyAsync()
        => await _js.InvokeAsync<string?>("coffeeShopSearch.getGoogleKey");

    /// <summary>Persiste (ou efface si null/vide) la clé Google API en localStorage.</summary>
    public async Task SetGoogleKeyAsync(string? key)
        => await _js.InvokeVoidAsync("coffeeShopSearch.setGoogleKey", key);
}
