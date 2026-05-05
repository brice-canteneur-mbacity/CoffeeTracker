using BlazorDexie.Database;
using BlazorDexie.Options;
using CoffeeTracker.Models;

namespace CoffeeTracker.Data;

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
    public CoffeeDb(BlazorDexieOptions options)
        : base("coffee-tracker", 5, Array.Empty<IDbVersion>(), options)
    {
    }
}
