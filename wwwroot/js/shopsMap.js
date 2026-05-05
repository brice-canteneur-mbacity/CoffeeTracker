// Leaflet-based map for shop visits.
// Each registered map is keyed by its DOM element id; init() destroys & rebuilds.
window.coffeeMap = (function () {
  const _maps = new Map();

  function escapeHtml(s) {
    return String(s ?? '').replace(/[&<>"']/g, c => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
  }

  // Couleur du marqueur selon la note — palette café.
  function markerColor(rating) {
    const r = Number(rating) || 0;
    if (r >= 4) return '#3b2418'; // coffee-800
    if (r >= 3) return '#7d5128'; // coffee-600
    if (r >= 1) return '#a06d3b'; // coffee-500
    return '#d4b487';             // coffee-300 (no rating)
  }

  function makeIcon(rating) {
    const color = markerColor(rating);
    const label = rating > 0 ? rating : '';
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
      const stars = r > 0 ? '★'.repeat(r) + '☆'.repeat(Math.max(0, 5 - r)) : '';
      const cityLine = s.city ? `<div class="coffee-popup-meta">${escapeHtml(s.city)}</div>` : '';
      const visitsCount = Number(s.visitCount) || 0;
      const visitsLine = visitsCount > 0
        ? `<div class="coffee-popup-meta">${visitsCount} visite${visitsCount > 1 ? 's' : ''}${s.lastVisitLabel ? ' · dernière le ' + escapeHtml(s.lastVisitLabel) : ''}</div>`
        : '<div class="coffee-popup-meta">Aucune visite</div>';
      const popup = `
        <div class="coffee-popup">
          <div class="coffee-popup-title">${escapeHtml(s.shopName || 'Sans nom')}</div>
          ${cityLine}
          ${visitsLine}
          ${stars ? `<div class="coffee-popup-stars">${stars}</div>` : ''}
          <a class="coffee-popup-link" href="shops/${s.id}">Voir le shop ›</a>
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
    entry.map.remove();
    _maps.delete(elementId);
  }

  return { init, update, destroy };
})();
