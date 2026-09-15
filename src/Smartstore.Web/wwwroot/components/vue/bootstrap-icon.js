window.Smartstore = window.Smartstore || {};
Smartstore.Vue = Smartstore.Vue || {};

Smartstore.Vue.BootstrapIcon = {
    install(app, options) {
        const spriteUrl = options.spriteUrl;

        app.component("bootstrap-icon", {
            template: `
                <svg class="bi" fill="currentColor" width="1em" height="1em" role="img" focusable="false">
                    <use :xlink:href="href"></use>
                </svg>
            `,

            props: {
                name: { type: String, required: true }
            },

            computed: {
                href() {
                    return spriteUrl + '#' + this.name;
                }
            }
        });
    }
};
