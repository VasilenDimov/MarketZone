(function () {
    const fileInput = document.getElementById("avatarFileInput");
    const previewImg = document.getElementById("avatarPreview");
    const zoomInput = document.getElementById("avatarZoom");

    const stepChoose = document.getElementById("avatarStepChoose");
    const stepCrop = document.getElementById("avatarStepCrop");

    const applyBtn = document.getElementById("applyAvatarBtn");

    const mainAvatarImg = document.getElementById("profileAvatarImg");
    const hiddenBase64 = document.getElementById("profileImageBase64");

    const modalEl = document.getElementById("avatarModal");

    if (!fileInput || !previewImg || !zoomInput || !applyBtn || !mainAvatarImg || !hiddenBase64 || !modalEl) {
        return;
    }

    let loadedImage = new Image();
    let imageReady = false;

    function showCropStep() {
        stepChoose.classList.add("d-none");
        stepCrop.classList.remove("d-none");
    }

    function showChooseStep() {
        stepCrop.classList.add("d-none");
        stepChoose.classList.remove("d-none");
        zoomInput.value = "1";
        previewImg.removeAttribute("src");
        imageReady = false;
    }

    function updatePreviewZoom() {
        const zoom = parseFloat(zoomInput.value || "1");
        previewImg.style.transform = `translate(-50%, -50%) scale(${zoom})`;
    }

    zoomInput.addEventListener("input", updatePreviewZoom);

    fileInput.addEventListener("change", () => {
        const file = fileInput.files && fileInput.files[0];
        if (!file) return;

        if (!file.type || !file.type.startsWith("image/")) {
            fileInput.value = "";
            return;
        }

        const reader = new FileReader();
        reader.onload = (e) => {
            const dataUrl = e.target.result;

            loadedImage = new Image();
            loadedImage.onload = () => {
                imageReady = true;
                previewImg.src = dataUrl;
                updatePreviewZoom();
                showCropStep();
            };
            loadedImage.src = dataUrl;
        };

        reader.readAsDataURL(file);
    });

    function renderCroppedPngBase64(size) {
        const zoom = parseFloat(zoomInput.value || "1");

        const canvas = document.createElement("canvas");
        canvas.width = size;
        canvas.height = size;

        const ctx = canvas.getContext("2d");
        if (!ctx) return null;

        const srcW = loadedImage.naturalWidth;
        const srcH = loadedImage.naturalHeight;

        if (!srcW || !srcH) return null;

        // Crop a centered square from the source image, adjusted by zoom.
        const baseCrop = Math.min(srcW, srcH);

        const crop = baseCrop / zoom;

        const sx = (srcW - crop) / 2;
        const sy = (srcH - crop) / 2;

        // Draw the crop into a circle mask.
        ctx.clearRect(0, 0, size, size);
        ctx.save();
        ctx.beginPath();
        ctx.arc(size / 2, size / 2, size / 2, 0, Math.PI * 2);
        ctx.closePath();
        ctx.clip();

        ctx.drawImage(
            loadedImage,
            sx, sy, crop, crop,
            0, 0, size, size
        );

        ctx.restore();

        return canvas.toDataURL("image/png");
    }

    applyBtn.addEventListener("click", () => {
        if (!imageReady) return;

        const base64 = renderCroppedPngBase64(256);
        if (!base64) return;

        mainAvatarImg.src = base64;
        hiddenBase64.value = base64;

        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    });

    modalEl.addEventListener("hidden.bs.modal", () => {
        fileInput.value = "";
        showChooseStep();
    });

    showChooseStep();
})();