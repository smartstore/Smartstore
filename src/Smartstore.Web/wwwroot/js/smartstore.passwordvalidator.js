export class PasswordValidator {
    // TODO: (mg) Make this unobtrusive: declare validation rules via data-attributes on the input field,
    // e.g. data-require-digit="true", data-min-length="8", etc.
    // Pass the input field (or a selector) as parameter to the constructor.
    // Create the requirements only once on init.
    // On input/blur events, run all requirements' validate methods and update validation messages accordingly.
    constructor(config, ressources) {
        this.$el = $('.validate-password');
        this.config = config ?? {};
        this.res = ressources ?? {};

        if (!this.$el.length) {
            console.warn("PasswordValidator: field not found for selector ", fieldSelector);
            return;
        }

        this.$el.on('input.smartstore.passwordvalidator', () => {
            const requirements = this.validate();
            //requirements.forEach(x => { if (!x.ok) console.log(x.msg); });

        }).on('blur.smartstore.passwordvalidator', () => {
            // Hide requirements on blur.
        });
    }

    validate() {
        const requirements = [];
        const value = String(this.$el.val() ?? '');

        if (this.config.minLength > 0) {
            requirements.push({
                ok: value.length >= this.config.minLength,
                msg: this.res.TooShort || `At least ${this.config.minLength} characters`
            });
        }

        if (this.config.requireLower) {
            requirements.push({
                ok: this._testRegex(value, /\p{Ll}/u, /[a-z]/),
                msg: this.res.RequireLower || 'At least one lowercase letter (a–z)'
            });
        }

        if (this.config.requireUpper) {
            requirements.push({
                ok: this._testRegex(value, /\p{Lu}/u, /[A-Z]/),
                msg: this.res.RequireUpper || 'At least one uppercase letter (A–Z)'
            });
        }

        if (this.config.requireDigit) {
            requirements.push({
                ok: this._testRegex(value, /\p{Nd}/u, /\d/),
                msg: this.res.RequireDigit || 'At least one number (0–9)'
            });
        }

        if (this.config.requireNonAlpha) {
            // .NET RequireNonAlphanumeric: at least one char where !char.IsLetterOrDigit(c)
            // char.IsLetterOrDigit => Letter (L*) OR DecimalDigitNumber (Nd)
            // So "non-alphanumeric" => NOT (L or Nd)
            requirements.push({
                ok: this._testRegex(value, /[^\p{L}\p{Nd}]/u, /[^A-Za-z0-9]/),
                msg: this.res.RequireNonAlpha || 'At least one special character (e.g. !@#$)'
            });
        }

        if (this.config.requireUniqueChars > 0) {
            requirements.push({
                ok: this._countUniqueChars(value) >= this.config.requireUniqueChars,
                msg: this.res.RequireUniqueChars || `At least ${this.config.requireUniqueChars} unique characters`
            });
        }

        return requirements;
    }

    _testRegex(value, unicodeRegex, asciiFallbackRegex) {
        try {
            return unicodeRegex.test(value);
        } catch {
            return asciiFallbackRegex.test(value);
        }
    }

    _countUniqueChars(value) {
        // Identity counts distinct .NET char (UTF-16 code units), not codepoints/graphemes.
        const set = new Set();
        for (let i = 0; i < value.length; i++) {
            set.add(value[i]);
        }
        return set.size;
    }
}