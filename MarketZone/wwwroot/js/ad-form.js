document.addEventListener("DOMContentLoaded", () => {

    const existingImages =
        (window.adFormConfig && window.adFormConfig.existingImages) || [];

    /* Elements */
    const form = document.getElementById("ad-form");
    const submitBtn = document.getElementById("submit-btn");

    // Title
    const titleInput = document.getElementById("Title");
    const titleError = document.querySelector("[data-valmsg-for='Title']");

    // Description
    const descInput = document.getElementById("Description");
    const descError = document.querySelector("[data-valmsg-for='Description']");

    // Price
    const priceInput = document.getElementById("Price");
    const priceError = document.querySelector("[data-valmsg-for='Price']");

    // Address
    const addrInput = document.getElementById("address-input");
    const addrError = document.querySelector("[data-valmsg-for='Address']");

    // Category
    const categoryInput = document.querySelector("#CategoryId") ||
        document.querySelector("[name='CategoryId']");
    const categoryError = document.querySelector("[data-valmsg-for='CategoryId']");

    // Condition
    const conditionInputs = document.querySelectorAll("[name='Condition']");
    const conditionError = document.querySelector("[data-valmsg-for='Condition']");

    // Images
    const grid = document.getElementById("image-grid");
    const fileInput = document.getElementById("image-input");
    const imageError = document.querySelector("[data-valmsg-for='Images']");

    // Category picker
    const list = document.getElementById("category-list");
    const backBtn = document.getElementById("back-btn");
    const breadcrumb = document.getElementById("breadcrumb");

    let parentStack = [];
    let breadcrumbStack = [];
    let currentParentId = null;
    let files = [];
    const MAX_IMAGES = 10;

    /* ── Validators ── */
    function validateTitle() {
        const val = titleInput?.value.trim() ?? "";
        const ok = val.length >= 1 && val.length <= 100;
        if (titleError) titleError.textContent = ok ? "" : "Title is required (max 100 chars).";
        return ok;
    }

    function validateDesc() {
        const val = descInput?.value.trim() ?? "";
        const ok = val.length >= 40 && val.length <= 5000; // (you said ignore this for now)
        if (descError) descError.textContent = ok ? "" : "Description must be at least 40 characters.";
        return ok;
    }

    function validatePrice() {
        const val = parseFloat(priceInput?.value ?? "0");
        const ok = val > 0;
        if (priceError) priceError.textContent = ok ? "" : "Price must be greater than 0.";
        return ok;
    }

    function validateAddress() {
        // Check the flag from address-picker.js
        const confirmed = window.addressConfirmed === true;
        if (addrError) addrError.textContent = confirmed ? "" : "Please select an address from the suggestions.";
        return confirmed;
    }

    function validateCategory() {
        const val = categoryInput?.value ?? "0";
        const ok = val !== "0" && val !== "";
        if (categoryError) categoryError.textContent = ok ? "" : "Please select a category.";
        return ok;
    }

    function validateCondition() {
        const checked = [...conditionInputs].some(r => r.checked);
        if (conditionError) conditionError.textContent = checked ? "" : "Please select a condition.";
        return checked;
    }

    function validateImages() {
        const count = existingImages.length + files.length;
        const ok = count > 0;
        if (imageError) imageError.textContent = ok ? "" : "At least one image is required.";
        return ok;
    }

    function validateAll() {
        const ok =
            validateTitle() &
            validateDesc() &
            validatePrice() &
            validateAddress() &
            validateCategory() &
            validateCondition() &
            validateImages();

        if (submitBtn) {
            submitBtn.disabled = !ok;
        }

        return !!ok;
    }

    /* Show all errors on load & disable submit */
    window.validateAll = validateAll;
    validateAll();

    /* Real-time listeners */
    titleInput?.addEventListener("input", validateAll);
    descInput?.addEventListener("input", validateAll);
    priceInput?.addEventListener("input", validateAll);
    addrInput?.addEventListener("input", validateAll);
    conditionInputs.forEach(r => r.addEventListener("change", validateAll));

    / Category picker */
    if (list) {
        const selectedId = parseInt(categoryInput?.value || "0", 10);

        // If editing and category is already selected, restore the picker UI
        if (selectedId > 0) {
            fetch(`/Ad/GetCategoryPath?categoryId=${selectedId}`)
                .then(r => r.json())
                .then(path => {
                    if (!path || !path.length) {
                        return loadCategories(null);
                    }

                    // path: root -> leaf
                    const leaf = path[path.length - 1];
                    const parent = path.length >= 2 ? path[path.length - 2] : null;

                    // Setup stacks to match your back button logic
                    parentStack = [];
                    breadcrumbStack = [];

                    if (path.length >= 2) {
                        breadcrumbStack = path.slice(0, -1).map(x => x.name);
                        parentStack = [null, ...path.slice(0, -2).map(x => x.id)];
                    } else {
                        breadcrumbStack = [];
                        parentStack = [];
                    }

                    return loadCategories(parent ? parent.id : null);
                })
                .catch(() => loadCategories(null));
        } else {
            // Create page
            loadCategories(null);
        }
    }

    function loadCategories(parentId, label = null) {
        if (label) breadcrumbStack.push(label);

        return fetch(`/Ad/GetChildren?parentId=${parentId ?? ""}`)
            .then(r => r.json())
            .then(data => {
                list.innerHTML = "";
                currentParentId = parentId;

                backBtn.style.display =
                    parentStack.length > 0 ? "inline-block" : "none";
                breadcrumb.textContent = breadcrumbStack.join(" → ");

                const selectedId = parseInt(categoryInput?.value || "0", 10);

                data.forEach(c => {
                    const li = document.createElement("li");
                    li.className = "p-2 border-bottom";
                    li.style.cursor = "pointer";
                    li.textContent = c.name;

                    // Highlight selected leaf if present in current list
                    if (!c.hasChildren && selectedId === c.id) {
                        li.classList.add("bg-light");
                        breadcrumb.textContent = [...breadcrumbStack, c.name].join(" → ");
                    }

                    if (c.hasChildren) {
                        li.innerHTML += " ➜";
                        li.onclick = () => {
                            parentStack.push(currentParentId);
                            loadCategories(c.id, c.name);
                        };
                    } else {
                        li.onclick = () => {
                            categoryInput.value = c.id;
                            [...list.children].forEach(x => x.classList.remove("bg-light"));
                            li.classList.add("bg-light");

                            breadcrumb.textContent = [...breadcrumbStack, c.name].join(" → ");

                            validateAll();
                        };
                    }
                    list.appendChild(li);
                });
            });
    }

    if (backBtn) {
        backBtn.onclick = () => {
            currentParentId = parentStack.pop();
            breadcrumbStack.pop();
            categoryInput.value = "0";
            [...list.children].forEach(x => x.classList.remove("bg-light"));
            validateAll();
            loadCategories(currentParentId);
        };
    }

    /* Image upload */
    if (!grid || !fileInput) return;

    renderGrid();

    function renderGrid() {
        grid.innerHTML = "";

        existingImages.forEach((url, index) => {
            const box = document.createElement("div");
            box.className = "image-box";
            box.innerHTML = `
        <img src="${url}">
        <div class="image-actions"><button type="button">🗑</button></div>`;

            box.querySelector("button").onclick = e => {
                e.stopPropagation();

                const urlToRemove = url;

                existingImages.splice(index, 1);

                const escaped = CSS.escape(urlToRemove);
                document.querySelector(`input.existing-image-input[data-url="${escaped}"]`)?.remove();

                renderGrid();
                validateAll();
            };

            grid.appendChild(box);
        });

        files.forEach((file, index) => {
            const box = document.createElement("div");
            box.className = "image-box";
            const reader = new FileReader();
            reader.onload = e => {
                box.innerHTML = `
                    <img src="${e.target.result}">
                    <div class="image-actions"><button type="button">🗑</button></div>`;
                box.querySelector("button").onclick = ev => {
                    ev.stopPropagation();
                    files.splice(index, 1);
                    syncInputFiles();
                    renderGrid();
                    validateAll();
                };
            };
            reader.readAsDataURL(file);
            grid.appendChild(box);
        });

        const used = existingImages.length + files.length;
        for (let i = used; i < MAX_IMAGES; i++) {
            const empty = document.createElement("div");
            empty.className = "image-box";
            empty.innerHTML = "<span>+</span>";
            empty.onclick = () => fileInput.click();
            grid.appendChild(empty);
        }
    }

    fileInput.addEventListener("change", e => {
        for (let file of e.target.files) {
            if (files.length + existingImages.length < MAX_IMAGES) files.push(file);
        }
        fileInput.value = "";
        syncInputFiles();
        renderGrid();
        validateAll();
    });

    function syncInputFiles() {
        const dt = new DataTransfer();
        files.forEach(f => dt.items.add(f));
        fileInput.files = dt.files;
    }

    /* Final guard on submit */
    form?.addEventListener("submit", e => {
        if (!validateAll()) e.preventDefault();
    });

});