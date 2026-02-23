(function () {
    const EMAIL_KEY = "mz_login_email";
    const PASS_KEY = "mz_login_password";

    function ready(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn);
        } else {
            fn();
        }
    }

    ready(() => {
        const form = document.getElementById("account");
        const emailInput = document.getElementById("Input_Email");
        const passInput = document.getElementById("Input_Password");

        if (!form || !emailInput || !passInput) return;

        // Restore saved values (only within the same tab/session)
        const savedEmail = sessionStorage.getItem(EMAIL_KEY);
        const savedPass = sessionStorage.getItem(PASS_KEY);

        if (savedEmail && !emailInput.value) emailInput.value = savedEmail;
        if (savedPass && !passInput.value) passInput.value = savedPass;

        // Save on typing
        emailInput.addEventListener("input", () => {
            sessionStorage.setItem(EMAIL_KEY, emailInput.value || "");
        });

        passInput.addEventListener("input", () => {
            sessionStorage.setItem(PASS_KEY, passInput.value || "");
        });

        // Before any submit (login / resend / forgot), persist the latest values
        form.addEventListener("submit", () => {
            sessionStorage.setItem(EMAIL_KEY, emailInput.value || "");
            sessionStorage.setItem(PASS_KEY, passInput.value || "");
        });
    });
})();