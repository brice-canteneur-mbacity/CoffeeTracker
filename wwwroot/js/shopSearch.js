// Shop search facade — Google Places (New) when an API key is configured,
// otherwise Photon (OpenStreetMap).
// Returned shape (camelCase, mapped to PlaceResult/PlaceSearchResponse C# records):
//   { provider: "google"|"osm",
//     results: [{ provider, externalId, name, address, city, country, latitude, longitude }] }

window.coffeeShopSearch = (function () {
  const KEY_STORAGE = 'coffee.google.apiKey';
  let _gmapsPromise = null;

  function getGoogleKey() {
    return localStorage.getItem(KEY_STORAGE) || null;
  }

  function setGoogleKey(key) {
    if (key && key.trim().length > 0) localStorage.setItem(KEY_STORAGE, key.trim());
    else localStorage.removeItem(KEY_STORAGE);
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
    const { suggestions } = await AutocompleteSuggestion.fetchAutocompleteSuggestions({
      input: query,
      sessionToken: token,
      includedPrimaryTypes: ['cafe', 'restaurant']
    });

    const out = [];
    for (const sug of (suggestions || [])) {
      const pred = sug.placePrediction;
      if (!pred) continue;
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

  return { getGoogleKey, setGoogleKey, googleSearch, osmSearch, search };
})();
