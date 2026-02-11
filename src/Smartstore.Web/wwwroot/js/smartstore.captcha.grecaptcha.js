// Google reCAPTCHA adapter for Smartstore generic CAPTCHA bootstrap
// - Supports v2 (checkbox + invisible) and v3 (score-based)
// - Expects the Google API script to be loaded by the asset registry
// - Integrates with the generic bootstrap (CaptchaRegistry)

(function () {
    'use strict';   

    var H = window.CaptchaHelpers;
    var PROVIDER = 'Captcha.GoogleRecaptcha';
    var RESPONSE_FIELD = 'g-recaptcha-response';
    var ACTION_FIELD = 'captcha-action';
    var GUARD_NAME = '__captchaResubmit';

    /**
     * Helper to check if the Google API is actually ready for the specific mode.
     * v2 needs .render, v3 needs .execute.
     */
    function isRecaptchaApiReady() {
        return window.grecaptcha && (window.grecaptcha.execute || window.grecaptcha.render);
    }

    function ensureActionInput(form, action) {
        var input = form.querySelector('input[name="' + ACTION_FIELD + '"]');
        if (!input) {
            input = document.createElement('input');
            input.type = 'hidden';
            input.name = ACTION_FIELD;
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
        if (!form) return;

        H.bindSubmit(form, GUARD_NAME, function (e, helpers) {
            // jQuery unobtrusive validation
            const $form = $(form);
            const validator = $form.data('validator');
            if (validator) {
                $form.validate();
                if (!$form.valid()) return true; // Allow event to proceed for validation
            }

            // Block and execute reCAPTCHA v3
            disableSubmitTemporarily(form);

            const exec = () => {
                if (!window.grecaptcha || !grecaptcha.execute) {
                    console.error("reCAPTCHA v3 execute method not found.");
                    return;
                }

                grecaptcha.execute(siteKey, { action: action }).then((token) => {
                    helpers.ensureResponseInput(form, RESPONSE_FIELD).value = token;
                    ensureActionInput(form, action);
                    helpers.resubmitForm(form, GUARD_NAME);
                }, function (err) {
                    console.error("reCAPTCHA v3 execution failed", err);
                });
            };

            if (isRecaptchaApiReady()) {
                exec();
            } else {
                H.waitFor(isRecaptchaApiReady, exec, 8000);
            }

            return false; // Block submit
        }, '__captchaV3Bound');
    }

    // --- V2 Logic (Checkbox or Invisible) ---
    function bindV2(ctx) {
        const container = ctx.container;
        const cfg = ctx.config || {};
        const siteKey = cfg.siteKey;
        const size = (cfg.size || 'normal');
        const theme = (cfg.theme || 'light');
        let elementId = ctx.elementId;

        if (!elementId) {
            const el = container.querySelector('.g-recaptcha[id]');
            if (el) elementId = el.id;
        }

        if (!siteKey || !elementId) return;

        const form = container.closest('form');
        const isInvisible = (size === 'invisible');

        function render() {
            if (!window.grecaptcha || !window.grecaptcha.render) return;

            try {
                const badge = isInvisible ? (cfg.badge || 'bottomright') : undefined;

                const widgetId = grecaptcha.render(elementId, {
                    sitekey: siteKey,
                    size: isInvisible ? 'invisible' : size,
                    theme: theme,
                    badge: badge,
                    callback: (token) => {
                        if (!form) return;
                        H.ensureResponseInput(form, RESPONSE_FIELD).value = token;

                        if (isInvisible) {
                            H.resubmitForm(form, GUARD_NAME);
                        }
                    },
                    'expired-callback': () => {
                        if (form) H.ensureResponseInput(form, RESPONSE_FIELD).value = '';
                    }
                });

                if (isInvisible && form) {
                    H.bindSubmit(form, GUARD_NAME, function (e, helpers) {
                        disableSubmitTemporarily(form);
                        grecaptcha.execute(widgetId);
                        return false; // Block submit
                    }, '__captchaV2InvisibleBound');
                }
            } catch (e) {
                console.error("Failed to render reCAPTCHA v2", e);
            }
        }

        if (isRecaptchaApiReady()) {
            render();
        } else {
            H.waitFor(isRecaptchaApiReady, render, 8000);
        }
    }

    // Register adapter
    H.register(PROVIDER, function (ctx) {
        if (!ctx || !ctx.container) return;
        // Distinguish logic based on mode passed from server
        if (ctx.mode === 'score') return bindV3(ctx);
        return bindV2(ctx);
    });
})();