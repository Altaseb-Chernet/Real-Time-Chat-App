// ── OrbitChat Image Editor (Cropper.js Integration) ─────────────────────────
// Provides crop & rotate functionality for images before upload, Telegram-style.

(() => {
  let cropper = null;
  let editorModal = null;
  let resolvePromise = null;
  let rejectPromise = null;

  /**
   * Opens the image editor modal with the given image data URL.
   * Returns a promise that resolves with { base64, mimeType, fileName } on confirm,
   * or null if cancelled.
   */
  function openImageEditor(dataUrl, fileName) {
    return new Promise((resolve, reject) => {
      resolvePromise = resolve;
      rejectPromise = reject;

      // Build modal
      editorModal = document.createElement("div");
      editorModal.className = "img-editor-overlay";
      editorModal.innerHTML = `
        <div class="img-editor-modal">
          <div class="img-editor-header">
            <span class="img-editor-title">Edit Image</span>
            <button class="img-editor-close" title="Cancel">✕</button>
          </div>
          <div class="img-editor-canvas-wrap">
            <img id="img-editor-src" src="${dataUrl}" alt="Edit preview" />
          </div>
          <div class="img-editor-toolbar">
            <button class="img-editor-btn" data-action="rotateLeft" title="Rotate left">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 2.13-9.36L1 10"/>
              </svg>
            </button>
            <button class="img-editor-btn" data-action="rotateRight" title="Rotate right">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/>
              </svg>
            </button>
            <button class="img-editor-btn" data-action="flipH" title="Flip horizontal">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M8 3H5a2 2 0 0 0-2 2v14c0 1.1.9 2 2 2h3"/><path d="M16 3h3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-3"/><line x1="12" y1="20" x2="12" y2="4"/>
              </svg>
            </button>
            <button class="img-editor-btn" data-action="flipV" title="Flip vertical">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M3 8V5a2 2 0 0 1 2-2h14c1.1 0 2 .9 2 2v3"/><path d="M3 16v3a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-3"/><line x1="4" y1="12" x2="20" y2="12"/>
              </svg>
            </button>
            <button class="img-editor-btn" data-action="reset" title="Reset">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/>
              </svg>
            </button>
          </div>
          <div class="img-editor-actions">
            <button class="img-editor-cancel">Cancel</button>
            <button class="img-editor-confirm">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <polyline points="20 6 9 17 4 12"/>
              </svg>
              Apply & Send
            </button>
          </div>
        </div>
      `;

      document.body.appendChild(editorModal);

      // Wait for the image to appear in the DOM, then init Cropper
      requestAnimationFrame(() => {
        const imgEl = document.getElementById("img-editor-src");
        if (!imgEl) { cleanup(); reject(new Error("Editor image not found")); return; }

        cropper = new Cropper(imgEl, {
          viewMode: 1,
          dragMode: "move",
          autoCropArea: 1,
          responsive: true,
          background: false,
          guides: true,
          highlight: true,
          cropBoxMovable: true,
          cropBoxResizable: true,
          toggleDragModeOnDblclick: true,
        });
      });

      // Toolbar button events
      editorModal.querySelectorAll(".img-editor-btn").forEach(btn => {
        btn.addEventListener("click", () => {
          if (!cropper) return;
          const action = btn.dataset.action;
          switch (action) {
            case "rotateLeft":  cropper.rotate(-90); break;
            case "rotateRight": cropper.rotate(90); break;
            case "flipH":      cropper.scaleX(cropper.getData().scaleX === -1 ? 1 : -1); break;
            case "flipV":      cropper.scaleY(cropper.getData().scaleY === -1 ? 1 : -1); break;
            case "reset":      cropper.reset(); break;
          }
        });
      });

      // Cancel
      editorModal.querySelector(".img-editor-close").addEventListener("click", () => {
        cleanup();
        resolvePromise?.(null);
      });
      editorModal.querySelector(".img-editor-cancel").addEventListener("click", () => {
        cleanup();
        resolvePromise?.(null);
      });

      // Confirm — crop and return base64
      editorModal.querySelector(".img-editor-confirm").addEventListener("click", () => {
        if (!cropper) { cleanup(); resolvePromise?.(null); return; }
        const canvas = cropper.getCroppedCanvas({
          maxWidth: 2048,
          maxHeight: 2048,
          imageSmoothingQuality: "high"
        });
        if (!canvas) { cleanup(); resolvePromise?.(null); return; }
        const base64 = canvas.toDataURL("image/png");
        canvas.toBlob((blob) => {
          cleanup();
          resolvePromise?.({
            base64: base64,
            mimeType: "image/png",
            fileName: fileName.replace(/\.[^.]+$/, ".png"),
            bytes: blob ? blob.size : 0
          });
        }, "image/png");
      });

      // Click overlay to cancel
      editorModal.addEventListener("click", (e) => {
        if (e.target === editorModal) {
          cleanup();
          resolvePromise?.(null);
        }
      });
    });
  }

  function cleanup() {
    if (cropper) { cropper.destroy(); cropper = null; }
    if (editorModal) { editorModal.remove(); editorModal = null; }
  }

  /**
   * Reads a file from an <input> element and opens the image editor.
   * Returns the edited result or null.
   */
  async function editFromInput(inputEl) {
    const file = inputEl?.files?.[0];
    if (!file) return null;
    if (!file.type.startsWith("image/")) return null; // only edit images

    const dataUrl = await new Promise((resolve) => {
      const reader = new FileReader();
      reader.onload = (e) => resolve(e.target.result);
      reader.readAsDataURL(file);
    });

    return openImageEditor(dataUrl, file.name);
  }

  window.orbitImageEditor = {
    openImageEditor,
    editFromInput,
    cleanup
  };
})();
