(function () {
    function ready(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn);
        } else {
            fn();
        }
    }

    function nowMs() {
        return Date.now();
    }

    function getInt(v, fallback) {
        const n = parseInt(v, 10);
        return Number.isFinite(n) ? n : fallback;
    }

    function formatSeconds(s) {
        return `${s}s`;
    }

    ready(() => {
        const buttons = Array.from(document.querySelectorAll("button[data-cooldown-key]"));
        if (!buttons.length) return;

        // Prevent double-click spam while the request is submitting (NOT the cooldown).
        const forms = new Set(buttons.map(b => b.closest("form")).filter(Boolean));
        forms.forEach((form) => {
            form.addEventListener("submit", (e) => {
                const submitter = e.submitter;
                if (!submitter) return;
                if (submitter.matches("button[data-cooldown-key]")) {
                    submitter.disabled = true;
                }
            });
        });

        function ensureTimerEl(btn) {
            const timerAttr = btn.getAttribute("data-cooldown-timer-id");
            let timerEl = null;

            if (timerAttr) timerEl = document.getElementById(timerAttr);

            if (!timerEl) {
                const next = btn.nextElementSibling;
                if (next && next.classList.contains("cooldown-timer")) timerEl = next;
            }

            if (!timerEl) {
                timerEl = document.createElement("span");
                timerEl.className = "ms-2 text-muted small cooldown-timer";
                btn.insertAdjacentElement("afterend", timerEl);
            }

            return timerEl;
        }

        function setState(btn, timerEl, remainingSec) {
            if (remainingSec > 0) {
                btn.disabled = true;
                timerEl.textContent = `(${formatSeconds(remainingSec)})`;
            } else {
                btn.disabled = false;
                timerEl.textContent = "";
            }
        }

        function startTick(btn, storageKey, seconds) {
            const timerEl = ensureTimerEl(btn);

            // Clear existing ticks
            if (btn._cooldownIntervalId) {
                clearInterval(btn._cooldownIntervalId);
                btn._cooldownIntervalId = null;
            }

            btn._cooldownIntervalId = setInterval(() => {
                const until = getInt(sessionStorage.getItem(storageKey), 0);
                const remainingMs = until - nowMs();
                const remainingSec = Math.max(0, Math.ceil(remainingMs / 1000));

                setState(btn, timerEl, remainingSec);

                if (remainingSec <= 0) {
                    sessionStorage.removeItem(storageKey);
                    clearInterval(btn._cooldownIntervalId);
                    btn._cooldownIntervalId = null;
                }
            }, 250);

            const until = getInt(sessionStorage.getItem(storageKey), 0);
            const remainingMs = until - nowMs();
            const remainingSec = Math.max(0, Math.ceil(remainingMs / 1000));
            setState(btn, timerEl, remainingSec);
        }

        function beginCooldownForKey(key, seconds) {
            const btn = buttons.find(b => (b.getAttribute("data-cooldown-key") || "") === key);
            if (!btn) return;

            const storageKey = `mz_cooldown_${key}`;
            sessionStorage.setItem(storageKey, String(nowMs() + seconds * 1000));
            startTick(btn, storageKey, seconds);
        }

        // Restore existing cooldowns (if tab refreshed mid-cooldown)
        buttons.forEach((btn) => {
            const key = btn.getAttribute("data-cooldown-key");
            const seconds = getInt(btn.getAttribute("data-cooldown-seconds"), 30);
            const storageKey = `mz_cooldown_${key}`;
            const until = getInt(sessionStorage.getItem(storageKey), 0);
            const remainingMs = until - nowMs();
            const remainingSec = Math.max(0, Math.ceil(remainingMs / 1000));

            if (remainingSec > 0) {
                startTick(btn, storageKey, seconds);
            } else {
                const timerEl = ensureTimerEl(btn);
                setState(btn, timerEl, 0);
            }
        });

        // Start cooldown ONLY if server requested it
        const start = document.getElementById("authCooldownStart");
        if (start) {
            const key = start.getAttribute("data-key") || "";
            const seconds = getInt(start.getAttribute("data-seconds"), 30);
            if (key) {
                beginCooldownForKey(key, seconds);
            }
        }
    });
})();