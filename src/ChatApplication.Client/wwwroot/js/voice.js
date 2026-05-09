(() => {
  // Very small voice-recorder helper for Blazor WASM.
  // Uses MediaRecorder and returns base64 audio data.
  let recorder = null;
  let chunks = [];
  let stream = null;

  function pickMimeType() {
    const candidates = [
      "audio/ogg;codecs=opus",
      "audio/ogg",
      "audio/webm;codecs=opus",
      "audio/webm",
      "audio/mp4"
    ];
    for (const c of candidates) {
      if (window.MediaRecorder && MediaRecorder.isTypeSupported && MediaRecorder.isTypeSupported(c)) return c;
    }
    return "";
  }

  async function startVoiceRecording() {
    if (!navigator.mediaDevices?.getUserMedia) {
      throw new Error("Microphone is not supported in this browser.");
    }
    if (recorder && recorder.state === "recording") return true;

    chunks = [];
    stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true
      }
    });
    const mimeType = pickMimeType();
    recorder = new MediaRecorder(stream, mimeType ? { mimeType } : undefined);

    recorder.addEventListener("dataavailable", (e) => {
      if (e.data && e.data.size > 0) chunks.push(e.data);
    });

    // Use a timeslice to ensure dataavailable fires consistently across browsers.
    recorder.start(250);
    return true;
  }

  async function stopVoiceRecording() {
    if (!recorder) throw new Error("Recorder not started.");
    if (recorder.state !== "recording") throw new Error("Recorder is not recording.");

    const mimeType = recorder.mimeType || "audio/webm";

    const blob = await new Promise((resolve, reject) => {
      recorder.addEventListener("stop", () => resolve(new Blob(chunks, { type: mimeType })), { once: true });
      recorder.addEventListener("error", (e) => reject(e.error || new Error("Recording error")), { once: true });
      try { recorder.requestData(); } catch {}
      recorder.stop();
    });

    // stop mic
    try { stream?.getTracks()?.forEach(t => t.stop()); } catch {}
    stream = null;
    recorder = null;

    const buffer = await blob.arrayBuffer();
    const bytes = new Uint8Array(buffer);
    if (!bytes || bytes.length < 512) {
      throw new Error("Recorded audio is empty. Please allow microphone access and try again.");
    }

    // base64 encode
    let binary = "";
    const chunkSize = 0x8000;
    for (let i = 0; i < bytes.length; i += chunkSize) {
      binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunkSize));
    }
    const base64 = btoa(binary);

    const ext = mimeType.includes("ogg") ? "ogg" : mimeType.includes("mp4") ? "m4a" : "webm";
    return { base64, mimeType, fileName: `voice-${Date.now()}.${ext}`, bytes: bytes.length };
  }

  function cancelVoiceRecording() {
    try {
      if (recorder && recorder.state === "recording") recorder.stop();
    } catch {}
    try { stream?.getTracks()?.forEach(t => t.stop()); } catch {}
    stream = null;
    recorder = null;
    chunks = [];
    return true;
  }

  window.voice = {
    startVoiceRecording,
    stopVoiceRecording,
    cancelVoiceRecording
  };
})();

