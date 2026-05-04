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

- ☑ **16. Sync entre appareils via GitHub Gist** — Réglages → champ Personal Access Token GitHub (scope `gist`). L'app crée un Gist privé au premier sync, push/pull du JSON complet (cafés, brews, visites, machines). Auto-pull au démarrage + bouton « Synchroniser maintenant » (pull puis push, last-write-wins).

## UX & confort

- ☑ **17. Mode sombre** — `PaletteDark` ajouté à `CoffeeTheme`, `ThemeService` gère la préférence (système / clair / sombre) en localStorage, App.razor binde `IsDarkMode` sur `MudThemeProvider`. CSS variables coffee-* pour les composants custom (BottomNav, PageHeader, popups Leaflet, marqueurs, etc.). Toggle radio dans Réglages.
- ☑ **18. Notifications PWA proactives** — `AlertsService` évalue les règles à l'ouverture de l'app (stock ≤ 30 g, sac vide, café > 35 j depuis torréfaction). Affichage en MudSnackbar in-app + notification système si l'utilisateur a activé l'option et accordé la permission navigateur. Pas de push à distance (impossible sans backend), mais effective dès que l'app est ouverte.

## Features coffee-geek (suite)

- ☑ **19. Vue comparaison de brews côte-à-côte** — sur la fiche d'un café, cases à cocher sur chaque brew. Bouton « Comparer X/2 » disponible dès 2 sélectionnés → ouverture d'un `BrewCompareDialog` qui affiche les deux brews en colonnes avec les écarts surlignés.

---

**Hors scope MVP, à reconsidérer plus tard :**
- Cupping form structuré SCA (acidité/corps/sucrosité/finale en sliders)
- Scan photo de paquet via LLM vision (coût API, abandonné)
- Publication app stores (PWABuilder)
