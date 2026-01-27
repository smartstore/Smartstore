export class PasswordValidator {
    constructor(passwordSelector, ressources) {
        const $el = $(passwordSelector);
        if (!$el.length) {
            console.warn("PasswordValidator: input field not found for selector ", passwordSelector);
            return;
        }

        const requirements = this._getRequirements($el, ressources);
        if (requirements.length === 0) {
            return;
        }

        const $widget = $('<div class="password-requirements mt-2 hide" aria-live="polite"></div>');
        const $toggleGroup = $el.parent('.toggle-pwd-group');
        if ($toggleGroup.length) {
            $toggleGroup.after($widget);
        } else {
            $el.after($widget);
        }

        this._createWidget($widget, requirements);

        $el.on('input.smartstore.passwordvalidator', () => {
            const value = String($el.val() ?? '');

            const checkedRequirements = requirements.map(x => ({
                key: x.key,
                msg: x.msg,
                ok: x.test(value)
            }));

            this._updateWidget($widget, checkedRequirements);
            $widget.removeClass('hide');
            //console.log(tests.filter(x => !x.ok).map(x => x.msg).join(', '));
        }).on('blur.smartstore.passwordvalidator', () => {
            $widget.addClass('hide');
        });
    }

    _createWidget($widget, requirements) {
        const $ul = $('<ul class="list-unstyled small mb-0"></ul>');

        for (const r of requirements) {
            const $li = $(`
                <li class="text-muted" data-requirement="${r.key}">
                    <i class="fa fa-fw mr-1 requirement-icon" aria-hidden="true"></i>
                    <span>${r.msg}</span>
                </li>`);

            $ul.append($li);
        }

        $widget.html($ul);
    }

    _updateWidget($widget, checkedRequirements) {
        for (const r of checkedRequirements) {
            const $li = $widget.find(`[data-requirement="${r.key}"]`);
            if ($li.length) {
                $li.toggleClass('text-success', r.ok);
                $li.toggleClass('text-muted', !r.ok);

                const $icon = $li.find('.requirement-icon');
                $icon.toggleClass('fa-check', r.ok);
                //$icon.toggleClass('fa-minus', !r.ok);
            }
        }

        // TODO: setCustomValidity....
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