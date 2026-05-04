using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

public class CoffeeShopVisit
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string DrinkType { get; set; } = string.Empty;
    public int? CoffeeId { get; set; }
    public string? CoffeeOrigin { get; set; }
    public string? Notes { get; set; }
    public int Rating { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; } = "EUR";
    public string? PhotoDataUrl { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ExternalPlaceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
