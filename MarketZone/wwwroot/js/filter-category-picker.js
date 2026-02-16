document.addEventListener("DOMContentLoaded", () => {
    const categoryInput = document.getElementById("filter-category-input");
    const list = document.getElementById("filter-category-list");
    const backBtn = document.getElementById("filter-back-btn");
    const breadcrumb = document.getElementById("filter-breadcrumb");

    if (!list || !categoryInput) return;

    let parentStack = [];
    let breadcrumbStack = [];
    let currentParentId = null;

    // Load initial categories
    loadCategories(null);

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
                            categoryInput.value = c.id;
                            [...list.children].forEach(x => x.classList.remove("bg-light"));
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
            categoryInput.value = "0";
            [...list.children].forEach(x => x.classList.remove("bg-light"));
            loadCategories(currentParentId);
        };
    }
});
