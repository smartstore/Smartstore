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

Smartstore.Admin.taxCalculator = (() => {
    let pricesIncludeTax = true;    // A value indicating whether prices include tax or not.
    let taxRate = 0;                // Current tax rate (e.g. 19). Default is 0, which means that tax rate will be read from the [data-tax-field="rate"] field.
    let quantity = 1;               // Current quantity.
    let calculationLocked = false;  // A value indicating whether tax calculation is locked.
    let $root;                      // Root element for the tax calculator. Typically, it is an HTML form.

    return {
        initialize: (options) => {
            $root = $(options.rootSelector || 'form:first');
            const getTaxRate = options.taxRate === undefined;

            pricesIncludeTax = options.pricesIncludeTax;
            taxRate = (parseFloat(getTaxRate ? $root.find('[data-tax-field="rate"]').val() : options.taxRate) || 0) / 100;

            $root
                .on('click', '.btn-tax-lock', (e) => {
                    // Toggler icon clicked to enable/disable the tax calculation.
                    const T = window.taxCalculatorRes;
                    const $btn = $(e.currentTarget);
                    const $taxPair = $btn.closest('[data-tax-pair]');
                    const active = !$taxPair.is('[data-tax-active]');

                    $btn.find('i')
                        .toggleClass('fa-lock', active)
                        .toggleClass('fa-lock-open text-muted', !active);

                    $btn.attr('title', T['Admin.Common.TaxCalculator.' + (active ? 'Disable' : 'Enable')]);
                    active ? $taxPair.attr('data-tax-active', '') : $taxPair.removeAttr('data-tax-active');

                    if (active) {
                        calculate($taxPair.find(fieldSel()));
                    }
                })
                .on('change', '[data-tax-field="quantity"]', (e) => {
                    // Quantity changed.
                    quantity = parseFloat($(e.currentTarget).val()) || 1;
                })
                .on('change', '[data-tax-field="rate"]', (e) => {
                    // Tax rate changed.
                    if (getTaxRate) {
                        taxRate = (parseFloat($(e.currentTarget).val()) || 0) / 100;
                    }

                    $root.find(fieldSel()).each((_, el) => {
                        calculate($(el));
                    });
                })
                .on('change', '[data-tax-field="gross"], [data-tax-field="net"]', (e) => {
                    // Gross or net changed.
                    calculate($(e.currentTarget));
                });
        },

        lock: (lock) => {
            calculationLocked = lock;
        }
    };

    function fieldSel() {
        return pricesIncludeTax ? '[data-tax-field="gross"]' : '[data-tax-field="net"]';
    }

    function calculate(el) {
        if (calculationLocked) {
            return;
        }

        const $taxPair = el.closest('[data-tax-pair]');
        if (!$taxPair.length || !$taxPair.is('[data-tax-active]')) {
            return;
        }

        calculationLocked = true;
        try {
            let grossToNet = pricesIncludeTax;
            if (el.is('[data-tax-field="gross"]')) {
                // Gross updated.
                grossToNet = true;
            }
            else if (el.is('[data-tax-field="net"]')) {
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
            $taxPair.find(grossToNet ? '[data-tax-field="net"]' : '[data-tax-field="gross"]')
                .val(amount)
                .trigger('change.ni');

            // Update total price when unit price is changed or vice versa.
            updateTotalOrUnitPrice($taxPair);
        }
        finally {
            calculationLocked = false;
        }
    }

    function updateTotalOrUnitPrice(sourcePair) {
        let updateTotal;
        if (sourcePair.is('[data-tax-pair="unitprice"]')) {
            // Unit price changed -> update total.
            updateTotal = true;
        }
        else if (sourcePair.is('[data-tax-pair="total"]')) {
            // Total changed -> update unit price.
            updateTotal = false;
        }
        else {
            return;
        }

        const $targetPair = $root.find(updateTotal ? '[data-tax-pair="total"]' : '[data-tax-pair="unitprice"]');
        if (!$targetPair.length || !$targetPair.is('[data-tax-active]')) {
            return;
        }

        const gross = sourcePair.find('[data-tax-field="gross"]').val();
        if (!isNaN(gross)) {
            $targetPair.find('[data-tax-field="gross"]')
                .val(updateTotal ? (gross * quantity) : (gross / quantity))
                .trigger('change.ni');
        }

        const net = sourcePair.find('[data-tax-field="net"]').val();
        if (!isNaN(net)) {
            $targetPair.find('[data-tax-field="net"]')
                .val(updateTotal ? (net * quantity) : (net / quantity))
                .trigger('change.ni');
        }
    }
})();
