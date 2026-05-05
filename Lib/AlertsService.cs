using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

public enum AlertSeverity { Info, Warning, Error }

public record CoffeeAlert(AlertSeverity Severity, string Title, string Message, int? CoffeeId);

/// <summary>
/// Évalue les règles de rappels (stock bas, hors fenêtre de dégazage…) au démarrage de l'app.
/// Combine snackbars in-app + notifications système si la permission est accordée et activée.
/// </summary>
public class AlertsService(CoffeeDb db, IJSRuntime js)
{
    private readonly CoffeeDb _db = db;
    private readonly IJSRuntime _js = js;

    public async Task<List<CoffeeAlert>> EvaluateAsync()
    {
        var alerts = new List<CoffeeAlert>();
        var coffees = await _db.Coffees.ToCollection().ToList();
        var brews = await _db.Brews.ToCollection().ToList();

        var brewsByCoffee = brews
            .GroupBy(b => b.CoffeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var c in coffees.Where(x => x.IsOwned && x.FinishedAt is null))
        {
            // Règle 1 : stock bas / vide
            if (c.WeightGrams is int total && total > 0)
            {
                var used = brewsByCoffee.TryGetValue(c.Id, out var bs)
                    ? bs.Sum(b => b.DoseG ?? 0m)
                    : 0m;
                // Inclut l'ajustement manuel : Stock = Weight + adjustment - used.
                var remaining = total + c.StockAdjustmentG - used;
                if (remaining <= 0m)
                {
                    alerts.Add(new CoffeeAlert(
                        AlertSeverity.Error,
                        "Sac vide",
                        $"« {c.Name} » est à 0 g — pense à le marquer terminé.",
                        c.Id));
                }
                else if (remaining <= 30m)
                {
                    alerts.Add(new CoffeeAlert(
                        AlertSeverity.Warning,
                        "Stock bas",
                        $"« {c.Name} » : {remaining.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} g restants.",
                        c.Id));
                }
            }

            // Règle 2 : hors fenêtre optimale de dégazage
            if (c.RoastDate is not null)
            {
                var deg = Degassing.Compute(c.RoastDate);
                if (deg is not null && deg.Days > 35)
                {
                    alerts.Add(new CoffeeAlert(
                        AlertSeverity.Warning,
                        "Au-delà du pic",
                        $"« {c.Name} » : {deg.Days} j depuis torréfaction.",
                        c.Id));
                }
            }
        }

        return alerts;
    }

    public async Task<bool> IsSupportedAsync()
        => await _js.InvokeAsync<bool>("coffeeNotifications.isSupported");

    public async Task<string> GetPermissionAsync()
        => await _js.InvokeAsync<string>("coffeeNotifications.permission");

    public async Task<bool> RequestPermissionAsync()
        => await _js.InvokeAsync<bool>("coffeeNotifications.requestPermission");

    public async Task<bool> IsEnabledAsync()
        => await _js.InvokeAsync<bool>("coffeeNotifications.isEnabled");

    public async Task SetEnabledAsync(bool enabled)
        => await _js.InvokeVoidAsync("coffeeNotifications.setEnabled", enabled);

    public async Task ShowSystemAsync(CoffeeAlert alert)
        => await _js.InvokeVoidAsync("coffeeNotifications.show", alert.Title, alert.Message);
}
