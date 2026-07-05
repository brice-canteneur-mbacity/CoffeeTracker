// Shop search facade — Google Places (New) when an API key is configured,
// otherwise Photon (OpenStreetMap).
// Returned shape (camelCase, mapped to PlaceResult/PlaceSearchResponse C# records):
//   { provider: "google"|"osm",
//     results: [{ provider, externalId, name, address, city, country, latitude, longitude }] }

window.coffeeShopSearch = (function () {
  const KEY_STORAGE = 'coffee.google.apiKey';
  const PRIMARY_TYPES_STORAGE = 'coffee.google.primaryTypes';
  const NO_FILTER_STORAGE = 'coffee.google.noTypeFilter';
  // 5 types par défaut, limite API. Voir Settings pour changer la sélection.
  const DEFAULT_PRIMARY_TYPES = ['cafe', 'coffee_shop', 'bakery', 'restaurant', 'bar'];
  const MAX_PRIMARY_TYPES = 5;
  let _gmapsPromise = null;

  function getGoogleKey() {
    return localStorage.getItem(KEY_STORAGE) || null;
  }

  function setGoogleKey(key) {
    if (key && key.trim().length > 0) localStorage.setItem(KEY_STORAGE, key.trim());
    else localStorage.removeItem(KEY_STORAGE);
  }

  // Sélection utilisateur des primary types Google Places. Retourne la valeur par
  // défaut si rien n'est stocké ou si le JSON est invalide (protection contre corruption
  // manuelle de localStorage). Toujours capé à 5 (limite API : INVALID_ARGUMENT au-delà).
  function getPrimaryTypes() {
    const raw = localStorage.getItem(PRIMARY_TYPES_STORAGE);
    if (!raw) return DEFAULT_PRIMARY_TYPES.slice();
    try {
      const arr = JSON.parse(raw);
      if (!Array.isArray(arr) || arr.length === 0) return DEFAULT_PRIMARY_TYPES.slice();
      return arr.filter(x => typeof x === 'string' && x.length > 0).slice(0, MAX_PRIMARY_TYPES);
    } catch (_) {
      return DEFAULT_PRIMARY_TYPES.slice();
    }
  }

  function setPrimaryTypes(types) {
    if (!Array.isArray(types) || types.length === 0) {
      localStorage.removeItem(PRIMARY_TYPES_STORAGE);
      return;
    }
    const clean = types.filter(x => typeof x === 'string' && x.length > 0).slice(0, MAX_PRIMARY_TYPES);
    localStorage.setItem(PRIMARY_TYPES_STORAGE, JSON.stringify(clean));
  }

  function getDefaultPrimaryTypes() {
    return DEFAULT_PRIMARY_TYPES.slice();
  }

  // Mode "pas de filtre" : sans includedPrimaryTypes, l'autocomplete renvoie tout
  // (adresses, villes, régions inclus). On filtre côté client sur types.includes('establishment')
  // pour ne garder que les commerces. Nécessaire pour trouver certains lieux dont le type primaire
  // n'est ni dans le catalogue ni dans une saisie custom.
  function getNoTypeFilter() {
    return localStorage.getItem(NO_FILTER_STORAGE) === '1';
  }

  function setNoTypeFilter(enabled) {
    if (enabled) localStorage.setItem(NO_FILTER_STORAGE, '1');
    else localStorage.removeItem(NO_FILTER_STORAGE);
  }

  function loadGoogleScript(key) {
    if (_gmapsPromise) return _gmapsPromise;
    _gmapsPromise = new Promise((resolve, reject) => {
      const cbName = '__gmapsReady_' + Date.now();
      window[cbName] = () => { try { delete window[cbName]; } catch (_) {} resolve(); };
      const s = document.createElement('script');
      s.async = true;
      s.src = 'https://maps.googleapis.com/maps/api/js'
            + '?key=' + encodeURIComponent(key)
            + '&libraries=places&v=weekly&loading=async'
            + '&callback=' + cbName;
      s.onerror = () => { _gmapsPromise = null; reject(new Error('Google Maps script failed to load')); };
      document.head.appendChild(s);
    });
    return _gmapsPromise;
  }

  async function googleSearch(query, key) {
    if (!query || query.length < 2) return [];
    await loadGoogleScript(key);
    // Make sure the Places library is imported (lazy in v=weekly).
    if (typeof google.maps.importLibrary === 'function') {
      await google.maps.importLibrary('places');
    }
    const { AutocompleteSuggestion, AutocompleteSessionToken } = google.maps.places;
    const token = new AutocompleteSessionToken();
    // Deux modes (configurable dans Settings) :
    //  - avec filtre (par défaut) : includedPrimaryTypes contraint le résultat aux 5 types
    //    max choisis par l'utilisateur (limite dure API : INVALID_REQUEST au-delà).
    //  - sans filtre : on omet includedPrimaryTypes et on filtre côté client sur
    //    types.includes('establishment') pour écarter adresses/villes/régions.
    const noFilter = getNoTypeFilter();
    const req = { input: query, sessionToken: token };
    if (!noFilter) req.includedPrimaryTypes = getPrimaryTypes();
    const { suggestions } = await AutocompleteSuggestion.fetchAutocompleteSuggestions(req);

    const out = [];
    for (const sug of (suggestions || [])) {
      const pred = sug.placePrediction;
      if (!pred) continue;
      // En mode "pas de filtre" on garde seulement les commerces (types 'establishment') ;
      // adresses/villes/régions n'ont pas ce type et sont écartés.
      if (noFilter) {
        const predTypes = pred.types || [];
        if (!predTypes.includes('establishment')) continue;
      }
      try {
        const place = pred.toPlace();
        await place.fetchFields({
          fields: ['displayName', 'formattedAddress', 'location', 'addressComponents']
        });

        const addrComps = place.addressComponents || [];
        const cityComp = addrComps.find(c => c.types && c.types.includes('locality'))
                      || addrComps.find(c => c.types && c.types.includes('postal_town'));
        const countryComp = addrComps.find(c => c.types && c.types.includes('country'));

        out.push({
          provider: 'google',
          externalId: 'google:' + (pred.placeId || ''),
          name: (place.displayName && (place.displayName.text || place.displayName)) || (pred.mainText && pred.mainText.text) || '',
          address: place.formattedAddress || (pred.text && pred.text.text) || null,
          city: cityComp ? (cityComp.longText || cityComp.shortText) : null,
          country: countryComp ? (countryComp.longText || countryComp.shortText) : null,
          latitude: place.location ? Number(place.location.lat()) : null,
          longitude: place.location ? Number(place.location.lng()) : null
        });
      } catch (e) {
        // Fall back to bare prediction info if details fetch fails.
        out.push({
          provider: 'google',
          externalId: 'google:' + (pred.placeId || ''),
          name: (pred.mainText && pred.mainText.text) || '',
          address: (pred.text && pred.text.text) || null,
          city: null, country: null, latitude: null, longitude: null
        });
      }
      if (out.length >= 8) break;
    }
    return out;
  }

  async function osmSearch(query) {
    if (!query || query.length < 2) return [];
    const url = 'https://photon.komoot.io/api/'
              + '?osm_tag=amenity:cafe&osm_tag=amenity:restaurant'
              + '&limit=10&q=' + encodeURIComponent(query);
    const r = await fetch(url, { headers: { 'Accept': 'application/json' } });
    if (!r.ok) throw new Error('Photon HTTP ' + r.status);
    const data = await r.json();
    const out = [];
    for (const f of (data.features || [])) {
      const p = f.properties || {};
      const coords = (f.geometry && f.geometry.coordinates) || [null, null];
      const street = [p.street, p.housenumber].filter(Boolean).join(' ');
      const addr = [street, p.postcode, p.city || p.town || p.village].filter(Boolean).join(', ');
      out.push({
        provider: 'osm',
        externalId: 'osm:' + (p.osm_type || 'N') + (p.osm_id || ''),
        name: p.name || '',
        address: addr || null,
        city: p.city || p.town || p.village || null,
        country: p.country || null,
        latitude: coords[1] != null ? Number(coords[1]) : null,
        longitude: coords[0] != null ? Number(coords[0]) : null
      });
    }
    return out.filter(r => r.name);
  }

  // forceProvider: 'google' | 'osm' | null/undefined.
  // - When set, uses that provider exclusively (no auto-fallback) and propagates errors.
  // - When null: Google if key present (with silent OSM fallback on failure), else OSM.
  async function search(query, forceProvider) {
    if (forceProvider === 'osm') {
      return { provider: 'osm', results: await osmSearch(query) };
    }
    if (forceProvider === 'google') {
      const key = getGoogleKey();
      if (!key) throw new Error('Aucune clé Google configurée — passe sur OpenStreetMap ou ajoute une clé dans Réglages.');
      return { provider: 'google', results: await googleSearch(query, key) };
    }
    // Auto mode: Google → fallback OSM
    const key = getGoogleKey();
    if (key) {
      try {
        const results = await googleSearch(query, key);
        return { provider: 'google', results };
      } catch (e) {
        console.warn('[coffeeShopSearch] Google failed, falling back to OSM:', e);
        const results = await osmSearch(query);
        return { provider: 'osm', results };
      }
    }
    const results = await osmSearch(query);
    return { provider: 'osm', results };
  }

  // ─── Géolocalisation ─────────────────────────────────────────────────────
  // Wrapper sur navigator.geolocation pour récupérer une position unique.
  // Rejette avec un message explicite (permission refusée, timeout, indispo).
  function getCurrentLocation(timeoutMs) {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) {
        reject(new Error('Géolocalisation non supportée par ce navigateur.'));
        return;
      }
      navigator.geolocation.getCurrentPosition(
        pos => resolve({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracy: pos.coords.accuracy
        }),
        err => {
          const msg = err && err.code === 1
            ? 'Géolocalisation refusée. Autorise-la dans les réglages du navigateur.'
            : (err && err.message) || 'Position indisponible.';
          reject(new Error(msg));
        },
        { enableHighAccuracy: true, timeout: timeoutMs || 10000, maximumAge: 60000 }
      );
    });
  }

  // ─── Recherche de cafés à proximité (Google Places New) ──────────────────
  // Place.searchNearby filtre par type/géographie ; on filtre nous-mêmes les
  // résultats par note minimale et nombre d'avis (évite les 5★ d'un seul avis).
  function haversineMeters(lat1, lng1, lat2, lng2) {
    const R = 6371000;
    const toRad = d => d * Math.PI / 180;
    const dLat = toRad(lat2 - lat1);
    const dLng = toRad(lng2 - lng1);
    const a = Math.sin(dLat / 2) ** 2
            + Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLng / 2) ** 2;
    return 2 * R * Math.asin(Math.sqrt(a));
  }

  async function nearbyCafes(latitude, longitude, radiusMeters, minRating, minRatingCount) {
    const key = getGoogleKey();
    if (!key) throw new Error('Aucune clé Google configurée — ajoute-en une dans Réglages pour utiliser cette fonction.');

    await loadGoogleScript(key);
    const placesLib = await google.maps.importLibrary('places');
    const { Place, SearchNearbyRankPreference } = placesLib;

    const request = {
      fields: ['displayName', 'formattedAddress', 'location', 'rating', 'userRatingCount', 'id', 'addressComponents'],
      locationRestriction: {
        center: { lat: Number(latitude), lng: Number(longitude) },
        radius: Number(radiusMeters)
      },
      includedPrimaryTypes: ['cafe'],
      maxResultCount: 20,
      rankPreference: SearchNearbyRankPreference.POPULARITY
    };

    const { places } = await Place.searchNearby(request);
    const minR = Number(minRating) || 0;
    const minC = Number(minRatingCount) || 0;

    const out = [];
    for (const p of (places || [])) {
      const rating = Number(p.rating) || 0;
      const ratingCount = Number(p.userRatingCount) || 0;
      if (rating < minR) continue;
      if (ratingCount < minC) continue;
      if (!p.location) continue;

      const lat = Number(p.location.lat());
      const lng = Number(p.location.lng());
      const addrComps = p.addressComponents || [];
      const cityComp = addrComps.find(c => c.types && c.types.includes('locality'))
                    || addrComps.find(c => c.types && c.types.includes('postal_town'));
      const countryComp = addrComps.find(c => c.types && c.types.includes('country'));

      out.push({
        externalId: 'google:' + (p.id || ''),
        name: (p.displayName && (p.displayName.text || p.displayName)) || '',
        address: p.formattedAddress || null,
        city: cityComp ? (cityComp.longText || cityComp.shortText) : null,
        country: countryComp ? (countryComp.longText || countryComp.shortText) : null,
        latitude: lat,
        longitude: lng,
        rating,
        userRatingCount: ratingCount,
        distanceMeters: Math.round(haversineMeters(Number(latitude), Number(longitude), lat, lng))
      });
    }
    out.sort((a, b) => a.distanceMeters - b.distanceMeters);
    return out;
  }

  return {
    getGoogleKey, setGoogleKey,
    getPrimaryTypes, setPrimaryTypes, getDefaultPrimaryTypes,
    getNoTypeFilter, setNoTypeFilter,
    googleSearch, osmSearch, search, getCurrentLocation, nearbyCafes
  };
})();
