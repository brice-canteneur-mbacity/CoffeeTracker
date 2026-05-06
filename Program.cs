// Composition root du host Blazor WebAssembly.
// Tout est Scoped : en WASM mono-utilisateur, Scoped == Singleton à l'échelle de l'onglet
// (l'app n'a pas de notion de "request"), donc parfait pour des services qui doivent
// partager leur état (ex : SyncService.LastSync, ThemeService.Preference).

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

// HttpClient pointe sur la base href de l'app — utile pour fetch des assets statiques.
// La sync GitHub Gist crée ses propres HttpClient à part (cf. SyncService).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddBlazorDexie();

// Services applicatifs.
builder.Services.AddScoped<CoffeeDb>();              // Couche d'accès IndexedDB.
builder.Services.AddScoped<PlaceSearchService>();    // Recherche shops Google/OSM (interop JS).
builder.Services.AddScoped<ThemeService>();          // Light/dark + media query.
builder.Services.AddScoped<SyncService>();           // GitHub Gist push/pull + debounced auto-push.
builder.Services.AddScoped<AlertsService>();         // Évaluation rappels (stock bas, dégazage).
builder.Services.AddScoped<MigrationService>();      // Migrations data idempotentes au démarrage.
builder.Services.AddScoped<LocalizationService>();   // i18n FR/EN/IT (chargement JSON dictionaries).

await builder.Build().RunAsync();
