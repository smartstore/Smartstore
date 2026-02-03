export class PasswordValidator {
    constructor(passwordSelector) {
        const $el = $(passwordSelector);
        if (!$el.length) {
            console.warn("PasswordValidator: password field not found for selector ", passwordSelector);
            return;
        }

        const state = this._getState($el);
        const rules = state.rules;
        if (rules.length === 0) {
            return;
        }

        const $widget = state.$widget;
        const $container = state.$container;
        const $containerOrBody = ($container && $container.length) ? $container : $('body');
        const $form = $el.closest('form');

        // WCAG
        const srHintId = $el.attr('id') + '-pwd-sr';
        const $srHint = $(`<span id="${srHintId}" class="sr-only" aria-live="polite" aria-atomic="true"></span>`).insertBefore($el);
        $el.aria('describedby', (($el.aria('describedby') || '') + ' ' + srHintId).trim());

        // Attach per-field state for the global validator method.
        $el.attr('data-val-pwdpolicy', '')
           .data('smPwdPolicy', {
               rules: rules,
               $widget: $widget,
               $srHint: $srHint
            });

        this._wireValidatorWhenReady($el, $form);

        $el.on('input.smartstore.passwordvalidator', () => {
            //$el.valid();
            const v = $form.data('validator');

            // Force-run the custom rule so widget updates even when validate would skip it.
            if (v && $.validator?.methods?.pwdpolicy) {
                $.validator.methods.pwdpolicy.call(v, $el.val(), $el[0], $el.rules?.()?.pwdpolicy);
            }

            $widget.collapse('show');
        }).on('focus.smartstore.passwordvalidator', () => {
            $widget.collapse('show');
        });

        $containerOrBody.on('clickoutside.smartstore.passwordvalidator', () => {
            setTimeout(() => {
                if (!$el.is(':focus')) $widget.collapse('hide');
            }, 100);
        });

        $form.one('submit.smartstore.pwdstatus', () => {
            // Show the policy widget and status on submit in case of a validation error.
            if ($container && $container.length) {
                $container.removeClass('pwd-status-hidden');
            }
            $widget.collapse('show');
        });
    }

    _getState($el) {
        const id = $el.attr('id');
        const $widget = $(`[data-pwd-policy-for="${id}"]`).first();
        const $dataHost = ($widget && $widget.length)
            ? $widget.find(`[data-pwd-policy-host="${id}"]`).first()
            : $(`[data-pwd-policy-host="${id}"]`).first();

        let $container = $el.closest('.pwd-container');
        if (!$container.length) {
            $container = $widget.parent();
            $container.addClass('pwd-container');
        }
        $container.addClass('pwd-status-hidden');

        const rules = this._getRules($dataHost, $widget);
        return {
            rules,
            $container,
            $widget
        };
    }

    _wireValidatorWhenReady($el, $form) {
        const wire = () => {
            if (!$.validator || !$form.length) {
                return;
            }

            PasswordValidator._addValidatorPolicy();

            const v = $form.data('validator');

            // Ensure unobtrusive has initialized the form validator (span-based messages etc.).
            if (!v && $.validator.unobtrusive) {
                $.validator.unobtrusive.parse($form);
            }

            // Patch errorPlacement ONCE per form (suppress error label for pwd fields).
            if (v && !v.settings._pwdNoMsgPatched) {
                v.settings._pwdNoMsgPatched = true;

                const origErrorPlacement = v.settings.errorPlacement;
                v.settings.errorPlacement = function (error, element) {
                    if ($(element).hasClass('pwd-no-msg')) {
                        error.remove();
                        return;
                    }
                    return origErrorPlacement
                        ? origErrorPlacement.call(this, error, element)
                        : error.insertAfter(element);
                };
            }

            // Attach the rule to THIS field (no adapter needed).
            $el.addClass('pwd-no-msg')
               .rules('add', { pwdpolicy: true });//messages: { pwdpolicy: '' }
        };

        // If validate is already there -> wire immediately.
        if ($.validator) {
            wire();
            return;
        }

        // Otherwise wire after full page load (asset bundles done).
        window.addEventListener('load', wire, { once: true });
    }

    static _addValidatorPolicy() {
        if (!$.validator || $.validator._smPwdPolicyAdded) {
            return;
        }

        $.validator._smPwdPolicyAdded = true;

        $.validator.addMethod('pwdpolicy', function (value, element) {
            const state = $(element).data('smPwdPolicy');
            if (!state || !state.rules) {
                return true;
            }

            const checkedRules = state.rules.map(x => ({
                key: x.key,
                msg: x.msg,
                ok: x.test(value)
            }));
            //console.log(checkedRules.filter(x => !x.ok).map(x => x.msg).join(', '));

            // Update widget.
            const $widget = state.$widget;
            if ($widget && $widget.length) {
                for (const r of checkedRules) {
                    const $li = $widget.find(`[data-rule="${r.key}"]`);
                    if ($li.length) {
                        $li.toggleClass('text-success', r.ok);

                        const $icon = $li.find('.rule-icon');
                        $icon.toggleClass('fa-check', r.ok).toggleClass('fa-ban', !r.ok);
                    }
                }
            }

            // Update SR hint.
            const $srHint = state.$srHint;
            if ($srHint && $srHint.length) {
                const msg = checkedRules.filter(x => !x.ok).map(x => x.msg).join(', ');
                if ($srHint.text() !== msg) {
                    $srHint.text(msg);
                }
            }

            return this.optional(element) ? true : checkedRules.every(x => x.ok);
        });

        $.validator.unobtrusive.adapters.add('pwdpolicy',
            ['minlength', 'lower', 'upper', 'digit', 'nonalpha', 'uniquechars'],
            (options) => {
                options.rules['pwdpolicy'] = {};
                if (options.message) {
                    options.messages['pwdpolicy'] = options.message;
                }
            }
        );
    }

    _getRules($dataHost, $widget) {
        const rules = [];
        if (!$dataHost || !$dataHost.length || !$widget || !$widget.length) {
            return rules;
        }

        const minLength = parseInt($dataHost.data('min-length')) || 0;
        const requireLower = toBool($dataHost.data('require-lower'));
        const requireUpper = toBool($dataHost.data('require-upper'));
        const requireDigit = toBool($dataHost.data('require-digit'));
        const requireNonAlpha = toBool($dataHost.data('require-nonalpha'));
        const uniqueChars = parseInt($dataHost.data('uniquechars')) || 0;

        const getMsg = (ruleKey, fallback) => {
            const $li = $widget.find(`[data-rule="${ruleKey}"]`).first();
            return ($li.data('msg') || $li.text() || fallback || '').trim();
        };

        if (minLength > 0) {
            rules.push({
                key: 'minlength',
                test: (v) => v.length >= minLength,
                msg: getMsg('minlength', `At least ${minLength} characters`)
            });
        }

        if (requireLower) {
            rules.push({
                key: 'lower',
                test: this._getRegexTest(/\p{Ll}/u, /[a-z]/),
                msg: getMsg('lower', 'At least one lowercase letter (a–z)')
            });
        }

        if (requireUpper) {
            rules.push({
                key: 'upper',
                test: this._getRegexTest(/\p{Lu}/u, /[A-Z]/),
                msg: getMsg('upper', 'At least one uppercase letter (A–Z)')
            });
        }

        if (requireDigit) {
            rules.push({
                key: 'digit',
                test: this._getRegexTest(/\p{Nd}/u, /\d/),
                msg: getMsg('digit', 'At least one number (0–9)')
            });
        }

        if (requireNonAlpha) {
            // .NET RequireNonAlphanumeric: at least one char where !char.IsLetterOrDigit(c)
            // char.IsLetterOrDigit => Letter (L*) OR DecimalDigitNumber (Nd)
            // So "non-alphanumeric" => NOT (L or Nd)
            rules.push({
                key: 'nonalpha',
                test: this._getRegexTest(/[^\p{L}\p{Nd}]/u, /[^A-Za-z0-9]/),
                msg: getMsg('nonalpha', 'At least one special character (e.g. !@#$)')
            });
        }

        if (uniqueChars > 0) {
            rules.push({
                key: 'uniquechars',
                test: (v) => this._countUniqueChars(v) >= uniqueChars,
                msg: getMsg('uniquechars', `At least ${uniqueChars} unique characters`)
            });
        }

        return rules;
    }

    _getRegexTest(unicodeRegex, asciiFallbackRegex) {
        try {
            unicodeRegex.test('');
            return (x) => unicodeRegex.test(x);
        } catch {
            return (x) => asciiFallbackRegex.test(x);
        }
    }

    _countUniqueChars(value) {
        // Identity's RequiredUniqueChars counts distinct .NET char values (UTF-16 code units), not Unicode code points.
        // Using split('') preserves surrogate pairs as two units (like .NET char), whereas 'new Set(str)' would iterate by code point.
        return new Set(value.split('')).size;
    }
}