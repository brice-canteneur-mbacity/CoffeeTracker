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
    const map = L.map(el, { zoomControl: true, attributionControl: true })
      .setView([46.5, 2.5], 5);

    // CARTO Positron — fond clair épuré, gratuit, attribution OSM + CARTO requise.
    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
      maxZoom: 20,
      subdomains: 'abcd',
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank">OSM</a> &copy; <a href="https://carto.com/attributions" target="_blank">CARTO</a>'
    }).addTo(map);

    const layer = L.layerGroup().addTo(map);
    _maps.set(elementId, { map, layer });
    update(elementId, visits);
  }

  function update(elementId, visits) {
    const entry = _maps.get(elementId);
    if (!entry) return;
    entry.layer.clearLayers();

    const points = [];
    for (const v of (visits || [])) {
      if (v.latitude == null || v.longitude == null) continue;
      const r = Number(v.rating) || 0;
      const stars = '★'.repeat(r) + '☆'.repeat(Math.max(0, 5 - r));
      const coffee = v.coffeeOrigin ? `<div class="coffee-popup-meta">${escapeHtml(v.coffeeOrigin)}</div>` : '';
      const drink = v.drinkType ? `<div class="coffee-popup-meta">${escapeHtml(v.drinkType)}</div>` : '';
      const popup = `
        <div class="coffee-popup">
          <div class="coffee-popup-title">${escapeHtml(v.shopName || 'Sans nom')}</div>
          ${drink}
          ${coffee}
          <div class="coffee-popup-stars">${stars}</div>
          <div class="coffee-popup-date">${escapeHtml(v.dateLabel || '')}</div>
          <a class="coffee-popup-link" href="/shops/${v.id}/edit">Modifier ›</a>
        </div>`;
      const marker = L.marker([v.latitude, v.longitude], { icon: makeIcon(r) }).bindPopup(popup);
      entry.layer.addLayer(marker);
      points.push([v.latitude, v.longitude]);
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
