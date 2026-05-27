# Coffee Tracker — Roadmap

Items classés par ordre de priorité. ☑ = fait, ▶ = en cours, ⬜ = à faire.

## Robustesse / sécurité des données

- ☑ **1. Export / import JSON** — sauvegarde manuelle du contenu IndexedDB. Critique tant qu'on n'a pas de cloud
- ☑ **5. Recherche dans les listes** — utile dès 50+ cafés ou 200+ brews

## Workflow quotidien

- ☑ **2. Timer intégré au formulaire de brew** — démarre/arrête, remplit auto le champ temps
- ☑ **3. Bouton "refaire ce brew"** — duplique une recette en pré-remplissant les champs
- ☑ **4. Stock restant dans le sac** — chaque brew retire automatiquement la dose, alerte quand bas
- ☑ **7. Marquer un brew comme favori** — pour vite retrouver ses meilleures recettes

## Features coffee-geek

- ☑ **6. Tracker de dégazage** — fenêtre optimale selon date torréfaction et méthode (espresso vs filter)
- ☑ **8. Calculateur de ratio interactif** — change la dose, le yield s'ajuste auto selon ratio cible
- ☑ **11. TDS / extraction yield** — pour les utilisateurs de réfractomètre
- ☑ **12. Heatmap calendrier** — visualisation type GitHub de l'activité brew (13 dernières sem.)

## Stats avancées

- ☑ **9. Évolution du score d'un café au fil des brews** — graphe pour voir si on tune bien ses recettes
- ☑ **10. Coût moyen par tasse** — (prix sac / poids) × dose moyenne
## Nouveau matériel

- ☑ **13. Liste de machines à café** — CRUD du matos (machines espresso, moulins, bouilloires, balances) avec **lien aux brews** (sélection optionnelle de la machine espresso + moulin sur le formulaire brew). Nouvel **onglet « Machines »** dans la bottom nav (5 onglets). Schéma DB bumped à v2.

## Recherche en ligne

- ☑ **14. Recherche shop par nom (Google + OSM fallback)** — bouton « Rechercher en ligne » sur le formulaire shop, dialog avec MudAutocomplete. Google Places (New) si clé API présente dans Réglages, sinon Photon/OpenStreetMap. CoffeeShopVisit enrichi avec Address, Latitude, Longitude, ExternalPlaceId. Schéma DB bumped à v3, export JSON v3.

## Déploiement

- ☑ **15. Déployer l'app sur GitHub Pages** — workflow `.github/workflows/deploy.yml` qui à chaque push sur `master` : publish Blazor WASM, patch `<base href="/CoffeeTracker/">`, génère `404.html` (fallback SPA) et `.nojekyll`, deploy via `actions/deploy-pages@v4`. URL prod : `https://brice-canteneur-mbacity.github.io/CoffeeTracker/`

## Multi-appareil

- ☑ **16. Sync entre appareils via GitHub Gist** — Réglages → champ Personal Access Token GitHub (scope `gist`). L'app crée un Gist privé au premier sync, push/pull du JSON complet (cafés, brews, visites, machines). ⚠️ Comportement initial (auto-pull au démarrage + last-write-wins destructif) **revu en profondeur**, cf. item 20.

## UX & confort

- ☑ **17. Mode sombre** — `PaletteDark` ajouté à `CoffeeTheme`, `ThemeService` gère la préférence (système / clair / sombre) en localStorage, App.razor binde `IsDarkMode` sur `MudThemeProvider`. CSS variables coffee-* pour les composants custom (BottomNav, PageHeader, popups Leaflet, marqueurs, etc.). Toggle radio dans Réglages.
- ☑ **18. Notifications PWA proactives** — `AlertsService` évalue les règles à l'ouverture de l'app (stock ≤ 30 g, sac vide, café > 35 j depuis torréfaction). Affichage en MudSnackbar in-app + notification système si l'utilisateur a activé l'option et accordé la permission navigateur. Pas de push à distance (impossible sans backend), mais effective dès que l'app est ouverte.

## Features coffee-geek (suite)

- ☑ **19. Vue comparaison de brews côte-à-côte** — sur la fiche d'un café, cases à cocher sur chaque brew. Bouton « Comparer X/2 » disponible dès 2 sélectionnés → ouverture d'un `BrewCompareDialog` qui affiche les deux brews en colonnes avec les écarts surlignés.

## Robustesse données — refonte de la sync (PR #8)

- ▶ **20. Sync Gist : sauvegarde sans perte + manuelle uniquement** — refonte suite à une **perte de données** : l'ancien auto-pull au démarrage faisait `Clear()` sur les tables locales puis réinjectait le Gist, donc un Gist vide/ancien/illisible écrasait le local sain.
  - **Pull non destructif** : `MergeBackupAsync` fusionne **par Id** (upsert) au lieu de `Clear()` + réinjection ; aucun enregistrement local n'est jamais supprimé. Sur conflit, le plus récent gagne (`UpdatedAt` si dispo — Coffee/Shop/Machine —, sinon `CreatedAt` — Brew/CoffeeShopVisit).
  - **Garde anti-vide** : un Gist vide n'écrase plus un local non vide (le push restaure alors le cloud).
  - **Clichés de sécurité locaux** : nouveau store IndexedDB `Snapshots` (modèle `Models/BackupSnapshot.cs`), cliché complet pris avant chaque pull, 10 derniers conservés. Restauration depuis **Réglages → Clichés de sécurité** (annulable, prend un cliché de l'état courant avant de restaurer). **Schéma DB bumpé v7 → v8.**
  - **Synchro 100 % manuelle** : suppression de l'auto-pull au démarrage (et de la découverte réseau du Gist dans `InitializeAsync`) et du push debounced (`RequestPush` retiré du `SyncService`, de tous les formulaires et de `MigrationService`). Seule entrée : le bouton « Synchroniser » (header + Réglages) → `SyncAsync` = pull (merge) puis push.
  - Textes Réglages reformulés (fr/en/it) : « Sauvegarde cloud », rappel que rien n'est envoyé automatiquement.
  - **Fichiers clés** : `Lib/SyncService.cs`, `Models/BackupSnapshot.cs`, `Data/CoffeeDb.cs` (store + version 8), `Pages/Settings.razor` (UI clichés), `wwwroot/i18n/{fr,en,it}.json`.
  - **⚠️ Reste à faire (prochaine session)** :
    - **Compiler** (`dotnet build`) et corriger d'éventuelles erreurs — le code n'a **pas pu être compilé** dans l'environnement web (pas de SDK .NET). Points à vérifier en priorité : l'API BlazorDexie utilisée (`Store<T,int>.Get/Put/Add/Delete/OrderBy`), le pattern matching de `GetId<T>`, le merge générique `MergeStoreAsync<T>`.
    - **Tester en navigateur** : démarrage sans appel réseau, bouton sync (pull+push), Gist vide + local non vide, création/restauration de cliché, absence de push auto après une saisie.
    - Vérifier la **migration de schéma v7 → v8** (création du store `Snapshots`) sur une base existante.
    - Merger la PR #8 une fois validée.

---

**Hors scope MVP, à reconsidérer plus tard :**
- Cupping form structuré SCA (acidité/corps/sucrosité/finale en sliders)
- Scan photo de paquet via LLM vision (coût API, abandonné)
- Publication app stores (PWABuilder)
