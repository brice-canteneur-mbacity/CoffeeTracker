# ☕ Coffee Tracker

PWA personnelle pour suivre cafés achetés, préparations maison, visites de coffee shops et matériel.

**Live demo** : https://brice-canteneur-mbacity.github.io/CoffeeTracker/

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor)](https://learn.microsoft.com/aspnet/core/blazor/)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-9.4-594AE2)](https://mudblazor.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Pourquoi ?

J'achète des cafés de spécialité, j'en bois en boutique, j'expérimente des recettes à la maison.
Aucune app n'embrassait à la fois mes sacs au frigo, mes brews, mes visites de shops et mon matos.
Donc j'en ai construit une — en C# parce que j'aime mieux que React, et statique parce que je ne veux
pas payer d'infra.

## Fonctionnalités

### 📦 Cafés
- **Possédés** (sacs) avec poids, prix, date d'achat, niveau de torréfaction, process,
  origine (pays/région/ferme/producteur/altitude/variété), variété, photo, notes
- **Dégustés en boutique** : version allégée, juste nom + métadonnées clés
- **Blends** : champ Composition libre (ex : « 60% Brésil natural, 40% Éthiopie washed »)
- **Décaféiné** : flag séparé
- **Stock restant calculé** automatiquement : poids initial − somme des doses des brews
- **Tracker de dégazage** : timeline visuelle 0–42 j avec ta position actuelle dans la fenêtre
- **Coût par tasse** : (prix sac / poids) × dose moyenne

### ☕ Brews (préparations maison)
- Méthode (Espresso, V60, AeroPress, Chemex, French press, Moka, Kalita, Origami, Cold brew…)
- Recette : dose, ratio cible (chips presets `1:1.5` à `1:17`), yield (auto-calculé), mouture
- Score 5 ⭐ + favori ❤
- Lien vers la **machine** et le **moulin** utilisés (filtré par méthode du brew)
- Bouton **« Refaire ce brew »** qui duplique avec date du jour
- **Vue comparaison** côte-à-côte de 2 brews (différences surlignées)
- Stats par café : moyenne, évolution chronologique, etc.

### 🏪 Coffee shops visités
- Nom du shop + ville + pays + adresse (avec autocomplétion sur tes shops déjà visités →
  réutilise les coordonnées)
- **Recherche en ligne** intégrée : Google Places (New) si tu fournis une clé API,
  sinon **OpenStreetMap (Photon)** par défaut. Pré-remplit nom/ville/pays/adresse/lat/long.
- Date, type de boisson, café dégusté (autocomplétion sur tes cafés bibliothèque),
  prix, score, photo, notes
- **Vue carte** Leaflet avec marqueurs colorés selon la note, fond CARTO Positron épuré

### 🔧 Machines
- CRUD du matos : Espresso, V60, AeroPress, Chemex, French press, Moka, Kalita, Origami,
  Cold brew, Moulin, Bouilloire, Balance, Autre
- Marque, modèle, date d'achat, prix, photo, notes
- Sélectionnables depuis le formulaire de brew (filtrées par méthode)

### 📊 Stats
- Compteurs cafés / brews / shops
- Activité 7j / 30j
- **Heatmap calendrier** style GitHub (13 dernières semaines)
- Méthode favorite, origine la plus achetée, meilleur café (≥ 2 brews)
- Note moyenne brews / shops
- Budget : total dépensé en sacs, coût moyen par tasse, total shops
- Répartition par méthode (barres)

### 🔔 Rappels
- Notifications à l'ouverture de l'app : stock bas (≤ 30 g), sac vide, café au-delà du pic
  de dégazage (> 35 j)
- En in-app (snackbars) + notifications système si activées + permission accordée

### ☁️ Synchronisation
- **GitHub Gist privé** : configure un Personal Access Token (scope `gist` uniquement) →
  l'app crée un Gist privé qui contient l'export JSON, auto-pull au démarrage,
  bouton « Synchroniser maintenant » sur demande
- 100 % gratuit, pas de backend, historique versionné par GitHub

### 💾 Sauvegarde locale
- Export / import JSON manuel (pour backup ponctuel ou migration entre origines différentes)

### 🎨 Apparence
- Mode clair / sombre / suivi du système (palette café cohérente dans les deux modes)

---

## Stack technique

| Couche | Choix |
|---|---|
| Front | **Blazor WebAssembly** (.NET 9), pure client-side (aucun backend) |
| UI | [**MudBlazor 9.4**](https://mudblazor.com/) — composants Material Design natifs |
| Stockage local | [**BlazorDexie 2.2**](https://github.com/simon-kuster/BlazorDexie) (IndexedDB via Dexie.js) |
| Carte | [**Leaflet 1.9**](https://leafletjs.com/) + tuiles **CARTO Positron** |
| Recherche shops | **Google Places API (New)** (optionnel) ou **Photon / OpenStreetMap** |
| Sync | GitHub Gist API (REST) avec Personal Access Token côté client |
| PWA | Service worker généré par le template Blazor WASM, manifest dédié |
| Hébergement | **GitHub Pages** (gratuit, HTTPS, déployé via GitHub Actions) |

### Pourquoi pas de backend ?

Volontairement. Coût zéro, simplicité maximale, données restent chez l'utilisateur.
Inconvénients assumés :
- Pas de vraie auth (la sync utilise le token GitHub de l'utilisateur)
- Pas de push notifications réelles (seulement à l'ouverture de l'app)
- Sync = stratégie last-write-wins, pas de vrai diff

---

## Architecture

```
coffee/
├── App.razor                  # Routeur, ThemeProvider, init des services
├── CoffeeTheme.cs             # Palette MudBlazor light + dark
├── Program.cs                 # DI registrations
├── Models/
│   ├── Coffee.cs              # Sac (possédé) ou café dégusté
│   ├── Brew.cs                # Préparation maison
│   ├── CoffeeShopVisit.cs     # Visite en boutique
│   ├── Machine.cs             # Matos (espresso, moulin, etc.)
│   └── Enums.cs               # Process, RoastLevel, BrewMethod, MachineType
├── Data/
│   └── CoffeeDb.cs            # Schéma BlazorDexie (4 stores), version DB
├── Lib/                       # Services métier
│   ├── Format.cs              # Helpers d'affichage (date, prix, ratio…)
│   ├── Degassing.cs           # Calcul fenêtre optimale dégazage
│   ├── PlaceSearchService.cs  # Wrapper recherche Google/OSM
│   ├── ThemeService.cs        # Préférence light/dark + persistance
│   ├── SyncService.cs         # GitHub Gist push/pull
│   ├── AlertsService.cs       # Évaluation règles + notifications
│   └── CoffeeBackup.cs        # DTO export/import
├── Pages/                     # Routes Blazor
│   ├── Coffees.razor          # Liste 3 onglets (En cours / Terminés / Dégustés)
│   ├── CoffeeDetail.razor     # Fiche complète + brews + dégazage + stock
│   ├── CoffeeForm.razor       # CRUD café (avec switch possédé/dégusté/blend)
│   ├── Brews.razor            # Liste avec recherche + filtre favoris
│   ├── BrewForm.razor         # CRUD brew avec ratio chips + machines
│   ├── Shops.razor            # Liste + carte Leaflet (toggle)
│   ├── ShopForm.razor         # CRUD visite + recherche en ligne
│   ├── Machines.razor         # Liste matos
│   ├── MachineForm.razor      # CRUD machine
│   ├── Stats.razor            # Compteurs, heatmap, budget, répartitions
│   └── Settings.razor         # Apparence, sync, sauvegarde, recherche en ligne, notifications
├── Layout/
│   ├── MainLayout.razor       # Layout + évaluation alertes au démarrage
│   └── BottomNav.razor        # 5 onglets (Cafés/Brews/Machines/Shops/Stats)
├── Shared/                    # Composants réutilisables
│   ├── PageHeader.razor       # Header sticky avec back button + actions
│   ├── StarRating.razor       # Wrapper MudRating en couleur ambre
│   ├── DetailRow.razor        # Ligne label/valeur dans les fiches
│   ├── Counter.razor          # Petit indicateur stat
│   ├── HeatmapCalendar.razor  # Calendrier 7×N type GitHub
│   ├── PlaceSearchDialog.razor # Dialog Google/OSM
│   └── BrewCompareDialog.razor # Comparaison côte-à-côte de 2 brews
└── wwwroot/
    ├── index.html             # Entrée + chargement scripts
    ├── manifest.webmanifest   # PWA manifest
    ├── service-worker.js
    ├── css/app.css            # Variables CSS coffee-* (light/dark) + custom styles
    └── js/                    # Helpers JS (interop)
        ├── imageHelper.js     # Compression photo via canvas
        ├── theme.js           # Préférence light/dark
        ├── notifications.js   # Wrapper Notification API
        ├── shopSearch.js      # Google Places + Photon
        ├── shopsMap.js        # Leaflet (markers, popups, fitBounds)
        └── auth.js            # (legacy, non utilisé)
```

### Modèle de données (4 stores IndexedDB)

```
Coffee (id auto-inc)
├── Roaster, Name, Country/Region/Farm/Producer/Altitude/Variety
├── Process, ProcessNotes, RoastLevel, RoastDate
├── IsDecaf, IsBlend (+ Composition), IsOwned
├── WeightGrams, Price, Currency, PurchaseDate, FinishedAt   ← uniquement si IsOwned
├── TastingNotes, PhotoDataUrl
└── CreatedAt, UpdatedAt

Brew (id auto-inc)
├── CoffeeId → Coffee.Id
├── Date, Method
├── DoseG, YieldG, Ratio, GrindSize
├── Notes, Rating, IsFavorite
├── BrewerMachineId → Machine.Id (optionnel)
├── GrinderId → Machine.Id (optionnel)
└── CreatedAt

CoffeeShopVisit (id auto-inc)
├── ShopName, City, Country, Address
├── Latitude, Longitude, ExternalPlaceId    ← optionnels (recherche en ligne)
├── Date, DrinkType, Notes, Rating
├── CoffeeId → Coffee.Id (optionnel) — auto-créé si nom inconnu
├── CoffeeOrigin (legacy, fallback texte)
├── Price, Currency, PhotoDataUrl
└── CreatedAt

Machine (id auto-inc)
├── Name, Type (Espresso/V60/AeroPress/.../Grinder/Kettle/Scale/Other)
├── Brand, Model, PurchaseDate, Price, Currency
├── PhotoDataUrl, Notes
└── CreatedAt, UpdatedAt
```

---

## Lancer en local

```bash
# Prérequis : .NET 9 SDK
git clone https://github.com/brice-canteneur-mbacity/CoffeeTracker.git
cd CoffeeTracker
dotnet run
```

Ouvre `https://localhost:5001` (ou le port que `dotnet run` affiche).

### Configurer la recherche shops avec Google (optionnel)

1. [Google Cloud Console](https://console.cloud.google.com/) → créer un projet → activer
   **Places API (New)** + **Maps JavaScript API**
2. Créer une API key, restreindre par HTTP referrer :
   - `http://localhost:*`
   - `https://brice-canteneur-mbacity.github.io/*`
3. Pour le dev, copier `wwwroot/devKey.js.example` vers `wwwroot/devKey.js`
   (gitignored) et y coller la clé.
4. En prod, la coller dans **Réglages → Recherche de coffee shops**.

Sans clé, l'app utilise OpenStreetMap (Photon) — fonctionnel mais données moins riches.

---

## Déploiement

GitHub Actions (`.github/workflows/deploy.yml`) déploie sur GitHub Pages à chaque push sur `master` :

1. `dotnet publish` du projet Blazor WASM
2. Patch du `<base href>` pour le sous-chemin `/CoffeeTracker/`
3. Génération de `404.html` (fallback SPA) et `.nojekyll`
4. Upload via `actions/upload-pages-artifact` puis deploy via `actions/deploy-pages@v4`

URL prod : https://brice-canteneur-mbacity.github.io/CoffeeTracker/

### Installer comme PWA

Sur mobile (iOS Safari / Android Chrome) ou desktop : ouvrir l'URL → menu navigateur →
« Sur l'écran d'accueil » / « Installer l'application ». L'app s'installe avec icône, plein écran,
fonctionne hors-ligne (service worker met les assets en cache).

---

## Roadmap

Voir [`ROADMAP.md`](ROADMAP.md) pour le détail. **19 items implémentés**.

---

## Licence

MIT — utilise, modifie, redistribue.

---

## Notes

- Construit avec [Claude Code](https://claude.com/claude-code) en pair-programming.
- Données 100 % locales (IndexedDB par origine + appareil) — la sync GitHub Gist est le seul mécanisme
  multi-appareils.
- L'app est en français (UX, libellés). Les enums internes sont en anglais.
