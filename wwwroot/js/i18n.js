// Helpers JS pour le LocalizationService C#.
// - getBrowserLanguage : lit navigator.language (ex : "fr-FR") et renvoie le code court ("fr").
// - getStoredPreference / setStoredPreference : persistance de la langue choisie en localStorage.
window.coffeeI18n = (function () {
  const KEY = 'coffee.lang';

  function getBrowserLanguage() {
    const raw = (navigator.language || navigator.userLanguage || 'fr').toLowerCase();
    return raw.split('-')[0]; // "fr-FR" → "fr"
  }

  function getStoredPreference() {
    try { return localStorage.getItem(KEY); } catch { return null; }
  }

  function setStoredPreference(lang) {
    try {
      if (lang) localStorage.setItem(KEY, lang);
      else localStorage.removeItem(KEY);
    } catch { /* quota / privacy mode : on ignore */ }
  }

  return { getBrowserLanguage, getStoredPreference, setStoredPreference };
})();
