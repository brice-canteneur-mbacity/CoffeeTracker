using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

public record PlaceResult(
    string Provider,
    string ExternalId,
    string Name,
    string? Address,
    string? City,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);

public record PlaceSearchResponse(string Provider, List<PlaceResult> Results);

public class PlaceSearchService(IJSRuntime js)
{
    private readonly IJSRuntime _js = js;

    public async Task<PlaceSearchResponse> SearchAsync(string query, string? forceProvider = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new PlaceSearchResponse("none", new List<PlaceResult>());
        return await _js.InvokeAsync<PlaceSearchResponse>("coffeeShopSearch.search", ct, query, forceProvider);
    }

    public async Task<string?> GetGoogleKeyAsync()
        => await _js.InvokeAsync<string?>("coffeeShopSearch.getGoogleKey");

    public async Task SetGoogleKeyAsync(string? key)
        => await _js.InvokeVoidAsync("coffeeShopSearch.setGoogleKey", key);
}
