using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

public class Coffee
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }
    public string Roaster { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? Farm { get; set; }
    public string? Producer { get; set; }
    public string? Altitude { get; set; }
    public string? Variety { get; set; }
    public Process Process { get; set; } = Process.Washed;
    public string? ProcessNotes { get; set; }
    public bool IsDecaf { get; set; }
    public bool IsOwned { get; set; } = true;
    public bool IsBlend { get; set; }
    public string? Composition { get; set; }
    public DateOnly? RoastDate { get; set; }
    public RoastLevel? RoastLevel { get; set; }
    public int? WeightGrams { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; } = "EUR";
    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? TastingNotes { get; set; }
    public string? PhotoDataUrl { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
