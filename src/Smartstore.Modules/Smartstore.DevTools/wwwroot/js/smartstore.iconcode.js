// Keep the developer examples local to DevTools, using the browser's selected address and options.
(function ($) {
    'use strict';
    $(function () {
        $('.devtools-icon-code').each(function () {
            const root = $(this);
            const browser = root.find('.icon-browser');
            const output = root.find('#icon-code-output');
            const copy = root.find('.btn-clipboard');
            let format = 'tag-helper';
            const escape = value => value.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

            function update() {
                const address = browser.find('.icon-browser-select').val() || '';
                const options = {};
                browser.find('[data-icon-option]').each(function () {
                    const key = this.dataset.iconOption;
                    if (!this.value || (key === 'stroke-scale' && browser.find('[data-option="stroke-scale"]').hasClass('d-none'))) return;
                    options[key] = key.startsWith('flip-') ? this.value.toLowerCase() : this.value.replace(',', '.');
                });
                let code = '';
                if (address) {
                    if (format === 'expression') {
                        const separator = address.indexOf('?');
                        const name = separator < 0 ? address : address.slice(0, separator);
                        const query = new URLSearchParams(separator < 0 ? '' : address.slice(separator + 1));
                        const flip = query.get('flip') || '';
                        if ('flip-h' in options || 'flip-v' in options) {
                            const horizontal = 'flip-h' in options ? options['flip-h'] === 'true' : flip.includes('x');
                            const vertical = 'flip-v' in options ? options['flip-v'] === 'true' : flip.includes('y');
                            query.set('flip', (horizontal ? 'x' : '') + (vertical ? 'y' : '') || 'none');
                        }
                        for (const [key, value] of Object.entries(options)) {
                            if (!key.startsWith('flip-')) query.set(key, value);
                        }
                        const modifiers = [...query].map(([key, value]) => encodeURIComponent(key) + '=' + encodeURIComponent(value)).join('&');
                        code = name + (modifiers ? '?' + modifiers : '');
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
            // Selection handlers reset old options first; read the completed state afterwards.
            browser.on('change', '.icon-browser-select, [data-icon-option]', () => queueMicrotask(update));
            update();
        });
    });
})(jQuery);
