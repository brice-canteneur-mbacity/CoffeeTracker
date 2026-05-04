using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

public class Brew
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }
    public int CoffeeId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public BrewMethod Method { get; set; } = BrewMethod.Espresso;
    public decimal? DoseG { get; set; }
    public decimal? YieldG { get; set; }
    public string? Ratio { get; set; }
    public string? GrindSize { get; set; }
    public string? Notes { get; set; }
    public int Rating { get; set; }
    public bool IsFavorite { get; set; }
    public int? BrewerMachineId { get; set; }
    public int? GrinderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
