export class PasswordValidator {
    // TODO: (mg) Showing validation error state on input WHILE TYPING is annoying and distracting.
    constructor(passwordSelector, ressources) {
        const $el = $(passwordSelector);
        if (!$el.length) {
            console.warn("PasswordValidator: password field not found for selector ", passwordSelector);
            return;
        }

        const rules = this._getRules($el, ressources);
        if (rules.length === 0) {
            return;
        }

        const $widget = this._createWidget($el, rules, ressources);

        // Attach per-field state for the global validator method.
        $el.attr('data-val-pwdpolicy', '')
           .data('smPwdPolicy', {
                rules: rules,
                $widget: $widget
            });

        this._addValidatorPolicy();

        $el.on('input.smartstore.passwordvalidator', () => {
            $el.valid();
            $widget.collapse('show');
        }).on('focus.smartstore.passwordvalidator', () => {
            $widget.collapse('show');
        });

        $widget.closest('.pwd-container').on('clickoutside.smartstore.passwordvalidator', (e) => {
            setTimeout(() => {
                if (!$el.is(':focus')) $widget.collapse('hide');
            }, 100);
        });
    }

    _addValidatorPolicy() {
        if (!$.validator || $.validator._smPwdPolicyAdded) {
            return;
        }

        $.validator._smPwdPolicyAdded = true;

        $.validator.addMethod('pwdpolicy', function (value, element) {
            if (this.optional(element)) {
                return true;
            }

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

            return checkedRules.every(x => x.ok);
        });

        $.validator.unobtrusive.adapters.add('pwdpolicy',
            ['minlength', 'lower', 'upper', 'digit', 'nonalpha', 'uniquechars'],
            (options) => {
                options.rules['pwdpolicy'] = {};//true?
                if (options.message) {
                    options.messages['pwdpolicy'] = options.message;
                }
            }
        );
    }

    _createWidget($el, rules, res) {
        const $widget = $('<div class="pwd-policy-wrap collapse" aria-live="polite"></div>');
        const $inner = $('<div class="pwd-policy small"></div>');

        const $content = $('<div class="p-2"></div>');
        $content.append($(`<div class="fwm mb-2">${res.MeetPasswordRules}</div>`));

        const $ul = $('<ul class="pwd-rules fa-ul mb-0"></ul>');
        for (const r of rules) {
            const $li = $(`
                <li class="pwd-rule" data-rule="${r.key}">
                    <span class="fa-li"><i class="fa fa-ban rule-icon" aria-hidden="true"></i></span>
                    ${r.msg}
                </li>`);

            $ul.append($li);
        }

        $content.append($ul);
        $inner.append($content);
        $widget.append($inner);

        const $elCtx = $el.closest('.pwd-container');
        if ($elCtx.length) {
            $elCtx.append($widget);
        }
        else {
            $el.after($widget);
        }

        return $widget;
    }

    _getRules($el, res) {
        const rules = [];
        const minLength = parseInt($el.data('min-length')) || 0;
        const requireLower = toBool($el.data('require-lower'));
        const requireUpper = toBool($el.data('require-upper'));
        const requireDigit = toBool($el.data('require-digit'));
        const requireNonAlpha = toBool($el.data('require-nonalpha'));
        const uniqueChars = parseInt($el.data('uniquechars')) || 0;

        if (minLength > 0) {
            rules.push({
                key: 'minlength',
                test: (v) => v.length >= minLength,
                msg: res.MinLength || `At least ${minLength} characters`
            });
        }

        if (requireLower) {
            rules.push({
                key: 'lower',
                test: this._getRegexTest(/\p{Ll}/u, /[a-z]/),
                msg: res.RequireLower || 'At least one lowercase letter (a–z)'
            });
        }

        if (requireUpper) {
            rules.push({
                key: 'upper',
                test: this._getRegexTest(/\p{Lu}/u, /[A-Z]/),
                msg: res.RequireUpper || 'At least one uppercase letter (A–Z)'
            });
        }

        if (requireDigit) {
            rules.push({
                key: 'digit',
                test: this._getRegexTest(/\p{Nd}/u, /\d/),
                msg: res.RequireDigit || 'At least one number (0–9)'
            });
        }

        if (requireNonAlpha) {
            // .NET RequireNonAlphanumeric: at least one char where !char.IsLetterOrDigit(c)
            // char.IsLetterOrDigit => Letter (L*) OR DecimalDigitNumber (Nd)
            // So "non-alphanumeric" => NOT (L or Nd)
            rules.push({
                key: 'nonalpha',
                test: this._getRegexTest(/[^\p{L}\p{Nd}]/u, /[^A-Za-z0-9]/),
                msg: res.RequireNonAlpha || 'At least one special character (e.g. !@#$)'
            });
        }

        if (uniqueChars > 0) {
            rules.push({
                key: 'uniquechars',
                test: (v) => this._countUniqueChars(v) >= uniqueChars,
                msg: res.UniqueChars || `At least ${uniqueChars} unique characters`
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