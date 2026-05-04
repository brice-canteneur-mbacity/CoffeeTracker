using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

public class Machine
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MachineType Type { get; set; } = MachineType.Espresso;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; } = "EUR";
    public string? PhotoDataUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
