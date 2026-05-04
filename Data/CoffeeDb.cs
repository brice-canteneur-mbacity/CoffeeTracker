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
        new("++Id", nameof(CoffeeShopVisit.ShopName), nameof(CoffeeShopVisit.City),
            nameof(CoffeeShopVisit.Date), nameof(CoffeeShopVisit.Rating),
            nameof(CoffeeShopVisit.CreatedAt));

    public Store<Machine, int> Machines { get; set; } =
        new("++Id", nameof(Machine.Name), nameof(Machine.Type), nameof(Machine.CreatedAt));

    public CoffeeDb(BlazorDexieOptions options)
        : base("coffee-tracker", 3, Array.Empty<IDbVersion>(), options)
    {
    }
}
