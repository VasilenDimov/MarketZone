document.addEventListener("DOMContentLoaded", () => {
    initAddressPicker();
    initCategoryPicker();
});

/* ─── Address Autocomplete (Nominatim) ─── */
function initAddressPicker() {
    const input = document.getElementById("filter-address-input");
    const suggestions = document.getElementById("filter-address-suggestions");

    if (!input || !suggestions) return;

    let debounceTimer;

    document.addEventListener("click", (e) => {
        if (!input.contains(e.target) && !suggestions.contains(e.target)) {
            suggestions.classList.remove("show");
        }
    });

    input.addEventListener("input", () => {
        const query = input.value.trim();
        clearTimeout(debounceTimer);

        if (query.length < 3) {
            suggestions.classList.remove("show");
            return;
        }

        debounceTimer = setTimeout(() => {
            fetch(`https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&addressdetails=1&limit=6`, {
                headers: { "Accept-Language": "en" }
            })
                .then(res => res.json())
                .then(data => {
                    suggestions.innerHTML = "";
                    if (!data.length) {
                        suggestions.classList.remove("show");
                        return;
                    }

                    data.forEach(item => {
                        const div = document.createElement("div");
                        div.className = "suggestion-item";

                        const displayName = item.display_name; // keep full
                        div.textContent = displayName;

                        div.addEventListener("click", () => {
                            input.value = displayName;
                            suggestions.classList.remove("show");
                        });

                        suggestions.appendChild(div);
                    });

                    suggestions.classList.add("show");
                })
                .catch(() => suggestions.classList.remove("show"));
        }, 300);
    });
}

/* ─── Hierarchical Category Picker ─── */
function initCategoryPicker() {
    const container = document.getElementById("category-picker-container");
    const dropdown = document.getElementById("category-dropdown-menu");
    const list = document.getElementById("filter-category-list");
    const input = document.getElementById("filter-category-id");
    const display = document.getElementById("selected-category-name");
    const backBtn = document.getElementById("cat-back-btn");
    const breadcrumb = document.getElementById("cat-breadcrumb");
    const clearBtn = document.getElementById("clear-category-btn");

    if (!container || !dropdown || !list || !input || !display || !backBtn || !breadcrumb || !clearBtn) return;

    let parentStack = [];
    let breadcrumbStack = ["All Categories"];
    let currentParentId = null;

    container.addEventListener("click", (e) => {
        if (dropdown.contains(e.target)) return;

        const isVisible = dropdown.classList.contains("show");
        if (isVisible) {
            dropdown.classList.remove("show");
            return;
        }

        dropdown.classList.add("show");

        // Load initial
        if (list.children.length <= 1) {
            loadCategories(null);
        }
    });

    document.addEventListener("click", (e) => {
        if (!container.contains(e.target)) {
            dropdown.classList.remove("show");
        }
    });

    clearBtn.addEventListener("click", (e) => {
        e.stopPropagation();
        input.value = "";
        display.textContent = "All Categories";
        dropdown.classList.remove("show");

        parentStack = [];
        breadcrumbStack = ["All Categories"];
        loadCategories(null);
    });

    backBtn.addEventListener("click", (e) => {
        e.stopPropagation();
        currentParentId = parentStack.pop() ?? null;
        breadcrumbStack.pop();
        loadCategories(currentParentId);
    });

    function loadCategories(parentId) {
        backBtn.style.display = parentStack.length > 0 ? "inline-block" : "none";
        breadcrumb.textContent = breadcrumbStack.join(" > ");

        list.innerHTML = `<li class="p-3 text-center text-muted"><span class="spinner-border spinner-border-sm"></span></li>`;

        fetch(`/Ad/GetChildren?parentId=${parentId ?? ""}`)
            .then(r => r.json())
            .then(data => {
                list.innerHTML = "";
                currentParentId = parentId;

                data.forEach(c => {
                    const li = document.createElement("li");
                    li.innerHTML = `<span>${c.name}</span>`;

                    if (c.hasChildren) {
                        li.innerHTML += `<span class="text-muted">›</span>`;
                        li.addEventListener("click", (e) => {
                            e.stopPropagation();
                            parentStack.push(currentParentId);
                            breadcrumbStack.push(c.name);
                            loadCategories(c.id);
                        });
                    } else {
                        li.addEventListener("click", (e) => {
                            e.stopPropagation();
                            input.value = c.id;
                            display.textContent = c.name;
                            dropdown.classList.remove("show");
                        });
                    }

                    list.appendChild(li);
                });
            });
    }
}
