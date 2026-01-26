export class PasswordValidator {
    constructor(config, ressources) {
        const $el = $('.validate-password');
        this.$el = $el;
        this.config = config ?? {};
        this.res = ressources ?? {};

        if (!$el.length) {
            console.warn("PasswordValidator: field not found for selector ", fieldSelector);
            return;
        }

        $el.on("input.smartstore.passwordvalidator blur.smartstore.passwordvalidator", () => this.validate());
    }

    validate() {
        const value = String(this.$el.val() ?? '');
        console.log('TODO: validate password ' + value);

        if (this.config.requireDigit) {
            console.log('requireDigit');
        }
    }
}