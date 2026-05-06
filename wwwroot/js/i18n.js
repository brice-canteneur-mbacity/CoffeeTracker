// Helpers JS pour le LocalizationService C#.
// - getBrowserLanguage : lit navigator.language (ex : "fr-FR") et renvoie le code court ("fr").
// - getStoredPreference / setStoredPreference : persistance de la langue choisie en localStorage.
// - loadDict : charge un dictionnaire JSON via fetch natif (plus fiable que HttpClient sur Safari/PWA).
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

  // Charge un dictionnaire JSON via fetch natif. Renvoie null si échec (404, parse error, offline)
  // pour que l'appelant puisse fallback sans crasher. Log explicite en console pour débogage Safari.
  // On utilise un chemin RELATIF (sans / initial) pour respecter la <base href>, et on ajoute un
  // cache-buster pour ne pas se faire piéger par le service worker quand on update les JSON.
  async function loadDict(lang) {
    const url = 'i18n/' + lang + '.json?v=' + Date.now();
    try {
      const resp = await fetch(url, { cache: 'no-cache' });
      if (!resp.ok) {
        console.warn('[coffeeI18n] HTTP ' + resp.status + ' pour ' + url);
        return null;
      }
      return await resp.json();
    } catch (e) {
      console.warn('[coffeeI18n] fetch ' + url + ' a échoué :', e);
      return null;
    }
  }

  return { getBrowserLanguage, getStoredPreference, setStoredPreference, loadDict };
})();
