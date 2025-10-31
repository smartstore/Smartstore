// Google reCAPTCHA adapter for Smartstore generic CAPTCHA bootstrap
// - Supports v2 (checkbox + invisible) and v3 (score-based)
// - Expects the Google API script to be loaded by the asset registry
// - Integrates with the generic bootstrap (CaptchaRegistry)

(function () {
    // Simple readiness helper for grecaptcha
    function waitForRecaptchaReady(check, onReady, timeoutMs) {
        const start = Date.now();
        (function tick() {
            if (check()) return onReady();
            if (Date.now() - start > (timeoutMs || 8000)) return; // give up silently
            setTimeout(tick, 50);
        })();
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
        if (btn) { btn.disabled = true; setTimeout(function () { try { btn.disabled = false; } catch { } }, 4000); }
    }

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
                }, function () {
                    // Soft failure: allow form submit without token or show a message depending on policy
                });
            };

            // Wait for grecaptcha.execute to be available
            if (window.grecaptcha && grecaptcha.execute) {
                exec();
            }
            else {
                waitForRecaptchaReady(() => { return window.grecaptcha && grecaptcha.execute; }, exec, 8000);
            }
        });
    }

    function bindV2(ctx) {
        const container = ctx.container;
        const cfg = ctx.config || {};
        const siteKey = cfg.siteKey;
        const size = (cfg.size || 'normal');
        const theme = (cfg.theme || 'light');
        const elementId = ctx.elementId; // expected placeholder id

        if (!elementId) {
            const el = container.querySelector('.g-recaptcha[id]');
            if (el) elementId = el.id;
        }

        if (!siteKey || !elementId) return;

        const form = container.closest('form');

        function render() {
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
                    if (isInvisible) {
                        var isUnobtrusive = !!(form.getAttribute('data-ajax') || (window.jQuery && jQuery(form).data('ajax')));
                        if (isUnobtrusive && window.jQuery) {
                            // Resubmit through jQuery to keep unobtrusive AJAX pipeline
                            form.__captchaResubmit = true;
                            try { jQuery(form).trigger('submit'); } finally { form.__captchaResubmit = false; }
                        }
                        else {
                            form.submit();
                        }
                    }
                },
                'expired-callback': () => {
                    if (form) ensureResponseInput(form).value = '';
                }
            });

            // Invisible v2: intercept submit to trigger execution
            if (size === 'invisible' && form && !form.__captchaV2InvisibleBound) {
                form.__captchaV2InvisibleBound = true;
                form.addEventListener('submit', (e) => {
                    // Allow pass-through on the guarded resubmit
                    if (form.__captchaResubmit === true) return;

                    if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;
                    e.preventDefault();
                    disableSubmitTemporarily(form);
                    grecaptcha.execute(widgetId);
                });
            }
        }

        if (window.grecaptcha && grecaptcha.execute) {
            render();
        }
        else {
            waitForRecaptchaReady(() => { return window.grecaptcha && grecaptcha.execute; }, render, 8000);
        }
    }

    // Register adapter
    window.CaptchaRegistry.register('Captcha.GoogleRecaptcha', {
        init: function (ctx) {
            if (!ctx || !ctx.container) return;
            if (ctx.mode === 'score') return bindV3(ctx);
            return bindV2(ctx);
        }
    });
})();