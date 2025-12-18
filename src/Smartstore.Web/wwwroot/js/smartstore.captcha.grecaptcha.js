// Google reCAPTCHA adapter for Smartstore generic CAPTCHA bootstrap
// - Supports v2 (checkbox + invisible) and v3 (score-based)
// - Expects the Google API script to be loaded by the asset registry
// - Integrates with the generic bootstrap (CaptchaRegistry)

(function () {

    /**
     * Waits for the reCAPTCHA API to be fully loaded.
     * Adjusted to check for EITHER .execute (v3) OR .render (v2).
     */
    function waitForRecaptchaReady(check, onReady, timeoutMs) {
        const start = Date.now();
        (function tick() {
            if (check()) return onReady();
            if (Date.now() - start > (timeoutMs || 8000)) return; // Timed out
            setTimeout(tick, 50);
        })();
    }

    /**
     * Helper to check if the Google API is actually ready for the specific mode.
     * v2 needs .render, v3 needs .execute.
     */
    function isRecaptchaApiReady() {
        return window.grecaptcha && (window.grecaptcha.execute || window.grecaptcha.render);
    }

    function ensureResponseInput(form) {
        var input = form.querySelector('#g-recaptcha-response');
        if (!input) {
            input = document.createElement('input');
            input.type = 'hidden';
            input.id = 'g-recaptcha-response';
            input.name = 'g-recaptcha-response';
            form.appendChild(input);
        }
        return input;
    }

    function ensureActionInput(form, action) {
        var input = form.querySelector('input[name="captcha-action"]');
        if (!input) {
            input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'captcha-action';
            form.appendChild(input);
        }
        input.value = action;
        return input;
    }

    function disableSubmitTemporarily(form) {
        const btn = form.querySelector('[type="submit"]');
        if (btn) {
            btn.disabled = true;
            setTimeout(function () {
                try { btn.disabled = false; } catch { }
            }, 4000);
        }
    }

    // --- V3 Logic (Score based) ---
    function bindV3(ctx) {
        const container = ctx.container;
        const cfg = ctx.config || {};
        const siteKey = cfg.siteKey;
        const action = cfg.defaultAction || 'submit';

        if (!siteKey) return;

        const form = container.closest('form');
        if (!form || form.__captchaV3Bound) return;
        form.__captchaV3Bound = true;

        form.addEventListener('submit', (e) => {
            // Re-entrancy guard: when set, allow normal pipeline (incl. unobtrusive) to proceed
            if (form.__captchaResubmit === true) return;

            // Let native validity run first; do nothing if invalid
            if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;

            e.preventDefault();
            disableSubmitTemporarily(form);

            const exec = () => {
                // Ensure execute is available before calling
                if (!window.grecaptcha || !grecaptcha.execute) {
                    console.error("reCAPTCHA v3 execute method not found.");
                    return;
                }

                grecaptcha.execute(siteKey, { action: action }).then((token) => {
                    ensureResponseInput(form).value = token;
                    // Provider-agnostic hint for servers that compare actions
                    ensureActionInput(form, action);

                    // If unobtrusive AJAX is enabled, trigger a jQuery submit with a reentrancy guard
                    const isUnobtrusive = !!(form.getAttribute('data-ajax') || (window.jQuery && jQuery(form).data('ajax')));
                    if (isUnobtrusive && window.jQuery) {
                        form.__captchaResubmit = true; // guard ON
                        try { jQuery(form).trigger('submit'); } finally { form.__captchaResubmit = false; } // guard OFF
                    } else {
                        // Fallback: native submit
                        form.submit();
                    }
                }, function (err) {
                    console.error("reCAPTCHA v3 execution failed", err);
                    // Soft failure: allow form submit without token or show a message depending on policy
                });
            };

            // Wait for grecaptcha to be available
            if (isRecaptchaApiReady()) {
                exec();
            } else {
                waitForRecaptchaReady(isRecaptchaApiReady, exec, 8000);
            }
        });
    }

    // --- V2 Logic (Checkbox or Invisible) ---
    function bindV2(ctx) {
        const container = ctx.container;
        const cfg = ctx.config || {};
        const siteKey = cfg.siteKey;
        const size = (cfg.size || 'normal');
        const theme = (cfg.theme || 'light');
        let elementId = ctx.elementId; // expected placeholder id

        // Fallback: try to find the element by class if ID is missing
        if (!elementId) {
            const el = container.querySelector('.g-recaptcha[id]');
            if (el) elementId = el.id;
        }

        if (!siteKey || !elementId) return;

        const form = container.closest('form');

        function render() {
            // Safety check: ensure 'render' is available. 
            // This is crucial for v2, as 'execute' might not be present immediately.
            if (!window.grecaptcha || !window.grecaptcha.render) return;

            try {
                const badge = (size === 'invisible') ? (cfg.badge || 'bottomright') : undefined;

                const widgetId = grecaptcha.render(elementId, {
                    sitekey: siteKey,
                    size: size === 'invisible' ? 'invisible' : size,
                    theme: theme,
                    badge: badge,
                    callback: (token) => {
                        if (!form) return;
                        ensureResponseInput(form).value = token;

                        const isInvisible = (size === 'invisible');
                        // For invisible v2, auto-submit after challenge is solved
                        if (isInvisible) {
                            var isUnobtrusive = !!(form.getAttribute('data-ajax') || (window.jQuery && jQuery(form).data('ajax')));
                            if (isUnobtrusive && window.jQuery) {
                                // Resubmit through jQuery to keep unobtrusive AJAX pipeline
                                form.__captchaResubmit = true;
                                try { jQuery(form).trigger('submit'); } finally { form.__captchaResubmit = false; }
                            } else {
                                form.submit();
                            }
                        }
                    },
                    'expired-callback': () => {
                        if (form) ensureResponseInput(form).value = '';
                    }
                });

                // Invisible v2 specific: intercept submit to trigger manual execution
                if (size === 'invisible' && form && !form.__captchaV2InvisibleBound) {
                    form.__captchaV2InvisibleBound = true;
                    form.addEventListener('submit', (e) => {
                        // Allow pass-through on the guarded resubmit
                        if (form.__captchaResubmit === true) return;

                        if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;
                        e.preventDefault();
                        disableSubmitTemporarily(form);

                        // Execute the rendered widget
                        grecaptcha.execute(widgetId);
                    });
                }
            } catch (e) {
                console.error("Failed to render reCAPTCHA v2", e);
            }
        }

        // Wait logic: we accept EITHER render OR execute to be present to consider it "ready".
        // This fixes the issue where v2 waits forever for 'execute'.
        if (isRecaptchaApiReady()) {
            render();
        } else {
            waitForRecaptchaReady(isRecaptchaApiReady, render, 8000);
        }
    }

    // Register adapter
    if (window.CaptchaRegistry) {
        window.CaptchaRegistry.register('Captcha.GoogleRecaptcha', {
            init: function (ctx) {
                if (!ctx || !ctx.container) return;
                // Distinguish logic based on mode passed from server
                if (ctx.mode === 'score') return bindV3(ctx);
                return bindV2(ctx);
            }
        });
    }
})();