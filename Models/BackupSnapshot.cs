using System.Text.Json.Serialization;

namespace CoffeeTracker.Models;

/// <summary>
/// Cliché local complet des données, pris automatiquement avant chaque opération de synchro
/// susceptible de modifier le stockage (pull). Sert de filet de sécurité : on peut toujours
/// revenir à un état antérieur si une synchro a mal tourné, même hors ligne.
/// </summary>
public class BackupSnapshot
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Raison de la capture (ex. « avant pull », « avant restauration »).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Nombre total d'enregistrements au moment du cliché (pour affichage rapide).</summary>
    public int RecordCount { get; set; }

    /// <summary>Backup complet sérialisé (<see cref="Lib.CoffeeBackup"/>).</summary>
    public string Json { get; set; } = string.Empty;
}
