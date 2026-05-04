// Wrapper minimal autour de Notification API + persistance de la préférence.
window.coffeeNotifications = (function () {
  const ENABLED_KEY = 'coffee.notifs';

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

  return { isSupported, permission, requestPermission, show, isEnabled, setEnabled };
})();
