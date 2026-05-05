using BlazorDexie.Extensions;
using CoffeeTracker;
using CoffeeTracker.Data;
using CoffeeTracker.Lib;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddBlazorDexie();
builder.Services.AddScoped<CoffeeDb>();
builder.Services.AddScoped<PlaceSearchService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<AlertsService>();
builder.Services.AddScoped<MigrationService>();

await builder.Build().RunAsync();
