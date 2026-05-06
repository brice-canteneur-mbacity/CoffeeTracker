using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Microsoft.JSInterop;

namespace CoffeeTracker.Lib;

/// <summary>Niveau de sévérité d'un rappel — mappé vers MudBlazor Severity côté UI.</summary>
public enum AlertSeverity { Info, Warning, Error }

/// <summary>Rappel évalué : titre court + message + lien optionnel vers un café.</summary>
public record CoffeeAlert(AlertSeverity Severity, string Title, string Message, int? CoffeeId);

/// <summary>
/// Évalue les règles de rappels (stock bas, hors fenêtre de dégazage…) au démarrage de l'app.
/// Combine snackbars in-app + notifications système si la permission est accordée et activée.
/// </summary>
public class AlertsService(CoffeeDb db, IJSRuntime js, LocalizationService loc)
{
    private readonly CoffeeDb _db = db;
    private readonly IJSRuntime _js = js;
    private readonly LocalizationService _loc = loc;

    /// <summary>
    /// Parcourt les cafés possédés non terminés et applique deux règles :
    /// (1) stock ≤ 0 ou ≤ 30 g (en intégrant <see cref="Coffee.StockAdjustmentG"/>),
    /// (2) jours depuis torréfaction &gt; 35 j (au-delà du pic de dégazage).
    /// Les cafés terminés ou non possédés sont ignorés.
    /// </summary>
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
                        _loc.T("alerts.empty_bag_title"),
                        _loc.T("alerts.empty_bag_msg", c.Name),
                        c.Id));
                }
                else if (remaining <= 30m)
                {
                    alerts.Add(new CoffeeAlert(
                        AlertSeverity.Warning,
                        _loc.T("alerts.low_stock_title"),
                        _loc.T("alerts.low_stock_msg", c.Name,
                            remaining.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)),
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
                        _loc.T("alerts.past_peak_title"),
                        _loc.T("alerts.past_peak_msg", c.Name, deg.Days),
                        c.Id));
                }
            }
        }

        return alerts;
    }

    // ─── Wrappers fins autour de l'API Notifications du navigateur (cf. notifications.js) ───

    /// <summary>True si l'API Notifications est supportée par le navigateur.</summary>
    public async Task<bool> IsSupportedAsync()
        => await _js.InvokeAsync<bool>("coffeeNotifications.isSupported");

    /// <summary>"granted" / "denied" / "default" — état actuel de la permission navigateur.</summary>
    public async Task<string> GetPermissionAsync()
        => await _js.InvokeAsync<string>("coffeeNotifications.permission");

    /// <summary>Demande la permission au navigateur (peut afficher un prompt). Retourne true si "granted".</summary>
    public async Task<bool> RequestPermissionAsync()
        => await _js.InvokeAsync<bool>("coffeeNotifications.requestPermission");

    /// <summary>Préférence utilisateur (localStorage) — indépendante de la permission navigateur.</summary>
    public async Task<bool> IsEnabledAsync()
        => await _js.InvokeAsync<bool>("coffeeNotifications.isEnabled");

    /// <summary>Persiste la préférence utilisateur d'envoyer ou non des notifs système.</summary>
    public async Task SetEnabledAsync(bool enabled)
        => await _js.InvokeVoidAsync("coffeeNotifications.setEnabled", enabled);

    /// <summary>Affiche une notification système. À n'appeler que si <see cref="IsEnabledAsync"/> et <see cref="GetPermissionAsync"/>=="granted".</summary>
    public async Task ShowSystemAsync(CoffeeAlert alert)
        => await _js.InvokeVoidAsync("coffeeNotifications.show", alert.Title, alert.Message);
}
