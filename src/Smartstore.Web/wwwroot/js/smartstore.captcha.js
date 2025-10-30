// Generic CAPTCHA bootstrap for Smartstore
// - Provider-agnostic init for .captcha-box containers
// - Prefers event hooks; uses a SCOPED MutationObserver as fallback
// - Batches DOM work via requestAnimationFrame and filters aggressively

(function () {
    // --- Provider registry -----------------------------------------------------
    window.CaptchaRegistry = window.CaptchaRegistry || {
        providers: {},
        pending: new Map(), // providerName -> Set(containers waiting for adapter)

        register: function (name, handler) {
            this.providers[name] = handler;
            const waitset = this.pending.get(name);
            if (waitset) {
                waitset.forEach(function (container) { try { initWithHandler(handler, container); } catch { } });
                this.pending.delete(name);
            }
        },

        get: function (name) { return this.providers[name]; }
    };

    // --- Utilities -------------------------------------------------------------
    function readConfig(container) {
        const node = container.querySelector('script.captcha-config[type="application/json"]');
        if (!node) return {};
        try { return JSON.parse(node.textContent || '{}'); } catch { return {}; }
    }

    function initWithHandler(handler, container) {
        if (!container || container.__captchaInited === true) return;

        const mode = container.getAttribute('data-captcha-mode') || 'interactive';
        const config = readConfig(container);
        const elementIdAttr = container.getAttribute('data-captcha-element') || null;
        const elementId = elementIdAttr || config.elementId || null;

        try {
            handler.init({ container: container, mode: mode, elementId: elementId, config: config });
            container.__captchaInited = true;
        }
        catch { }
    }

    function initContainer(container) {
        if (!container || container.__captchaInited === true) return;

        const provider = container.getAttribute('data-captcha-provider');
        if (!provider) return;

        const handler = window.CaptchaRegistry.get(provider);
        if (!handler) {
            // Defer until adapter is registered
            var set = window.CaptchaRegistry.pending.get(provider);
            if (!set) { set = new Set(); window.CaptchaRegistry.pending.set(provider, set); }
            set.add(container);
            return;
        }

        initWithHandler(handler, container);
    }

    function initAll(scope) {
        (scope || document).querySelectorAll('.captcha-box').forEach(initContainer);
    }

    // --- Fallback: scoped MutationObserver ------------------------------------
    // Choose a narrow root: prefer an explicit [data-captcha-scope], then #content, then body
    const scopedRoot = document.querySelector('[data-captcha-scope]') || document.getElementById('content') || document.body || document.documentElement;

    const pending = new Set();
    var scheduled = false;

    function scheduleFlush() {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(() => {
            scheduled = false;
            pending.forEach((node) => {
                if (!(node instanceof Element)) return;
                if (node.classList && node.classList.contains('captcha-box')) initContainer(node);
                if (node.querySelectorAll) node.querySelectorAll('.captcha-box').forEach(initContainer);
            });
            pending.clear();
        });
    }

    const observer = new MutationObserver((mutations) => {
        for (var i = 0; i < mutations.length; i++) {
            const m = mutations[i];
            if (!m.addedNodes || m.addedNodes.length === 0) continue;
            for (var j = 0; j < m.addedNodes.length; j++) {
                const n = m.addedNodes[j];
                // Filter to Elements only; ignore text/comment nodes
                if (n && n.nodeType === 1) pending.add(n);
            }
        }
        if (pending.size) scheduleFlush();
    });

    function startObserver() {
        try { observer.observe(scopedRoot, { childList: true, subtree: true }); } catch { }
    }

    function stopObserver() {
        try { observer.disconnect(); } catch { }
    }

    // Expose pause/resume helpers for bulk DOM ops
    window.CaptchaObserver = { pause: stopObserver, resume: startObserver };

    // --- Boot sequence ---------------------------------------------------------
    function boot() {
        initAll(document);
        startObserver();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();