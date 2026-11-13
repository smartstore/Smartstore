// Keep the developer examples local to DevTools, using the browser's selected address and options.
(function ($) {
    'use strict';
    $(function () {
        $('.devtools-icon-code').each(function () {
            const root = $(this);
            const browser = root.find('.icon-browser');
            browser.iconBrowser();
            const picker = browser.data('iconBrowser');
            const output = root.find('#icon-code-output');
            const copy = root.find('.btn-clipboard');
            let format = 'tag-helper';
            const escape = value => value.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

            function update() {
                const { name: address, options, expression } = picker.getState();
                let code = '';
                if (address) {
                    if (format === 'expression') {
                        code = expression;
                    } else {
                        const attributes = Object.entries({ name: address, ...options })
                            .map(([key, value]) => `${key}="${escape(value)}"`).join(' ');
                        code = format === 'component' ? `<sm-icon ${attributes}></sm-icon>` : `<icon ${attributes} />`;
                    }
                }
                output.val(code);
                copy.prop('disabled', !code);
            }
            root.on('click', '[data-code-format]', function () {
                format = this.dataset.codeFormat;
                root.find('[data-code-format]').removeClass('active');
                $(this).addClass('active');
                root.find('[data-code-format-label]').text($(this).text());
                update();
            });
            // The browser publishes changes only after its in-memory state is complete.
            browser.on('change', '.icon-browser-expression', update);
            update();
        });
    });
})(jQuery);
