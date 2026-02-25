(function () {
    const modalEl = document.getElementById('avatarModal');
    if (!modalEl) return; // safety if script is loaded elsewhere

    const fileInput = document.getElementById('avatarFileInput');

    const stepChoose = document.getElementById('avatarStepChoose');
    const stepCrop = document.getElementById('avatarStepCrop');

    const previewImg = document.getElementById('avatarPreview');
    const zoomSlider = document.getElementById('avatarZoom');

    const saveBtn = document.getElementById('avatarSaveBtn');
    const currentAvatar = document.getElementById('currentAvatar');

    let originalImage = null;
    let objectUrl = null;

    function resetModal() {
        stepChoose.classList.remove('d-none');
        stepCrop.classList.add('d-none');
        saveBtn.classList.add('d-none');

        fileInput.value = '';
        zoomSlider.value = '1';

        previewImg.style.transform = 'translate(-50%, -50%) scale(1)';
        previewImg.removeAttribute('src');

        if (objectUrl) {
            URL.revokeObjectURL(objectUrl);
            objectUrl = null;
        }
        originalImage = null;
    }

    modalEl.addEventListener('hidden.bs.modal', resetModal);

    zoomSlider.addEventListener('input', () => {
        const z = parseFloat(zoomSlider.value);
        previewImg.style.transform = `translate(-50%, -50%) scale(${z})`;
    });

    fileInput.addEventListener('change', () => {
        const file = fileInput.files && fileInput.files[0];
        if (!file) return;

        if (objectUrl) URL.revokeObjectURL(objectUrl);
        objectUrl = URL.createObjectURL(file);

        originalImage = new Image();
        originalImage.onload = () => {
            previewImg.src = objectUrl;

            stepChoose.classList.add('d-none');
            stepCrop.classList.remove('d-none');
            saveBtn.classList.remove('d-none');
        };
        originalImage.src = objectUrl;
    });

    async function uploadBlob(blob) {
        const form = new FormData();
        form.append('ProfileImage', blob, 'avatar.png');

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const res = await fetch('?handler=ProfilePicture', {
            method: 'POST',
            headers: token ? { 'RequestVerificationToken': token } : {},
            body: form
        });

        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Upload failed.');
        }

        return await res.json();
    }

    function cropToCircleBlob() {
        const size = 512;
        const canvas = document.createElement('canvas');
        canvas.width = size;
        canvas.height = size;

        const ctx = canvas.getContext('2d');

        // Circle clip
        ctx.save();
        ctx.beginPath();
        ctx.arc(size / 2, size / 2, size / 2, 0, Math.PI * 2);
        ctx.closePath();
        ctx.clip();

        // Center crop with zoom
        const z = parseFloat(zoomSlider.value);

        const iw = originalImage.naturalWidth;
        const ih = originalImage.naturalHeight;

        const minDim = Math.min(iw, ih);
        const cropSize = minDim / z;

        const sx = (iw - cropSize) / 2;
        const sy = (ih - cropSize) / 2;

        ctx.drawImage(originalImage, sx, sy, cropSize, cropSize, 0, 0, size, size);
        ctx.restore();

        return new Promise((resolve) => {
            canvas.toBlob((blob) => resolve(blob), 'image/png', 0.92);
        });
    }

    saveBtn.addEventListener('click', async () => {
        if (!originalImage) return;

        saveBtn.disabled = true;
        try {
            const blob = await cropToCircleBlob();
            const result = await uploadBlob(blob);

            if (result && result.imageUrl) {
                currentAvatar.src = result.imageUrl;
            }

            // Close
            const bsModal = bootstrap.Modal.getInstance(modalEl);
            bsModal.hide();
        } catch (e) {
            alert(e.message || 'Failed to update profile picture.');
        } finally {
            saveBtn.disabled = false;
        }
    });
})();