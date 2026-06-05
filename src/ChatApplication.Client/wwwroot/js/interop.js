(() => {
    window.orbitInterop = {
        initFilePicker: function(inputId, dotnetRef, uploadUrl) {
            // Use event delegation because the input might be rendered dynamically
            // and might not exist when initFilePicker is first called.
            document.body.addEventListener('change', async (e) => {
                if (e.target && e.target.id === inputId) {
                    const input = e.target;
                    if (!input.files || input.files.length === 0) return;
                    const file = input.files[0];
                    const token = localStorage.getItem('token'); // fixed token key

                    try {
                        let result;
                        if (file.type.startsWith('image/')) {
                            const edited = await window.orbitImageEditor.editFromInput(input);
                            input.value = ''; // clear
                            if (!edited) return; // user cancelled

                            // Notify blazor upload started
                            dotnetRef.invokeMethodAsync('OnUploadStarted', edited.fileName);

                            // Convert base64 to byte array
                            const base64Data = edited.base64.split(',')[1];
                            const byteCharacters = atob(base64Data);
                            const byteNumbers = new Array(byteCharacters.length);
                            for (let i = 0; i < byteCharacters.length; i++) {
                                byteNumbers[i] = byteCharacters.charCodeAt(i);
                            }
                            const byteArray = new Uint8Array(byteNumbers);

                            result = await window.orbitUploader.uploadBlobWithProgress(
                                byteArray, edited.fileName, edited.mimeType, uploadUrl, token, dotnetRef
                            );
                        } else {
                            dotnetRef.invokeMethodAsync('OnUploadStarted', file.name);
                            result = await window.orbitUploader.uploadFileWithProgress(
                                input, uploadUrl, token, dotnetRef
                            );
                            input.value = ''; // clear
                        }

                        dotnetRef.invokeMethodAsync('OnUploadFinished', result);
                    } catch (err) {
                        dotnetRef.invokeMethodAsync('OnUploadFailed', err.message);
                        input.value = ''; // clear on error
                    }
                }
            });
        }
    };
})();
