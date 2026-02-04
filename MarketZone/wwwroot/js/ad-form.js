document.addEventListener("DOMContentLoaded", () => {

    /* Config */
    const existingImages =
        (window.adFormConfig && window.adFormConfig.existingImages) || [];

    /* Category picker */
    const list = document.getElementById("category-list");
    const hiddenInput = document.getElementById("CategoryId");
    const backBtn = document.getElementById("back-btn");
    const breadcrumb = document.getElementById("breadcrumb");

    let parentStack = [];
    let breadcrumbStack = [];
    let currentParentId = null;

    if (list) {
        loadCategories(null);
    }

    function loadCategories(parentId, label = null) {
        if (label) breadcrumbStack.push(label);

        fetch(`/Ad/GetChildren?parentId=${parentId ?? ""}`)
            .then(r => r.json())
            .then(data => {
                list.innerHTML = "";
                currentParentId = parentId;

                backBtn.style.display =
                    parentStack.length > 0 ? "inline-block" : "none";

                breadcrumb.textContent = breadcrumbStack.join(" → ");

                data.forEach(c => {
                    const li = document.createElement("li");
                    li.className = "p-2 border-bottom";
                    li.style.cursor = "pointer";
                    li.textContent = c.name;

                    if (c.hasChildren) {
                        li.innerHTML += " ➜";
                        li.onclick = () => {
                            parentStack.push(currentParentId);
                            loadCategories(c.id, c.name);
                        };
                    } else {
                        li.onclick = () => {
                            hiddenInput.value = c.id;
                            [...list.children].forEach(x =>
                                x.classList.remove("bg-light"));
                            li.classList.add("bg-light");
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
            loadCategories(currentParentId);
        };
    }

    /*  image upload.*/
    const grid = document.getElementById("image-grid");
    const input = document.getElementById("image-input");
    const errorSpan =
        document.querySelector("[data-valmsg-for='Images']");

    if (!grid || !input) return;

    const MAX_IMAGES = 10;
    let files = [];

    renderGrid();

    function renderGrid() {
        grid.innerHTML = "";

        // Existing images
        existingImages.forEach((url, index) => {
            const box = document.createElement("div");
            box.className = "image-box";

            box.innerHTML = `
                <img src="${url}">
                <div class="image-actions">
                    <button type="button">🗑</button>
                </div>
            `;

            box.querySelector("button").onclick = e => {
                e.stopPropagation();
                existingImages.splice(index, 1);

                const hiddenInputs =
                    document.querySelectorAll("input[name^='ExistingImageUrls']");
                hiddenInputs[index]?.remove();

                renderGrid();
            };

            grid.appendChild(box);
        });

        // New images
        files.forEach((file, index) => {
            const box = document.createElement("div");
            box.className = "image-box";

            const reader = new FileReader();
            reader.onload = e => {
                box.innerHTML = `
                    <img src="${e.target.result}">
                    <div class="image-actions">
                        <button type="button">🗑</button>
                    </div>
                `;

                box.querySelector("button").onclick = ev => {
                    ev.stopPropagation();
                    files.splice(index, 1);
                    syncInputFiles();
                    renderGrid();
                };
            };

            reader.readAsDataURL(file);
            grid.appendChild(box);
        });

        // Empty slots
        const used = existingImages.length + files.length;
        for (let i = used; i < MAX_IMAGES; i++) {
            const empty = document.createElement("div");
            empty.className = "image-box";
            empty.innerHTML = "<span>+</span>";
            empty.onclick = () => input.click();
            grid.appendChild(empty);
        }

        validateImages();
    }

    input.addEventListener("change", e => {
        for (let file of e.target.files) {
            if (files.length + existingImages.length < MAX_IMAGES) {
                files.push(file);
            }
        }

        input.value = "";
        syncInputFiles();
        renderGrid();
    });

    function syncInputFiles() {
        const dt = new DataTransfer();
        files.forEach(f => dt.items.add(f));
        input.files = dt.files;
    }

    function validateImages() {
        const count = existingImages.length + files.length;
        errorSpan.textContent =
            count === 0 ? "At least one image is required." : "";
    }
});
