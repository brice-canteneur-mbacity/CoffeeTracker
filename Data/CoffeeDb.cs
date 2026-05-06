using BlazorDexie.Database;
using BlazorDexie.Options;
using CoffeeTracker.Models;

namespace CoffeeTracker.Data;

/// <summary>
/// Schéma BlazorDexie / IndexedDB de l'application. Chaque <see cref="Store{T,TKey}"/> ci-dessous
/// se traduit en object store IndexedDB avec auto-incrément (++Id) et les indexes secondaires
/// listés (utilisés pour les requêtes <c>.Where(...)</c>).
/// </summary>
/// <remarks>
/// Toute évolution de schéma (ajout d'un store, ajout/suppression d'un index, etc.) DOIT
/// s'accompagner d'un bump du numéro de version dans le constructeur. Les changements purement
/// JSON-shape (ajout d'un champ optionnel sur un model) ne nécessitent techniquement pas de bump
/// mais on en fait un par convention pour tracer chaque évolution.
/// </remarks>
public class CoffeeDb : Db<CoffeeDb>
{
    public Store<Coffee, int> Coffees { get; set; } =
        new("++Id", nameof(Coffee.Roaster), nameof(Coffee.Name), nameof(Coffee.PurchaseDate),
            nameof(Coffee.FinishedAt), nameof(Coffee.CreatedAt));

    public Store<Brew, int> Brews { get; set; } =
        new("++Id", nameof(Brew.CoffeeId), nameof(Brew.Date), nameof(Brew.Method),
            nameof(Brew.Rating), nameof(Brew.CreatedAt));

    public Store<CoffeeShopVisit, int> ShopVisits { get; set; } =
        new("++Id", nameof(CoffeeShopVisit.ShopId), nameof(CoffeeShopVisit.Date),
            nameof(CoffeeShopVisit.Rating), nameof(CoffeeShopVisit.CreatedAt));

    public Store<Shop, int> Shops { get; set; } =
        new("++Id", nameof(Shop.Name), nameof(Shop.City), nameof(Shop.CreatedAt));

    public Store<Machine, int> Machines { get; set; } =
        new("++Id", nameof(Machine.Name), nameof(Machine.Type), nameof(Machine.CreatedAt));

    // Versions :
    //   1-2 : init (Coffee, Brew, CoffeeShopVisit)
    //   3   : ajout Machines + champs lat/long/address sur visit
    //   4   : ajout DecafProcess + StockAdjustmentG sur Coffee (champs nullables : pas de migration data)
    //   5   : extraction de Shop comme entité distincte (migration data dans MigrationService)
    //   6   : ajout MilkType (nullable) sur Brew et CoffeeShopVisit — pas de migration data
    public CoffeeDb(BlazorDexieOptions options)
        : base("coffee-tracker", 6, Array.Empty<IDbVersion>(), options)
    {
    }
}
