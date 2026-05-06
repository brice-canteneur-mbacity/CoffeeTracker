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
  origine (pays/région/ferme/producteur/altitude/variété), photo, notes
- **Dégustés en boutique** : version allégée, juste nom + métadonnées clés
- **Blends** : champ Composition libre (ex : « 60% Brésil natural, 40% Éthiopie washed »)
- **Décaféiné** avec **méthode** optionnelle (Swiss Water, Mountain Water, CO₂ supercritique,
  EA / sucre de canne, MC / dichlorométhane, Autre)
- **Stock restant calculé** automatiquement : `WeightGrams + StockAdjustmentG − Σ(Brew.DoseG)`
- **Ajustement manuel du stock** : bouton « Ajuster le stock » → modal qui demande le poids réel
  pesé du sac, on calcule le delta absolu et on persiste
- **Tracker de dégazage** : timeline visuelle 0–42 j avec ta position actuelle dans la fenêtre
- **Coût par tasse** : (prix sac / poids) × dose moyenne

### ☕ Brews (préparations maison)
- Méthode (Espresso, V60, AeroPress, Chemex, French press, Moka, Kalita, Origami, Cold brew…)
- Recette : dose, ratio cible (chips presets `1:1.5` à `1:17`), yield (auto-calculé), mouture
- **Type de lait** optionnel (Entier / Demi-écrémé / Écrémé / Sans lactose / Avoine / Amande /
  Soja / Coco / Riz / Autre)
- Score 5 ⭐ + favori ❤
- Lien vers la **machine** et le **moulin** utilisés (filtré par méthode du brew)
- Bouton **« Refaire ce brew »** qui duplique la recette avec date et heure du jour
- **Vue comparaison** côte-à-côte de 2 brews (différences surlignées)
- **Minuteur** dédié avec animation tasse SVG qui se remplit
- Stats par café : moyenne, évolution chronologique

### 🏪 Coffee shops & visites
**Modèle relationnel** : un Shop (lieu) **a** plusieurs visites. Chaque visite capture seulement
ce qui change d'une fois à l'autre (boisson, café, score, prix), pas l'adresse ni les coords.
- **Liste des Shops** avec note moyenne, nb de visites, dernière visite
- **Détail Shop** : infos partagées (adresse, coords, photo, notes) + liste des visites
- **Recherche en ligne** lors de la création d'un shop : Google Places (New) si une clé API
  est configurée, sinon **OpenStreetMap (Photon)** par défaut. Pré-remplit nom/ville/pays/
  adresse/lat/long et conserve un `ExternalPlaceId` pour traçabilité.
- **Type de lait** sur chaque visite (mêmes choix que les brews)
- **Vue carte** Leaflet avec 1 marker par shop coloré selon la note moyenne, fond CARTO Positron
- Recherche full-text sur nom / ville / pays / adresse

### 🔧 Machines
- CRUD du matos : Espresso, V60, AeroPress, Chemex, French press, Moka, Kalita, Origami,
  Cold brew, Moulin, Bouilloire, Balance, Autre
- Marque, modèle, date d'achat, prix, photo, notes
- Sélectionnables depuis le formulaire de brew (filtrées automatiquement par méthode)

### 📊 Stats
- Compteurs cafés / brews / shops / visites (distincts)
- Activité 7j / 30j
- **Heatmap calendrier** style GitHub (13 dernières semaines)
- Méthode favorite, origine la plus achetée, meilleur café (≥ 2 brews)
- Note moyenne brews / visites
- Budget : total dépensé en sacs, coût moyen par tasse, total visites en shop
- Répartition par méthode (barres horizontales)

### 🔔 Rappels
- Notifications à l'ouverture de l'app : stock bas (≤ 30 g), sac vide, café au-delà du pic
  de dégazage (> 35 j) — l'**ajustement manuel** est intégré dans la formule de stock
- En in-app (snackbars) + notifications système si activées + permission accordée

### ☁️ Synchronisation
- **GitHub Gist privé** : configure un Personal Access Token (scope `gist` uniquement) →
  l'app crée un Gist privé qui contient l'export JSON, auto-pull au démarrage,
  bouton « Synchroniser maintenant » sur demande, **auto-push debouncé** après chaque écriture
- 100 % gratuit, pas de backend, historique versionné par GitHub
- **Auto-recovery** : si le Gist stocké localement est introuvable (suppression, changement
  de PAT), l'app cherche un autre Gist dans le compte avant de créer

### 💾 Sauvegarde locale
- Export / import JSON manuel (format **v4** : Coffees + Brews + Shops + ShopVisits + Machines)
- Bouton sync visible **sur chaque page** (header sticky), animation rotation pendant la sync,
  tooltip relatif (« Synchronisé il y a 3 min ») et icône d'erreur en cas de problème

### 🛠️ Diagnostic & migrations
- **MigrationService** idempotent au démarrage : reconstruit les Shops à partir des visites
  legacy (DB pré-v5)
- Section « Diagnostic & maintenance » dans Réglages : bouton **Diagnostiquer** (compte les
  visites pending / orphelines), bouton **Reconstruire les shops** (relance manuelle avec
  affichage de l'erreur complète si échec)

### 🎨 Apparence
- Mode clair / sombre / suivi du système (palette café cohérente dans les deux modes)
- Variables CSS `--coffee-*` partagées entre MudBlazor et le CSS custom
- Icônes app dédiées (grain de café crème sur fond coffee-800) — favicon, PWA install, splash

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
├── App.razor                       # Routeur, MudProviders, init Theme + Sync
├── CoffeeTheme.cs                  # Palette MudBlazor light + dark
├── Program.cs                      # DI registrations
├── Models/
│   ├── Coffee.cs                   # Sac (possédé) ou café dégusté + ajustement stock
│   ├── Brew.cs                     # Préparation maison + type de lait
│   ├── Shop.cs                     # Coffee shop (entité, depuis v5) avec coords
│   ├── CoffeeShopVisit.cs          # Visite ponctuelle attachée à un Shop par FK
│   ├── Machine.cs                  # Matos (espresso, moulin, etc.)
│   └── Enums.cs                    # Process, RoastLevel, BrewMethod, MachineType,
│                                   # MilkType, DecafProcess
├── Data/
│   └── CoffeeDb.cs                 # Schéma BlazorDexie (5 stores), versions 1→6
├── Lib/                            # Services métier
│   ├── Format.cs                   # Helpers d'affichage (date, prix, ratio…)
│   ├── Degassing.cs                # Calcul fenêtre optimale dégazage
│   ├── PlaceSearchService.cs       # Wrapper recherche Google/OSM
│   ├── ThemeService.cs             # Préférence light/dark + persistance
│   ├── SyncService.cs              # GitHub Gist push/pull + auto-push debouncé + recovery
│   ├── AlertsService.cs            # Évaluation règles + notifications
│   ├── MigrationService.cs         # Migrations data idempotentes au démarrage
│   └── CoffeeBackup.cs             # DTO export/import (version 4)
├── Pages/                          # Routes Blazor
│   ├── Coffees.razor               # @ "/" et "/coffees" — 3 onglets (En cours/Terminés/Dégustés)
│   ├── CoffeeDetail.razor          # Fiche complète + brews + dégazage + stock
│   ├── CoffeeForm.razor            # CRUD café (switch possédé/dégusté/blend, + méthode décaf)
│   ├── Brews.razor                 # Liste avec recherche + filtre favoris + Refaire icône
│   ├── BrewForm.razor              # CRUD brew avec ratio chips + machines + lait
│   ├── BrewTimer.razor             # Minuteur SVG animé (route /brews/timer)
│   ├── Shops.razor                 # Liste des shops + carte Leaflet (toggle)
│   ├── ShopDetail.razor            # Infos shop + liste de toutes ses visites
│   ├── ShopForm.razor              # CRUD shop seul (avec recherche en ligne)
│   ├── VisitForm.razor             # CRUD visite — /shops/{id}/visits/new ou /visits/{id}/edit
│   ├── Machines.razor              # Liste matos
│   ├── MachineForm.razor           # CRUD machine
│   ├── Stats.razor                 # Compteurs, heatmap, budget, répartitions
│   └── Settings.razor              # Apparence, sync, sauvegarde, recherche shops, notifications,
│                                   # diagnostic & maintenance, zone dangereuse
├── Layout/
│   ├── MainLayout.razor            # Layout + déclenche migrations + alertes au démarrage
│   └── BottomNav.razor             # 5 onglets (Cafés/Brews/Machines/Shops/Stats)
├── Shared/                         # Composants réutilisables
│   ├── PageHeader.razor            # Header sticky + bouton sync visible partout + back
│   ├── StarRating.razor            # Wrapper MudRating en couleur ambre
│   ├── DetailRow.razor             # Ligne label/valeur dans les fiches
│   ├── Counter.razor               # Petit indicateur stat
│   ├── HeatmapCalendar.razor       # Calendrier 7×N type GitHub
│   ├── PlaceSearchDialog.razor     # Dialog Google/OSM
│   ├── BrewCompareDialog.razor     # Comparaison côte-à-côte de 2 brews
│   └── StockAdjustmentDialog.razor # Modal ajustement stock (« stock réel pesé »)
├── tools/
│   └── generate-icons.ps1          # Génère favicon/icon-192/icon-512 (System.Drawing)
└── wwwroot/
    ├── index.html                  # Entrée + chargement scripts
    ├── manifest.webmanifest        # PWA manifest
    ├── service-worker.js
    ├── favicon.png / icon-192.png / icon-512.png  # Grain de café sur coffee-800
    ├── css/app.css                 # Variables --coffee-* (light/dark) + custom styles
    └── js/                         # Helpers JS (interop)
        ├── imageHelper.js          # Compression photo via canvas + download blob JSON
        ├── theme.js                # Préférence light/dark
        ├── notifications.js        # Wrapper Notification API
        ├── shopSearch.js           # Google Places + Photon
        └── shopsMap.js             # Leaflet (markers shop-centric, popups, fitBounds)
```

### Modèle de données (5 stores IndexedDB)

```
Coffee (id auto-inc)
├── Roaster, Name, Country/Region/Farm/Producer/Altitude/Variety
├── Process, ProcessNotes, RoastLevel, RoastDate
├── IsDecaf, DecafMethod, IsBlend (+ Composition), IsOwned
├── WeightGrams, Price, Currency, PurchaseDate, FinishedAt   ← uniquement si IsOwned
├── StockAdjustmentG                                          ← ajustement manuel cumulé (g)
├── TastingNotes, PhotoDataUrl
└── CreatedAt, UpdatedAt

Brew (id auto-inc)
├── CoffeeId → Coffee.Id (obligatoire, parmi les IsOwned)
├── Date, Method
├── DoseG, YieldG, Ratio, GrindSize
├── Notes, Rating, IsFavorite
├── BrewerMachineId → Machine.Id (optionnel)
├── GrinderId → Machine.Id (optionnel)
├── Milk (MilkType?, null = sans lait)
└── CreatedAt

Shop (id auto-inc)                                            ← entité depuis v5
├── Name, City, Country, Address
├── Latitude, Longitude, ExternalPlaceId   ← optionnels (recherche en ligne)
├── PhotoDataUrl, Notes
└── CreatedAt, UpdatedAt

CoffeeShopVisit (id auto-inc)
├── ShopId → Shop.Id (obligatoire ; 0 = visite legacy non migrée)
├── Date, DrinkType, Notes, Rating
├── CoffeeId → Coffee.Id (optionnel) — auto-créé si nom inconnu
├── CoffeeOrigin (texte libre fallback)
├── Price, Currency, PhotoDataUrl
├── Milk (MilkType?)
└── CreatedAt
   ─── Champs legacy (pré-v5) lus uniquement par MigrationService :
       ShopName, City, Country, Address, Latitude, Longitude, ExternalPlaceId

Machine (id auto-inc)
├── Name, Type (Espresso/V60/AeroPress/.../Grinder/Kettle/Scale/Other)
├── Brand, Model, PurchaseDate, Price, Currency
├── PhotoDataUrl, Notes
└── CreatedAt, UpdatedAt
```

### Versions du schéma

| Version | Changement |
|---|---|
| 1–2 | Init (Coffee, Brew, CoffeeShopVisit) |
| 3 | Ajout `Machines` + champs lat/long/address sur visit |
| 4 | Ajout `DecafProcess` + `StockAdjustmentG` sur Coffee (champs nullables, pas de migration data) |
| 5 | Extraction de `Shop` comme entité distincte (migration data idempotente dans `MigrationService`) |
| 6 | Ajout `MilkType?` sur Brew et CoffeeShopVisit (nullable, pas de migration data) |

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

### Régénérer les icônes

```powershell
pwsh -File tools/generate-icons.ps1
```

Génère favicon.png (64×64), icon-192.png et icon-512.png à partir du design intégré au script
(grain de café crème sur fond coffee-800). Pas de dépendance externe — utilise `System.Drawing`.

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

Voir [`ROADMAP.md`](ROADMAP.md) pour le détail des items implémentés.

---

## Licence

MIT — utilise, modifie, redistribue.

---

## Notes

- Construit avec [Claude Code](https://claude.com/claude-code) en pair-programming.
- Données 100 % locales (IndexedDB par origine + appareil) — la sync GitHub Gist est le seul mécanisme
  multi-appareils.
- L'app est en français (UX, libellés). Les enums internes sont en anglais.
- Tout le code C# est documenté en XML doc-comments ; chaque page Razor a un commentaire de tête
  expliquant son rôle et ses routes.
