// Leaflet-based map for shop visits.
// Each registered map is keyed by its DOM element id; init() destroys & rebuilds.
window.coffeeMap = (function () {
  const _maps = new Map();

  function escapeHtml(s) {
    return String(s ?? '').replace(/[&<>"']/g, c => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
  }

  // Format officiel Google Maps URLs : search/?api=1&query=...&query_place_id=...
  // query est obligatoire (sert de fallback si le place_id n'est pas résolu dans l'app mobile).
  // Doc : https://developers.google.com/maps/documentation/urls/get-started
  function googleMapsUrl(externalId, lat, lng, address, name) {
    const query = [name, address].filter(Boolean).join(', ');
    if (externalId && externalId.indexOf('google:') === 0) {
      const placeId = externalId.substring('google:'.length);
      if (placeId) {
        const q = query || 'place';
        return 'https://www.google.com/maps/search/?api=1&query=' + encodeURIComponent(q)
          + '&query_place_id=' + encodeURIComponent(placeId);
      }
    }
    if (lat != null && lng != null) {
      return 'https://www.google.com/maps/search/?api=1&query=' + lat + ',' + lng;
    }
    if (query) {
      return 'https://www.google.com/maps/search/?api=1&query=' + encodeURIComponent(query);
    }
    return null;
  }

  // Couleur du marqueur selon la note — palette café.
  function markerColor(rating) {
    const r = Number(rating) || 0;
    if (r >= 4) return '#3b2418'; // coffee-800
    if (r >= 3) return '#7d5128'; // coffee-600
    if (r >= 1) return '#a06d3b'; // coffee-500
    return '#d4b487';             // coffee-300 (no rating)
  }

  // Note moyenne formatée au dixième selon la locale ("3,5" en FR, "3.5" en EN).
  function formatRating(r) {
    return r.toLocaleString(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 });
  }

  function makeIcon(rating) {
    const color = markerColor(rating);
    const label = rating > 0 ? formatRating(rating) : '';
    return L.divIcon({
      className: 'coffee-marker',
      html: `<div class="coffee-marker-dot" style="background:${color};">${label}</div>`,
      iconSize: [28, 28],
      iconAnchor: [14, 14],
      popupAnchor: [0, -16]
    });
  }

  function init(elementId, visits) {
    if (_maps.has(elementId)) destroy(elementId);
    const el = document.getElementById(elementId);
    if (!el) return;

    // Default view: roughly centered on France at low zoom.
    // Zoom control désactivé par défaut, on en ajoute un avec libellés français.
    const map = L.map(el, { zoomControl: false, attributionControl: true })
      .setView([46.5, 2.5], 5);

    L.control.zoom({
      position: 'topleft',
      zoomInTitle: 'Zoomer',
      zoomOutTitle: 'Dézoomer'
    }).addTo(map);

    // Préfixe d'attribution (par défaut « Leaflet ») en français.
    map.attributionControl.setPrefix('<a href="https://leafletjs.com" target="_blank">Leaflet</a>');

    // CARTO Positron — fond clair épuré, gratuit, attribution OSM + CARTO requise.
    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
      maxZoom: 20,
      subdomains: 'abcd',
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank">OSM</a> &copy; <a href="https://carto.com/attributions" target="_blank">CARTO</a>'
    }).addTo(map);

    // Tooltip du bouton de fermeture des popups en français (l'option n'est pas exposée).
    map.on('popupopen', (e) => {
      const btn = e.popup && e.popup._closeButton;
      if (btn) btn.setAttribute('title', 'Fermer');
    });

    const layer = L.layerGroup().addTo(map);
    _maps.set(elementId, { map, layer });
    update(elementId, visits);
  }

  function update(elementId, visits) {
    const entry = _maps.get(elementId);
    if (!entry) return;
    entry.layer.clearLayers();

    const points = [];
    // Markers shop-centric : 1 marker par Shop (pas par visite).
    // Format attendu : { id, shopName, city, visitCount, rating, latitude, longitude, lastVisitLabel }
    for (const s of (visits || [])) {
      if (s.latitude == null || s.longitude == null) continue;
      const r = Number(s.rating) || 0;
      // Les étoiles affichées sont arrondies à l'entier le plus proche ; la note numérique
      // au dixième est rendue juste à côté pour conserver la précision.
      const rRounded = Math.round(r);
      const stars = r > 0
        ? `${'★'.repeat(rRounded)}${'☆'.repeat(Math.max(0, 5 - rRounded))} <span class="coffee-popup-stars-value">${formatRating(r)}</span>`
        : '';
      const cityLine = s.city ? `<div class="coffee-popup-meta">${escapeHtml(s.city)}</div>` : '';
      const visitsCount = Number(s.visitCount) || 0;
      const visitsLine = visitsCount > 0
        ? `<div class="coffee-popup-meta">${visitsCount} visite${visitsCount > 1 ? 's' : ''}${s.lastVisitLabel ? ' · dernière le ' + escapeHtml(s.lastVisitLabel) : ''}</div>`
        : '<div class="coffee-popup-meta">Aucune visite</div>';
      const gmapsUrl = googleMapsUrl(s.externalPlaceId, s.latitude, s.longitude, s.address, s.shopName);
      const gmapsLine = gmapsUrl
        ? `<a class="coffee-popup-link" href="${gmapsUrl}" target="_blank" rel="noopener">Google Maps ↗</a>`
        : '';
      const popup = `
        <div class="coffee-popup">
          <div class="coffee-popup-title">${escapeHtml(s.shopName || 'Sans nom')}</div>
          ${cityLine}
          ${visitsLine}
          ${stars ? `<div class="coffee-popup-stars">${stars}</div>` : ''}
          <a class="coffee-popup-link" href="shops/${s.id}">Voir le shop ›</a>
          ${gmapsLine}
        </div>`;
      const marker = L.marker([s.latitude, s.longitude], { icon: makeIcon(r) }).bindPopup(popup);
      entry.layer.addLayer(marker);
      points.push([s.latitude, s.longitude]);
    }

    if (points.length === 1) {
      entry.map.setView(points[0], 14);
    } else if (points.length > 1) {
      entry.map.fitBounds(L.latLngBounds(points), { padding: [40, 40], maxZoom: 14 });
    }

    // Force resize after a tick (the container might have just become visible).
    setTimeout(() => entry.map.invalidateSize(), 100);
  }

  function destroy(elementId) {
    const entry = _maps.get(elementId);
    if (!entry) return;
    if (entry.dotNetRef) { try { entry.dotNetRef.dispose && entry.dotNetRef.dispose(); } catch (_) {} }
    entry.map.remove();
    _maps.delete(elementId);
  }

  // ─── Suggestions « Google bien notés à proximité » ────────────────────
  // Couche séparée pour ne pas se mélanger avec les shops déjà visités.
  // Marqueurs distincts (couleur or, label = note).
  function suggestionIcon(rating) {
    const label = Number(rating).toFixed(1);
    return L.divIcon({
      className: 'coffee-marker coffee-marker-suggestion',
      html: `<div class="coffee-marker-dot coffee-marker-dot-suggestion">${label}</div>`,
      iconSize: [32, 32],
      iconAnchor: [16, 16],
      popupAnchor: [0, -18]
    });
  }

  function formatDistance(meters) {
    return meters < 1000
      ? `${meters} m`
      : `${(meters / 1000).toFixed(1)} km`;
  }

  // suggestions: [{ externalId, name, address, latitude, longitude, rating, userRatingCount, distanceMeters }]
  // dotNetRef: DotNetObjectReference Blazor invoqué via OnSuggestionClick(externalId) au clic du bouton popup.
  function showSuggestions(elementId, suggestions, dotNetRef) {
    const entry = _maps.get(elementId);
    if (!entry) return;

    if (!entry.suggestionLayer) {
      entry.suggestionLayer = L.layerGroup().addTo(entry.map);
    } else {
      entry.suggestionLayer.clearLayers();
    }
    entry.dotNetRef = dotNetRef;

    const points = [];
    for (const s of (suggestions || [])) {
      if (s.latitude == null || s.longitude == null) continue;
      const safeName = escapeHtml(s.name || 'Sans nom');
      const safeAddr = s.address ? escapeHtml(s.address) : '';
      const safeId = escapeHtml(s.externalId || '');
      const rating = Number(s.rating) || 0;
      const ratingCount = Number(s.userRatingCount) || 0;
      const dist = formatDistance(Number(s.distanceMeters) || 0);
      const gmapsUrl = googleMapsUrl(s.externalId, s.latitude, s.longitude, s.address, s.name);
      const gmapsLine = gmapsUrl
        ? `<a class="coffee-popup-link" href="${gmapsUrl}" target="_blank" rel="noopener">Google Maps ↗</a>`
        : '';
      const popup = `
        <div class="coffee-popup">
          <div class="coffee-popup-title">${safeName}</div>
          <div class="coffee-popup-meta">★ ${rating.toFixed(1)} · ${ratingCount} avis · ${dist}</div>
          ${safeAddr ? `<div class="coffee-popup-meta">${safeAddr}</div>` : ''}
          ${gmapsLine}
          <button type="button" class="coffee-popup-link coffee-popup-action"
                  data-external-id="${safeId}">
            Ajouter à mes shops…
          </button>
        </div>`;
      const marker = L.marker([s.latitude, s.longitude], { icon: suggestionIcon(rating) })
        .bindPopup(popup);
      // Délégation : on attache le handler à l'ouverture du popup (le bouton n'existe pas avant).
      marker.on('popupopen', (e) => {
        const node = e.popup.getElement();
        if (!node) return;
        const btn = node.querySelector('button.coffee-popup-action');
        if (!btn) return;
        btn.onclick = () => {
          const id = btn.getAttribute('data-external-id');
          if (entry.dotNetRef && id) {
            try { entry.dotNetRef.invokeMethodAsync('OnSuggestionClick', id); } catch (_) {}
          }
        };
      });
      entry.suggestionLayer.addLayer(marker);
      points.push([s.latitude, s.longitude]);
    }

    // Recadre pour englober suggestions + shops existants (sans zoomer trop).
    if (points.length > 0) {
      const allPoints = points.slice();
      entry.layer.eachLayer(l => {
        const ll = l.getLatLng && l.getLatLng();
        if (ll) allPoints.push([ll.lat, ll.lng]);
      });
      entry.map.fitBounds(L.latLngBounds(allPoints), { padding: [40, 40], maxZoom: 16 });
    }
  }

  function clearSuggestions(elementId) {
    const entry = _maps.get(elementId);
    if (!entry || !entry.suggestionLayer) return;
    entry.suggestionLayer.clearLayers();
  }

  // ─── Marqueur « ma position » (point bleu + cercle de précision) ────────
  function showUserPosition(elementId, latitude, longitude, accuracyMeters) {
    const entry = _maps.get(elementId);
    if (!entry) return;
    const lat = Number(latitude);
    const lng = Number(longitude);
    const acc = Math.max(0, Number(accuracyMeters) || 0);

    if (entry.userMarker) {
      entry.userMarker.setLatLng([lat, lng]);
    } else {
      const icon = L.divIcon({
        className: 'user-position-marker',
        html: '<div class="user-position-dot"></div>',
        iconSize: [18, 18],
        iconAnchor: [9, 9]
      });
      entry.userMarker = L.marker([lat, lng], { icon, interactive: false, keyboard: false })
        .bindTooltip('Ma position', { direction: 'top', offset: [0, -10] })
        .addTo(entry.map);
    }
    // Zoom sur la position — feedback visuel immédiat pendant que Google Places renvoie
    // les résultats (~1-2s). showSuggestions() recadrera ensuite pour englober les cafés.
    entry.map.setView([lat, lng], 14);
    if (acc > 0) {
      if (entry.userCircle) {
        entry.userCircle.setLatLng([lat, lng]).setRadius(acc);
      } else {
        entry.userCircle = L.circle([lat, lng], {
          radius: acc,
          color: '#1a73e8',
          fillColor: '#1a73e8',
          fillOpacity: 0.10,
          weight: 1,
          interactive: false
        }).addTo(entry.map);
      }
    }
  }

  function clearUserPosition(elementId) {
    const entry = _maps.get(elementId);
    if (!entry) return;
    if (entry.userMarker) { entry.userMarker.remove(); entry.userMarker = null; }
    if (entry.userCircle) { entry.userCircle.remove(); entry.userCircle = null; }
  }

  return { init, update, destroy, showSuggestions, clearSuggestions, showUserPosition, clearUserPosition };
})();
