// Helpers JS pour : (1) déclencher le download d'un blob JSON depuis Blazor (export),
// (2) compresser une image en data URL via canvas (utilisé partout où on persiste une photo —
// les data URLs PNG/JPEG sont stockées telles quelles dans IndexedDB).
window.coffeeData = {
  /**
   * Trigger a browser download for the given JSON content.
   * @param {string} filename
   * @param {string} content
   */
  downloadJson: function (filename, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
};

window.coffeeImage = {
  // Paramètres par défaut — réglés serrés pour limiter la taille du Gist (les photos
  // servent à reconnaître un sac / une machine / un shop, pas à imprimer en A4).
  // ~35 % plus léger qu'un 1024 / 0.82, qualité visuelle préservée à l'écran.
  defaultMaxSize: 900,
  defaultQuality: 0.75,

  /**
   * Read a File via createImageBitmap, downscale to maxSize, return JPEG data URL.
   * @param {Blob | File} file
   * @param {number} [maxSize]
   * @param {number} [quality]
   * @returns {Promise<string>}
   */
  toCompressedDataUrl: async function (file, maxSize, quality) {
    const targetSize = maxSize ?? this.defaultMaxSize;
    const targetQuality = quality ?? this.defaultQuality;
    const bitmap = await createImageBitmap(file);
    const ratio = Math.min(1, targetSize / Math.max(bitmap.width, bitmap.height));
    const w = Math.round(bitmap.width * ratio);
    const h = Math.round(bitmap.height * ratio);
    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(bitmap, 0, 0, w, h);
    if (typeof bitmap.close === 'function') bitmap.close();
    return canvas.toDataURL('image/jpeg', targetQuality);
  },

  /**
   * Convert a stream from IBrowserFile.OpenReadStream() into a compressed data URL.
   * @param {Uint8Array} bytes - raw bytes of the source image
   * @param {string} mime
   * @param {number} [maxSize]
   * @param {number} [quality]
   */
  bytesToCompressedDataUrl: async function (bytes, mime, maxSize, quality) {
    const blob = new Blob([bytes], { type: mime || 'image/jpeg' });
    return await this.toCompressedDataUrl(blob, maxSize, quality);
  },

  /**
   * Re-encode an existing data URL (or any URL the browser can fetch) through the
   * compression pipeline. Used by the "Recompress photos" maintenance action.
   * @param {string} dataUrl
   * @param {number} [maxSize]
   * @param {number} [quality]
   * @returns {Promise<string>}
   */
  recompressDataUrl: async function (dataUrl, maxSize, quality) {
    const resp = await fetch(dataUrl);
    const blob = await resp.blob();
    return await this.toCompressedDataUrl(blob, maxSize, quality);
  }
};
