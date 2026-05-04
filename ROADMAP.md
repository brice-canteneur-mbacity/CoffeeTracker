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

- ⬜ **16. Sync entre appareils via GitHub Gist** — Réglages : champ pour un Personal Access Token GitHub (scope `gist`). L'app crée un Gist privé `coffee-tracker-data` au premier sync, push/pull du JSON complet (cafés, brews, visites, machines). Sync auto à l'ouverture + bouton « Synchroniser maintenant ». Stratégie last-write-wins (suffit pour usage solo). Versioning historique gratuit côté Gist. Indicateur dans la nav : *« ✓ synchronisé il y a Xmin »*.

## UX & confort

- ⬜ **17. Mode sombre** — Toggle dans Réglages (suit aussi `prefers-color-scheme` par défaut). MudBlazor supporte nativement via `MudThemeProvider` qui prend un `Theme` clair + `Theme` sombre. Adapter la palette café pour la version sombre (coffee-100/200 pour les fonds, coffee-50 pour le texte). Mettre à jour les styles maison (`.bottom-nav`, popups Leaflet, marqueurs map…) pour suivre.
- ⬜ **18. Notifications PWA proactives** — Web Push API (gratuit, fonctionne hors-ligne sur Android Chrome + iOS Safari récent). Cas d'usage : « Stock bas sur \<café\> » (≤ 30g restants), « Café \<X\> sort de la fenêtre optimale dans 3 jours », « Café \<Y\> au-delà du pic depuis hier ». Permissions demandées au 1er paramétrage. Évaluation périodique des règles via un Service Worker `periodicsync` (avec fallback sur évaluation à l'ouverture si l'API n'est pas dispo).

## Features coffee-geek (suite)

- ⬜ **19. Vue comparaison de brews côte-à-côte** — sur la fiche d'un café, sélection de 2 brews (cases à cocher) → bouton « Comparer » → vue diff montrant pour chaque champ les deux valeurs, les écarts mis en évidence (mouture changée, ratio différent, score qui a bougé). Très utile pour comprendre l'effet d'une variable ajustée.

---

**Hors scope MVP, à reconsidérer plus tard :**
- Cupping form structuré SCA (acidité/corps/sucrosité/finale en sliders)
- Scan photo de paquet via LLM vision (coût API, abandonné)
- Publication app stores (PWABuilder)
