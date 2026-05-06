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
  /**
   * Read a File via createImageBitmap, downscale to maxSize, return JPEG data URL.
   * @param {Blob | File} file
   * @param {number} maxSize
   * @param {number} quality
   * @returns {Promise<string>}
   */
  toCompressedDataUrl: async function (file, maxSize, quality) {
    const bitmap = await createImageBitmap(file);
    const ratio = Math.min(1, maxSize / Math.max(bitmap.width, bitmap.height));
    const w = Math.round(bitmap.width * ratio);
    const h = Math.round(bitmap.height * ratio);
    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(bitmap, 0, 0, w, h);
    if (typeof bitmap.close === 'function') bitmap.close();
    return canvas.toDataURL('image/jpeg', quality);
  },

  /**
   * Convert a stream from IBrowserFile.OpenReadStream() into a compressed data URL.
   * @param {Uint8Array} bytes - raw bytes of the source image
   * @param {string} mime
   * @param {number} maxSize
   * @param {number} quality
   */
  bytesToCompressedDataUrl: async function (bytes, mime, maxSize, quality) {
    const blob = new Blob([bytes], { type: mime || 'image/jpeg' });
    return await this.toCompressedDataUrl(blob, maxSize, quality);
  }
};
