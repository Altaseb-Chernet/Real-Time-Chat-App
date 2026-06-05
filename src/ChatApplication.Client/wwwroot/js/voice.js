(() => {
  // ── OrbitChat Voice Recorder ──────────────────────────────────────────────
  // Modern MediaRecorder-based voice recording with robust error handling.
  // Returns base64 audio data for Blazor WASM interop.

  let recorder = null;
  let chunks = [];
  let stream = null;
  let recordingStartTime = null;

  function pickMimeType() {
    const candidates = [
      "audio/webm;codecs=opus",
      "audio/webm",
      "audio/ogg;codecs=opus",
      "audio/ogg",
      "audio/mp4",
      "audio/wav"
    ];
    for (const c of candidates) {
      try {
        if (window.MediaRecorder && MediaRecorder.isTypeSupported && MediaRecorder.isTypeSupported(c)) return c;
      } catch { /* some browsers throw on isTypeSupported */ }
    }
    return "";
  }

  async function startVoiceRecording() {
    if (!navigator.mediaDevices?.getUserMedia) {
      throw new Error("Microphone is not supported in this browser. Please use a modern browser like Chrome, Firefox, or Edge.");
    }

    // If already recording, return
    if (recorder && recorder.state === "recording") return true;

    // Clean up any previous state
    cleanupRecorder();

    chunks = [];
    recordingStartTime = Date.now();

    try {
      stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true,
          sampleRate: 48000
        }
      });
    } catch (err) {
      if (err.name === "NotAllowedError" || err.name === "PermissionDeniedError") {
        throw new Error("Microphone access was denied. Please allow microphone permission in your browser settings and try again.");
      }
      if (err.name === "NotFoundError" || err.name === "DevicesNotFoundError") {
        throw new Error("No microphone found. Please connect a microphone and try again.");
      }
      throw new Error("Could not access microphone: " + (err.message || err.name));
    }

    const mimeType = pickMimeType();
    try {
      recorder = new MediaRecorder(stream, mimeType ? { mimeType } : undefined);
    } catch (err) {
      cleanupRecorder();
      throw new Error("MediaRecorder failed to initialize: " + (err.message || "Unknown error"));
    }

    recorder.addEventListener("dataavailable", (e) => {
      if (e.data && e.data.size > 0) chunks.push(e.data);
    });

    recorder.addEventListener("error", (e) => {
      console.error("MediaRecorder error:", e.error || e);
      cleanupRecorder();
    });

    // Use timeslice for consistent data chunks across browsers
    recorder.start(250);
    return true;
  }

  async function stopVoiceRecording() {
    if (!recorder) throw new Error("Recorder not started. Please start recording first.");
    if (recorder.state !== "recording") throw new Error("Recorder is not currently recording.");

    const mimeType = recorder.mimeType || "audio/webm";
    const duration = Date.now() - (recordingStartTime || Date.now());

    // Minimum recording duration check (500ms)
    if (duration < 500) {
      cleanupRecorder();
      throw new Error("Recording too short. Please hold the button longer to record a voice message.");
    }

    const blob = await new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error("Recording stop timed out. Please try again."));
      }, 5000);

      recorder.addEventListener("stop", () => {
        clearTimeout(timeout);
        resolve(new Blob(chunks, { type: mimeType }));
      }, { once: true });

      recorder.addEventListener("error", (e) => {
        clearTimeout(timeout);
        reject(e.error || new Error("Recording error occurred."));
      }, { once: true });

      try { recorder.requestData(); } catch { /* ignore */ }
      recorder.stop();
    });

    // Cleanup mic
    cleanupRecorder();

    const buffer = await blob.arrayBuffer();
    const bytes = new Uint8Array(buffer);
    if (!bytes || bytes.length < 512) {
      throw new Error("Recorded audio is empty or too small. Please check your microphone and try again.");
    }

    // Base64 encode in chunks to avoid stack overflow on large recordings
    let binary = "";
    const chunkSize = 0x8000;
    for (let i = 0; i < bytes.length; i += chunkSize) {
      binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunkSize));
    }
    const base64 = btoa(binary);

    const ext = mimeType.includes("ogg") ? "ogg" :
                mimeType.includes("mp4") ? "m4a" :
                mimeType.includes("wav") ? "wav" : "webm";

    return {
      base64,
      mimeType: mimeType.split(';')[0].trim(), // Strip codec params for server
      fileName: `voice-${Date.now()}.${ext}`,
      bytes: bytes.length
    };
  }

  function cancelVoiceRecording() {
    cleanupRecorder();
    return true;
  }

  function cleanupRecorder() {
    try {
      if (recorder && recorder.state === "recording") recorder.stop();
    } catch { /* ignore */ }
    try {
      if (stream) stream.getTracks().forEach(t => t.stop());
    } catch { /* ignore */ }
    stream = null;
    recorder = null;
    chunks = [];
    recordingStartTime = null;
  }

  // Expose to Blazor
  window.voice = {
    startVoiceRecording,
    stopVoiceRecording,
    cancelVoiceRecording
  };
})();
