// Wrapper minimal autour de Notification API + persistance de la préférence.
window.coffeeNotifications = (function () {
  const ENABLED_KEY = 'coffee.notifs';
  // Set des coffeeId déjà notifiés pour "past peak", pour ne pas répéter l'alerte
  // à chaque ouverture de l'app. Un café notifié le reste jusqu'à effacement manuel.
  const PAST_PEAK_KEY = 'coffee.notifs.pastPeakNotified';

  function isSupported() {
    return 'Notification' in window;
  }

  function permission() {
    return isSupported() ? Notification.permission : 'denied';
  }

  async function requestPermission() {
    if (!isSupported()) return false;
    if (Notification.permission === 'granted') return true;
    if (Notification.permission === 'denied') return false;
    const result = await Notification.requestPermission();
    return result === 'granted';
  }

  function show(title, body) {
    if (!isSupported()) return;
    if (Notification.permission !== 'granted') return;
    try {
      new Notification(title, { body, icon: 'icon-192.png', badge: 'icon-192.png' });
    } catch (e) {
      console.warn('[coffeeNotifications] show failed:', e);
    }
  }

  function isEnabled() {
    return localStorage.getItem(ENABLED_KEY) === '1';
  }

  function setEnabled(enabled) {
    if (enabled) localStorage.setItem(ENABLED_KEY, '1');
    else localStorage.removeItem(ENABLED_KEY);
  }

  // ─── Anti-répétition "past peak" ───────────────────────────────────────
  // On mémorise les coffeeId déjà notifiés dans localStorage pour éviter que la même
  // alerte (« Café X : X jours après torréfaction, au-delà du pic ») pop à chaque
  // ouverture de l'app. Corruption/absence : on retourne un Set vide, robuste.
  function getPastPeakNotified() {
    try {
      const raw = localStorage.getItem(PAST_PEAK_KEY);
      if (!raw) return [];
      const arr = JSON.parse(raw);
      if (!Array.isArray(arr)) return [];
      return arr.filter(x => Number.isInteger(x));
    } catch (_) { return []; }
  }

  function markPastPeakNotified(coffeeId) {
    const id = Number(coffeeId);
    if (!Number.isInteger(id)) return;
    const set = new Set(getPastPeakNotified());
    if (set.has(id)) return;
    set.add(id);
    localStorage.setItem(PAST_PEAK_KEY, JSON.stringify(Array.from(set)));
  }

  return {
    isSupported, permission, requestPermission, show, isEnabled, setEnabled,
    getPastPeakNotified, markPastPeakNotified
  };
})();
