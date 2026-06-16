/*
*  Project: Smartstore gross/net tax calculation.
*  Author: Marcus Gesing, SmartStore AG
*
* Variant          → actual price calculation, always takes precedence.
* Quantity         → actual price calculation, so that tiered prices/pricing rules apply.
* Gross unit price → remains unchanged; net and line total are calculated (unit price * quantity).
* Net unit price   → remains unchanged; gross and line total are calculated (unit price * quantity).
* Gross line total → remains unchanged, net line total and unit prices are calculated (line total / quantity).
* Net line total   → remains unchanged, gross line total and unit prices are calculated (line total / quantity).
* Tax rate         → depending on 'pricesIncludeTax', line total and unit prices are calculated.
*/

Smartstore.Admin.TaxCalculator = class TaxCalculator {
    #pricesIncludeTax;
    #taxRate = 0;
    #quantity = 1;
    #locked = false;
    #$root;
    #res;

    constructor(options) {
        this.#$root = $(options.rootSelector || 'form:first');
        this.#pricesIncludeTax = options.pricesIncludeTax;
        this.#res = options.res || {};

        const getTaxRate = options.taxRate === undefined;
        this.#taxRate = (parseFloat(getTaxRate ? this.#$root.find('[data-tax-field="rate"]').val() : options.taxRate) || 0) / 100;

        this.#bindEvents(getTaxRate);
    }

    lock(value) {
        this.#locked = value;
    }

    #fieldSel() {
        return this.#pricesIncludeTax ? '[data-tax-field="gross"]' : '[data-tax-field="net"]';
    }

    #bindEvents(getTaxRate) {
        this.#$root
            .on('click', '.btn-tax-lock', (e) => {
                // Toggler icon clicked to enable/disable the tax calculation.
                const $btn = $(e.currentTarget);
                const $taxPair = $btn.closest('[data-tax-pair]');
                const active = !$taxPair.is('[data-tax-active]');

                $btn.find('i')
                    .toggleClass('fa-lock', active)
                    .toggleClass('fa-lock-open text-muted', !active);

                $btn.attr('title', this.#res['Admin.Common.TaxCalculator.' + (active ? 'Disable' : 'Enable')]);
                active ? $taxPair.attr('data-tax-active', '') : $taxPair.removeAttr('data-tax-active');

                if (active) {
                    this.#calculate($taxPair.find(this.#fieldSel()));
                }
            })
            .on('change', '[data-tax-field="quantity"]', (e) => {
                // Quantity changed.
                this.#quantity = parseFloat($(e.currentTarget).val()) || 1;
            })
            .on('change', '[data-tax-field="rate"]', (e) => {
                // Tax rate changed.
                if (getTaxRate) {
                    this.#taxRate = (parseFloat($(e.currentTarget).val()) || 0) / 100;
                }

                this.#$root.find(this.#fieldSel()).each((_, el) => {
                    this.#calculate($(el));
                });
            })
            .on('change', '[data-tax-field="gross"], [data-tax-field="net"]', (e) => {
                // Gross or net changed.
                this.#calculate($(e.currentTarget));
            });
    }

    #calculate(el) {
        if (this.#locked) {
            return;
        }

        const $taxPair = el.closest('[data-tax-pair]');
        if (!$taxPair.length || !$taxPair.is('[data-tax-active]')) {
            return;
        }

        this.#locked = true;
        try {
            let grossToNet = this.#pricesIncludeTax;
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
                amount = amount / (1 + this.#taxRate);
            }
            else {
                amount = amount * (1 + this.#taxRate);
            }
            //console.log(`qty:${this.#quantity} taxRate:${this.#taxRate} grossToNet:${grossToNet} amount:${amount}`);

            // INFO: Do not trigger('change')! Causes a stack overflow.
            // See the event handlers that smartstore.numberinput.js listens for.
            $taxPair.find(grossToNet ? '[data-tax-field="net"]' : '[data-tax-field="gross"]')
                .val(amount)
                .trigger('change.ni');

            // Update line total price when unit price is changed or vice versa.
            this.#updateLineTotalOrUnitPrice($taxPair);
        }
        finally {
            this.#locked = false;
        }
    }

    #updateLineTotalOrUnitPrice(sourcePair) {
        let updateLineTotal;
        if (sourcePair.is('[data-tax-pair="unitprice"]')) {
            // Unit price changed -> update line total.
            updateLineTotal = true;
        }
        else if (sourcePair.is('[data-tax-pair="linetotal"]')) {
            // Line total changed -> update unit price.
            updateLineTotal = false;
        }
        else {
            return;
        }

        const $targetPair = this.#$root.find(updateLineTotal ? '[data-tax-pair="linetotal"]' : '[data-tax-pair="unitprice"]');
        if (!$targetPair.length || !$targetPair.is('[data-tax-active]')) {
            return;
        }

        const gross = sourcePair.find('[data-tax-field="gross"]').val();
        if (!isNaN(gross)) {
            $targetPair.find('[data-tax-field="gross"]')
                .val(updateLineTotal ? (gross * this.#quantity) : (gross / this.#quantity))
                .trigger('change.ni');
        }

        const net = sourcePair.find('[data-tax-field="net"]').val();
        if (!isNaN(net)) {
            $targetPair.find('[data-tax-field="net"]')
                .val(updateLineTotal ? (net * this.#quantity) : (net / this.#quantity))
                .trigger('change.ni');
        }
    }
};
