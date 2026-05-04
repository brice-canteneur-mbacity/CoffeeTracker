// Persistance et lecture de la préférence de thème.
// Trois valeurs : "system" (par défaut, suit prefers-color-scheme), "light", "dark".
window.coffeeTheme = (function () {
  const KEY = 'coffee.theme';

  function getPreference() {
    return localStorage.getItem(KEY) || 'system';
  }

  function setPreference(pref) {
    if (pref === 'system' || !pref) localStorage.removeItem(KEY);
    else localStorage.setItem(KEY, pref);
  }

  function prefersDark() {
    return !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
  }

  function applyBodyClass(isDark) {
    if (isDark) document.body.classList.add('theme-dark');
    else document.body.classList.remove('theme-dark');
  }

  return { getPreference, setPreference, prefersDark, applyBodyClass };
})();
