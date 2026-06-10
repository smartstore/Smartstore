/*
*  Project: Smartstore gross/net tax calculation.
*  Author: Marcus Gesing, SmartStore AG
*
* Variant          → actual price calculation, always takes precedence.
* Quantity         → actual price calculation, so that tiered prices/pricing rules apply.
* Gross unit price → remains unchanged; net and total are calculated (unit price * quantity).
* Net unit price   → remains unchanged; gross and total are calculated (unit price * quantity).
* Gross total      → remains unchanged, net total and unit prices are calculated (total / quantity).
* Net total        → remains unchanged, gross total and unit prices are calculated (total / quantity).
* Tax rate         → depending on 'pricesIncludeTax', total and unit prices are calculated.
*/

Smartstore.Admin.taxConverter = (() => {
    let pricesIncludeTax = true;    // A value indicating whether prices include tax or not.
    let taxRate = 0;                // Current tax rate (e.g. 19). Default is 0, which means that tax rate will be read from the ".taxcalc-rate" field.
    let quantity = 1;               // Current quantity.
    let calculationLocked = false;  // A value indicating whether tax calculation is locked.
    let $root;

    return {
        initialize: (selector, options) => {
            $root = $(selector);
            const getTaxRate = options.taxRate === undefined;

            pricesIncludeTax = options.pricesIncludeTax;
            taxRate = (parseFloat(getTaxRate ? $root.find('.taxcalc-rate').val() : options.taxRate) || 0) / 100;

            $root
                .on('click', '.btn-taxcalc-lock', (e) => {
                    // Toggler icon clicked to enable/disable the tax calculation.
                    const T = window.taxConverterRes;
                    const $btn = $(e.currentTarget);
                    const $converter = $btn.closest('.tax-converter');
                    const active = !$converter.hasClass('taxcalc-active');

                    $btn.find('i')
                        .toggleClass('fa-lock', active)
                        .toggleClass('fa-lock-open text-muted', !active);

                    $btn.attr('title', T[active ? 'Admin.Common.TaxConversion.Disable' : 'Admin.Common.TaxConversion.Enable']);
                    $converter.toggleClass('taxcalc-active', active);

                    if (active) {
                        calculate($converter.find(pricesIncludeTax ? '.taxcalc-gross' : '.taxcalc-net'));
                    }
                })
                .on('change', '.taxcalc-quantity', (e) => {
                    // Quantity updated.
                    quantity = parseFloat($(e.currentTarget).val()) || 1;
                })
                .on('change', '.taxcalc-rate', (e) => {
                    // Tax rate updated.
                    if (getTaxRate) {
                        taxRate = (parseFloat($(e.currentTarget).val()) || 0) / 100;
                    }

                    $root.find(pricesIncludeTax ? '.taxcalc-gross' : '.taxcalc-net').each((_, el) => {
                        calculate($(el));
                    });
                })
                .on('change', '.taxcalc-gross, .taxcalc-net', (e) => {
                    // Gross or net updated.
                    calculate($(e.currentTarget));
                });
        }
    };

    function calculate(el) {
        if (calculationLocked) {
            return;
        }

        const $converter = el.closest('.tax-converter');
        if (!$converter.hasClass('taxcalc-active')) {
            return;
        }

        calculationLocked = true;
        try {
            let grossToNet = pricesIncludeTax;
            if (el.hasClass('taxcalc-gross')) {
                // Gross updated.
                grossToNet = true;
            }
            else if (el.hasClass('taxcalc-net')) {
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
            //console.log(`qty:${quantity} taxRate:${taxRate} grossToNet:${grossToNet} amount:${amount}`);

            // INFO: Do not trigger('change')! Causes a stack overflow.
            // See the event handlers that smartstore.numberinput.js listens for.
            $converter.find(grossToNet ? '.taxcalc-net' : '.taxcalc-gross')
                .val(amount)
                .trigger('change.ni');

            // Update total.
            const $total = $root.find('.taxcalc-total');
            const $unitPrice = $root.find('.taxcalc-unitprice');

            if ($total.length && $unitPrice.length) {
                $total.find('.taxcalc-gross').val($unitPrice.find('.taxcalc-gross').val() * quantity).trigger('change.ni');
                $total.find('.taxcalc-net').val($unitPrice.find('.taxcalc-net').val() * quantity).trigger('change.ni');
            }
        }
        finally {
            calculationLocked = false;
        }
    }    
})();
