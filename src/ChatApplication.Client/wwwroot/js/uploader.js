// ── OrbitChat XHR Upload with Progress ──────────────────────────────────────
// Provides real-time upload progress reporting back to Blazor via JS Interop.

(() => {
  let currentXhr = null;

  /**
   * Uploads a file from an <input type="file"> to the server via XHR,
   * reporting progress to a Blazor .NET object reference.
   * @param {HTMLInputElement} inputEl - The file input element
   * @param {string} url - The upload endpoint URL
   * @param {string} authToken - Bearer token
   * @param {object} dotnetRef - Blazor DotNetObjectReference for progress callbacks
   */
  async function uploadFileWithProgress(inputEl, url, authToken, dotnetRef) {
    const file = inputEl?.files?.[0];
    if (!file) throw new Error("No file selected.");

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      currentXhr = xhr;
      const formData = new FormData();
      formData.append("file", file);

      xhr.upload.addEventListener("progress", (e) => {
        if (e.lengthComputable) {
          const pct = Math.round((e.loaded / e.total) * 100);
          dotnetRef.invokeMethodAsync("OnUploadProgress", pct);
        }
      });

      xhr.addEventListener("load", () => {
        currentXhr = null;
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            const json = JSON.parse(xhr.responseText);
            resolve(json);
          } catch {
            reject(new Error("Invalid JSON response from server."));
          }
        } else {
          let msg = `Upload failed (${xhr.status})`;
          try {
            const err = JSON.parse(xhr.responseText);
            if (err.errors) msg = err.errors.join(" ");
            else if (err.message) msg = err.message;
          } catch { /* use default */ }
          reject(new Error(msg));
        }
      });

      xhr.addEventListener("error", () => {
        currentXhr = null;
        reject(new Error("Network error during upload."));
      });

      xhr.addEventListener("abort", () => {
        currentXhr = null;
        reject(new Error("Upload cancelled."));
      });

      xhr.open("POST", url, true);
      if (authToken) {
        xhr.setRequestHeader("Authorization", `Bearer ${authToken}`);
      }
      xhr.send(formData);
    });
  }

  /**
   * Upload a Blob (e.g., recorded audio, cropped image canvas) with progress.
   * @param {Uint8Array} data - The file bytes
   * @param {string} fileName - File name
   * @param {string} mimeType - MIME type
   * @param {string} url - Upload endpoint
   * @param {string} authToken - Bearer token
   * @param {object} dotnetRef - Blazor DotNetObjectReference
   */
  async function uploadBlobWithProgress(data, fileName, mimeType, url, authToken, dotnetRef) {
    const blob = new Blob([new Uint8Array(data)], { type: mimeType });
    const file = new File([blob], fileName, { type: mimeType });

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      currentXhr = xhr;
      const formData = new FormData();
      formData.append("file", file);

      xhr.upload.addEventListener("progress", (e) => {
        if (e.lengthComputable) {
          const pct = Math.round((e.loaded / e.total) * 100);
          dotnetRef.invokeMethodAsync("OnUploadProgress", pct);
        }
      });

      xhr.addEventListener("load", () => {
        currentXhr = null;
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            resolve(JSON.parse(xhr.responseText));
          } catch {
            reject(new Error("Invalid JSON from server."));
          }
        } else {
          let msg = `Upload failed (${xhr.status})`;
          try {
            const err = JSON.parse(xhr.responseText);
            if (err.errors) msg = err.errors.join(" ");
            else if (err.message) msg = err.message;
          } catch { /* use default */ }
          reject(new Error(msg));
        }
      });

      xhr.addEventListener("error", () => { currentXhr = null; reject(new Error("Network error.")); });
      xhr.addEventListener("abort", () => { currentXhr = null; reject(new Error("Upload cancelled.")); });

      xhr.open("POST", url, true);
      if (authToken) xhr.setRequestHeader("Authorization", `Bearer ${authToken}`);
      xhr.send(formData);
    });
  }

  function cancelUpload() {
    if (currentXhr) {
      currentXhr.abort();
      currentXhr = null;
    }
  }

  window.orbitUploader = {
    uploadFileWithProgress,
    uploadBlobWithProgress,
    cancelUpload
  };
})();
