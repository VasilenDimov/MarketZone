document.addEventListener("DOMContentLoaded", () => {

    // address-input is now the asp-for bound field directly
    const input = document.getElementById("address-input");
    const suggestions = document.getElementById("address-suggestions");
    const loading = document.getElementById("address-loading");
    const mapContainer = document.getElementById("address-map-preview");
    const confirmedBadge = document.getElementById("address-confirmed-badge");
    const hiddenLat = document.getElementById("Latitude");
    const hiddenLng = document.getElementById("Longitude");

    if (!input) return;

    let map = null;
    let marker = null;
    let debounceTimer = null;
    let activeIndex = -1;
    let currentResults = [];

    // ── Debounced search ──
    input.addEventListener("input", () => {
        const query = input.value.trim();

        window.addressConfirmed = false;
        window.validateAll?.();

        clearTimeout(debounceTimer);
        confirmedBadge?.classList.remove("show");

        // Clear lat/lng when user types again
        if (hiddenLat) hiddenLat.value = "";
        if (hiddenLng) hiddenLng.value = "";

        // Hide map immediately when input is cleared
        if (!query) {
            hideMap();
            closeSuggestions();
            return;
        }

        if (query.length < 3) {
            closeSuggestions();
            return;
        }

        debounceTimer = setTimeout(() => searchAddress(query), 400);
    });

    async function searchAddress(query) {
        loading?.classList.add("show");
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
            loading?.classList.remove("show");
        }
    }

    function renderSuggestions(results) {
        if (!suggestions) return;

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

        results.forEach((r) => {
            const parts = (r.display_name || "").split(", ");
            const main = parts.slice(0, 2).join(", ");
            const sub = parts.slice(2).join(", ");

            const item = document.createElement("div");
            item.className = "suggestion-item";
            item.innerHTML = `
                <span class="suggestion-icon">📍</span>
                <div>
                    <div class="suggestion-main">${escapeHtml(main)}</div>
                    <div class="suggestion-sub">${escapeHtml(sub)}</div>
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
        const displayName = result.display_name || "";

        input.value = displayName;
        if (hiddenLat) hiddenLat.value = String(lat);
        if (hiddenLng) hiddenLng.value = String(lng);

        closeSuggestions();
        showMap(lat, lng);

        window.addressConfirmed = true;
        confirmedBadge?.classList.add("show");
        window.validateAll?.();
    }

    function showMap(lat, lng) {
        if (!mapContainer) return;

        mapContainer.classList.add("show");

        if (!map) {
            // Leaflet must be loaded
            if (typeof L === "undefined") return;

            map = L.map("address-map", { zoomControl: true, scrollWheelZoom: false });
            L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                attribution: "© OpenStreetMap contributors"
            }).addTo(map);
        }

        if (marker) map.removeLayer(marker);
        marker = L.marker([lat, lng]).addTo(map);
        map.setView([lat, lng], 14);

        // Fix Leaflet tile rendering inside hidden containers
        setTimeout(() => map && map.invalidateSize(), 100);
    }

    function hideMap() {
        mapContainer?.classList.remove("show");
        if (marker && map) {
            map.removeLayer(marker);
            marker = null;
        }
    }

    function closeSuggestions() {
        if (!suggestions) return;
        suggestions.classList.remove("show");
        suggestions.innerHTML = "";
        activeIndex = -1;
    }

    // ── Keyboard navigation ──
    input.addEventListener("keydown", (e) => {
        const items = suggestions?.querySelectorAll(".suggestion-item") || [];

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

    // ── Restore map if editing (existing address with coords) ──
    const existingLat = hiddenLat?.value;
    const existingLng = hiddenLng?.value;

    if (existingLat && existingLng && input.value) {
        showMap(parseFloat(existingLat), parseFloat(existingLng));
        confirmedBadge?.classList.add("show");

        // THIS is the key for your submit button validation on Edit
        window.addressConfirmed = true;
        window.validateAll?.();
    }

    function escapeHtml(str) {
        return String(str)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }
});