document.addEventListener("DOMContentLoaded", () => {

    if (!window.adDetailsConfig) return;

    const {
        adId,
        isAuthenticated,
        isOwner,
        antiForgeryToken
    } = window.adDetailsConfig;

    /* Require login */
    window.requireLogin = function () {
        alert("You need an account to use this feature.");
        window.location.href = "/Identity/Account/Register";
    };

    /* Favorites */
    if (!isAuthenticated || isOwner) return;

    const favBtn = document.getElementById("favorite-btn");
    if (!favBtn) return;

    favBtn.addEventListener("click", async () => {
        try {
            const response = await fetch("/Favorites/Toggle", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": antiForgeryToken
                },
                body: JSON.stringify(adId)
            });

            if (!response.ok)
                throw new Error("Request failed");

            const result = await response.json();

            favBtn.textContent = result.isFavorite
                ? "❤️ Remove from Favorites"
                : "🤍 Add to Favorites";
        }
        catch {
            alert("Failed to update favorites.");
        }
    });
});
