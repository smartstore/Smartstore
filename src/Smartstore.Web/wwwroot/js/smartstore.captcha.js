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

    // --- Shared Helpers for Adapters ------------------------------------------
    window.CaptchaHelpers = window.CaptchaHelpers || {
        /**
         * Generic polling helper for lazy-loaded global APIs
         * @param {Function} predicate - Condition to check
         * @param {Function} onReady - Callback when ready
         * @param {number} timeoutMs - Timeout in milliseconds (default 8000)
         */
        waitFor: function (predicate, onReady, timeoutMs) {
            const start = Date.now();
            (function poll() {
                try {
                    if (predicate()) { onReady(); return; }
                } catch (_) { /* ignore */ }
                if (Date.now() - start > (timeoutMs || 8000)) return;
                setTimeout(poll, 50);
            })();
        },

        /**
         * Ensure a hidden input exists for CAPTCHA response
         * @param {HTMLFormElement} form - The form element
         * @param {string} fieldName - Name attribute for the input
         * @returns {HTMLInputElement|null}
         */
        ensureResponseInput: function (form, fieldName) {
            if (!form || !fieldName) return null;
            let input = form.querySelector('input[name="' + fieldName + '"], textarea[name="' + fieldName + '"]');
            if (!input) {
                input = document.createElement('input');
                input.type = 'hidden';
                input.name = fieldName;
                form.appendChild(input);
            }
            return input;
        },

        /**
         * Dispatch a CustomEvent on a container
         * @param {HTMLElement} container - Event target
         * @param {string} type - Event type
         * @param {object} detail - Event detail object
         */
        dispatch: function (container, type, detail) {
            if (!container || typeof window.CustomEvent !== 'function') return;
            container.dispatchEvent(new CustomEvent(type, { bubbles: true, detail: detail || {} }));
        },

        /**
         * Check if form uses jQuery Unobtrusive AJAX
         * @param {HTMLFormElement} form
         * @returns {boolean}
         */
        isUnobtrusiveAjax: function (form) {
            return !!(form && (form.getAttribute('data-ajax') || (window.jQuery && window.jQuery(form).data('ajax'))));
        },

        /**
         * Capture the submit button that triggered the form submit
         * @param {HTMLFormElement} form
         * @param {Event} event - The submit event
         * @private
         */
        _captureSubmitButton: function (form, event) {
            if (!form) return;

            // Try to get the button from the event (if it's a real button click)
            var button = null;

            // 1. Check if event.submitter is available (modern browsers)
            if (event && event.submitter) {
                button = event.submitter;
            }
            // 2. Fall back to document.activeElement
            else if (document.activeElement && document.activeElement.form === form) {
                var active = document.activeElement;
                if (active.tagName === 'BUTTON' || (active.tagName === 'INPUT' && (active.type === 'submit' || active.type === 'image'))) {
                    button = active;
                }
            }

            // Store button reference on the form
            form.__captchaSubmitButton = button;
        },

        /**
         * Resubmit form honoring jQuery Unobtrusive AJAX and preserving submit button context
         * Uses modern requestSubmit() when available, falls back to trigger('submit') with hidden input
         * @param {HTMLFormElement} form
         * @param {string} reentryGuardName - Guard property name (e.g., '__captchaResubmit')
         */
        resubmitForm: function (form, reentryGuardName) {
            if (!form) return;
            const guard = reentryGuardName || '__captchaResubmit';
            const button = form.__captchaSubmitButton;

            form[guard] = true;
            try {
                // Modern: Use requestSubmit (clean, native, includes button automatically)
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit(button || null);
                }
                // Fallback: jQuery Unobtrusive + temporary hidden input for legacy browsers
                else if (this.isUnobtrusiveAjax(form) && window.jQuery) {
                    // Create temporary input for button name/value
                    var tempInput = null;
                    if (button && button.name && !button.disabled) {
                        tempInput = document.createElement('input');
                        tempInput.type = 'hidden';
                        tempInput.name = button.name;
                        tempInput.value = button.value || '';
                        form.appendChild(tempInput);
                    }

                    window.jQuery(form).trigger('submit');

                    // Cleanup after serialization (100ms delay to let unobtrusive serialize first)
                    if (tempInput) {
                        setTimeout(function () {
                            try { tempInput.remove(); } catch (_) { form.removeChild(tempInput); }
                        }, 100);
                    }
                }
                // Last resort: native submit (no button context, but works)
                else {
                    form.submit();
                }
            } finally {
                form[guard] = false;
            }
        },

        /**
         * Bind a submit handler that prevents double-submit with Unobtrusive AJAX
         * @param {HTMLFormElement} form
         * @param {string} guardName - Re-entrancy guard property name
         * @param {Function} onSubmit - Callback: (event, helpers) => { return true to allow, false to block }
         * @param {string} [boundFlagName] - Flag to prevent double-binding (optional)
         */
        bindSubmit: function (form, guardName, onSubmit, boundFlagName) {
            if (!form || !guardName || !onSubmit) return;

            const boundFlag = boundFlagName || ('__captcha_' + guardName + '_bound');
            if (form[boundFlag]) return;
            form[boundFlag] = true;

            const self = this;
            form.addEventListener('submit', function (e) {
                // Re-entrancy guard: allow pass-through
                if (form[guardName] === true) return;

                // Capture submit button BEFORE any validation/blocking
                self._captureSubmitButton(form, e);

                // Native validation first
                if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;

                // Call adapter logic; if it returns false, stop propagation
                const shouldBlock = onSubmit(e, self);
                if (shouldBlock === false) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                }
            }, true); // Capture phase
        },

        /**
         * Create metadata object for events
         * @param {string} provider - Provider name
         * @param {object} config - Config object
         * @param {HTMLFormElement} form - Form element
         * @returns {object}
         */
        createMeta: function (provider, config, form) {
            return {
                provider: provider || 'Unknown',
                formId: (form && form.id) || null,
                config: config || {}
            };
        },

        /**
         * Register an adapter with the CaptchaRegistry (waits if not ready yet)
         * @param {string} providerName - Provider name (e.g., 'Captcha.FriendlyCaptcha')
         * @param {Function} initFunction - Adapter init function
         * @param {string} [errorEventName] - Optional error event name for error dispatching
         */
        register: function (providerName, initFunction, errorEventName) {
            var self = this;
            (function tryRegister() {
                if (!window.CaptchaRegistry || typeof window.CaptchaRegistry.register !== 'function') {
                    self.waitFor(function () {
                        return window.CaptchaRegistry && typeof window.CaptchaRegistry.register === 'function';
                    }, tryRegister, 8000);
                    return;
                }

                window.CaptchaRegistry.register(providerName, {
                    init: function (ctx) {
                        try {
                            initFunction(ctx);
                        } catch (error) {
                            if (ctx && ctx.container && errorEventName) {
                                self.dispatch(ctx.container, errorEventName, { error: error });
                            }
                        }
                    }
                });
            })();
        }
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