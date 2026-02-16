document.addEventListener("DOMContentLoaded", () => {
    const DEBOUNCE_DELAY_MS = 400;
    const MIN_SEARCH_LENGTH = 3;
    
    const input = document.getElementById("filter-address-input");
    const suggestions = document.getElementById("filter-address-suggestions");
    const loading = document.getElementById("filter-address-loading");
    const hiddenLat = document.getElementById("filter-latitude");
    const hiddenLng = document.getElementById("filter-longitude");

    if (!input) return;

    let debounceTimer = null;
    let activeIndex = -1;
    let currentResults = [];

    // Debounced search
    input.addEventListener("input", () => {
        const query = input.value.trim();

        clearTimeout(debounceTimer);

        // Clear lat/lng when user types again
        if (hiddenLat) hiddenLat.value = "";
        if (hiddenLng) hiddenLng.value = "";

        // Hide suggestions immediately when input is cleared
        if (!query) {
            closeSuggestions();
            return;
        }

        if (query.length < MIN_SEARCH_LENGTH) {
            closeSuggestions();
            return;
        }

        debounceTimer = setTimeout(() => searchAddress(query), DEBOUNCE_DELAY_MS);
    });

    async function searchAddress(query) {
        loading.classList.add("show");
        closeSuggestions();

        try {
            const res = await fetch(
                `https://nominatim.openstreetmap.org/search?` +
                `q=${encodeURIComponent(query)}&format=json&addressdetails=1&limit=6`,
                { headers: { "Accept-Language": "en" } }
            );
            const data = await res.json();
            currentResults = data;
            renderSuggestions(data);
        } catch {
            closeSuggestions();
        } finally {
            loading.classList.remove("show");
        }
    }

    function renderSuggestions(results) {
        suggestions.innerHTML = "";
        activeIndex = -1;

        if (!results.length) {
            suggestions.innerHTML =
                `<div class="suggestion-item" style="color:#888;cursor:default">
                    No results found
                 </div>`;
            suggestions.classList.add("show");
            return;
        }

        results.forEach((r, i) => {
            const parts = r.display_name.split(", ");
            const main = parts.slice(0, 2).join(", ");
            const sub = parts.slice(2).join(", ");

            const item = document.createElement("div");
            item.className = "suggestion-item";
            item.innerHTML = `
                <span class="suggestion-icon">📍</span>
                <div>
                    <div class="suggestion-main">${main}</div>
                    <div class="suggestion-sub">${sub}</div>
                </div>`;

            item.addEventListener("mousedown", (e) => {
                e.preventDefault();
                selectResult(r);
            });

            suggestions.appendChild(item);
        });

        suggestions.classList.add("show");
    }

    function selectResult(result) {
        const lat = parseFloat(result.lat);
        const lng = parseFloat(result.lon);
        const displayName = result.display_name;

        input.value = displayName;
        if (hiddenLat) hiddenLat.value = lat;
        if (hiddenLng) hiddenLng.value = lng;

        closeSuggestions();
    }

    function closeSuggestions() {
        suggestions.classList.remove("show");
        suggestions.innerHTML = "";
        activeIndex = -1;
    }

    // Keyboard navigation
    input.addEventListener("keydown", (e) => {
        const items = suggestions.querySelectorAll(".suggestion-item");

        if (e.key === "ArrowDown") {
            e.preventDefault();
            activeIndex = Math.min(activeIndex + 1, items.length - 1);
            updateActive(items);
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            activeIndex = Math.max(activeIndex - 1, 0);
            updateActive(items);
        } else if (e.key === "Enter") {
            e.preventDefault();
            if (activeIndex >= 0 && currentResults[activeIndex]) {
                selectResult(currentResults[activeIndex]);
            }
        } else if (e.key === "Escape") {
            closeSuggestions();
        }
    });

    function updateActive(items) {
        items.forEach((item, i) => {
            item.classList.toggle("active", i === activeIndex);
        });
    }

    // Close on outside click
    document.addEventListener("click", (e) => {
        const wrapper = input.closest(".address-picker-wrapper");
        if (wrapper && !wrapper.contains(e.target)) {
            closeSuggestions();
        }
    });
});
