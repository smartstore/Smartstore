export class PasswordValidator {
    // TODO: (mg) Reveal/hide .password-requirements animated (fast slide in/out). But not with jQuery slide*, it's too laggy. Use BS Collapse API.
    // TODO: (mg) Don't hide .password-requirements if it is clicked, or if the "Confirm" field has focus (not sure about the latter though)
    // TODO: (mg) Don't display the regular .field-validation-error. It is redundant, the policy box says everything we need. Besides: it has annoying jump effects.
    // TODO: (mg) Terminology: Policy, Rules etc.. Not "Requirements".
    constructor(passwordSelector, ressources) {
        const $el = $(passwordSelector);
        if (!$el.length) {
            console.warn("PasswordValidator: password field not found for selector ", passwordSelector);
            return;
        }

        const requirements = this._getRequirements($el, ressources);
        if (requirements.length === 0) {
            return;
        }

        //const $fieldError = $el.closest('.password-container').find('.field-validation-valid, .field-validation-error').first();
        const $widget = this._createWidget($el, requirements);

        // TODO: (mg) Should we only display validation error after clicking the Submit button (instead of both error and requirements)? Too hackish?

        // TODO: (mg) $ validators are global, but this is an instantiable class. Register only once.
        // jQuery unobtrusive validation.
        $.validator.addMethod('pwpolicy', function (value, element) {
            if (this.optional(element)) {
                return true;
            }

            const checkedRequirements = requirements.map(x => ({
                key: x.key,
                msg: x.msg,
                ok: x.test(value)
            }));
            //console.log(checkedRequirements.filter(x => !x.ok).map(x => x.msg).join(', '));

            // Update widget.
            for (const r of checkedRequirements) {
                const $li = $widget.find(`[data-requirement="${r.key}"]`);
                if ($li.length) {
                    $li.toggleClass('text-success', r.ok);

                    const $icon = $li.find('.requirement-icon');
                    $icon.toggleClass('fa-check', r.ok).toggleClass('fa-ban', !r.ok);
                    //$icon.toggleClass('fa-minus', !r.ok);
                }
            }

            return checkedRequirements.every(x => x.ok);
        });

        $.validator.unobtrusive.adapters.add('pwpolicy',
            ['minlength', 'lower', 'upper', 'digit', 'nonalpha', 'uniquechars'],
            (options) => {
                options.rules['pwpolicy'] = { };
                if (options.message) {
                    options.messages['pwpolicy'] = options.message;
                }
            }
        );


        $el.on('input.smartstore.passwordvalidator', () => {
            $el.valid();
            $widget.removeClass('d-none');
            //$widget.slideDown('fast');
        }).on('blur.smartstore.passwordvalidator', (e) => {
            $widget.addClass('d-none');
            //$widget.slideUp('fast');
        }).on('focus.smartstore.passwordvalidator', () => {
            $widget.removeClass('d-none');
            //$widget.slideDown('fast');
        });
    }

    _createWidget($el, requirements) {
        const $widget = $('<div class="pwd-policy small d-none" aria-live="polite"></div>');
        const $elCtx = $el.closest('.pwd-container');
        if ($elCtx.length) {
            $elCtx.append($widget);
        }
        else {
            $el.after($widget);
        }

        // TODO: (mg) Localize (keep it short in DE)
        $widget.append($('<div class="fwm mb-2">Password must meet these rules:</div>'));

        const $ul = $('<ul class="pwd-rules list-unstyled mb-0"></ul>');

        for (const r of requirements) {
            const $li = $(`
                <li class="pwd-rule" data-requirement="${r.key}">
                    <i class="fa fa-fw fa-ban mr-1 requirement-icon" aria-hidden="true"></i>
                    <span>${r.msg}</span>
                </li>`);

            $ul.append($li);
        }

        $widget.append($ul);
        return $widget;
    }

    _getRequirements($el, res) {
        const requirements = [];
        const minLength = parseInt($el.data('min-length')) || 0;
        const requireLower = toBool($el.data('require-lower'));
        const requireUpper = toBool($el.data('require-upper'));
        const requireDigit = toBool($el.data('require-digit'));
        const requireNonAlpha = toBool($el.data('require-nonalpha'));
        const uniqueChars = parseInt($el.data('uniquechars')) || 0;

        if (minLength > 0) {
            requirements.push({
                key: 'minlength',
                test: (v) => v.length >= minLength,
                msg: res.MinLength || `At least ${minLength} characters`
            });
        }

        if (requireLower) {
            requirements.push({
                key: 'lower',
                test: this._getRegexTest(/\p{Ll}/u, /[a-z]/),
                msg: res.RequireLower || 'At least one lowercase letter (a–z)'
            });
        }

        if (requireUpper) {
            requirements.push({
                key: 'upper',
                test: this._getRegexTest(/\p{Lu}/u, /[A-Z]/),
                msg: res.RequireUpper || 'At least one uppercase letter (A–Z)'
            });
        }

        if (requireDigit) {
            requirements.push({
                key: 'digit',
                test: this._getRegexTest(/\p{Nd}/u, /\d/),
                msg: res.RequireDigit || 'At least one number (0–9)'
            });
        }

        if (requireNonAlpha) {
            // .NET RequireNonAlphanumeric: at least one char where !char.IsLetterOrDigit(c)
            // char.IsLetterOrDigit => Letter (L*) OR DecimalDigitNumber (Nd)
            // So "non-alphanumeric" => NOT (L or Nd)
            requirements.push({
                key: 'nonalpha',
                test: this._getRegexTest(/[^\p{L}\p{Nd}]/u, /[^A-Za-z0-9]/),
                msg: res.RequireNonAlpha || 'At least one special character (e.g. !@#$)'
            });
        }

        if (uniqueChars > 0) {
            requirements.push({
                key: 'uniquechars',
                test: (v) => this._countUniqueChars(v) >= uniqueChars,
                msg: res.UniqueChars || `At least ${uniqueChars} unique characters`
            });
        }

        return requirements;
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