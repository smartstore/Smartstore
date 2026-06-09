Smartstore.Admin.taxConverter = (() => {
    let pricesIncludeTax = true;    // A value indicating whether prices include tax or not.
    let taxRate = 0;                // Current tax rate (e.g. 19). Default is 0, which means that tax rate will be read from the ".conversion-taxrate" field.
    let $elTaxRate = null;          // jQuery element representing the tax rate input field.

    return {
        initialize: (options) => {
            pricesIncludeTax = options.pricesIncludeTax;

            const $form = $('.tax-converter:first').closest('form');

            if (options.taxRate !== undefined) {
                taxRate = (parseFloat(options.taxRate) || 0) / 100;
            }
            if (taxRate === 0) {
                $elTaxRate = $form.find('.conversion-taxrate:first');
            }

            $form
                .on('click', '.btn-conversion-toggler', (e) => {
                    // Toggler icon clicked to enable/disable the tax conversion.
                    const T = window.taxConverterRes;
                    const $btn = $(e.currentTarget);
                    const $converter = $btn.closest('.tax-converter');
                    const active = !$converter.hasClass('conversion-active');

                    $btn.find('i')
                        .toggleClass('fa-lock', active)
                        .toggleClass('fa-unlock-keyhole text-muted', !active);

                    $btn.attr('title', T[active ? 'Admin.Common.TaxConversion.Disable' : 'Admin.Common.TaxConversion.Enable']);
                    $converter.toggleClass('conversion-active', active);

                    if (active) {
                        convert($converter.find(pricesIncludeTax ? '.conversion-gross' : '.conversion-net'));                        
                    }
                })
                .on('change', '.conversion-gross, .conversion-net', (e) => {
                    // Gross or net updated.
                    convert($(e.currentTarget));
                })
                .on('change', '.conversion-taxrate', (e) => {
                    // Tax rate updated.
                    $form.find(pricesIncludeTax ? '.conversion-gross' : '.conversion-net').each((_, el) => {
                        convert($(el));
                    });
                });
        }
    };

    function convert(el) {
        const $converter = el.closest('.tax-converter');
        if (!$converter.hasClass('conversion-active')) {
            return;
        }

        if ($elTaxRate.length) {
            // Get current tax rate.
            taxRate = (parseFloat($elTaxRate.val()) || 0) / 100;
        }

        let grossToNet = pricesIncludeTax;
        if (el.hasClass('conversion-gross')) {
            // Gross updated.
            grossToNet = true;
        }
        else if (el.hasClass('conversion-net')) {
            // Net updated.
            grossToNet = false;
        }

        let amount = parseFloat(el.val()) || 0;
        if (grossToNet) {
            amount = amount / (1 + taxRate);
        }
        else {
            amount = amount * (1 + taxRate);
        }

        // INFO: Do not trigger('change')! Causes a stack overflow.
        // See the event handlers that smartstore.numberinput.js listens for.
        $converter.find(grossToNet ? '.conversion-net' : '.conversion-gross')
            .val(amount)
            .trigger('change.ni');
    }

})();
