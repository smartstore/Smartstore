/*
* Project: Smartstore gross/net tax calculation.
* Author: Marcus Gesing, SmartStore AG
*
* The net value is calculated automatically when the corresponding gross value changes—and vice versa. 
* In addition, fields such as the total amount or the total tax amount are updated.
*
* Server-side price calculation takes precedence. If the values affecting the calculation change (e.g. quantity or variant),
* the new prices take precedence and override any previous manual changes.
*
* Calculations are performed without rounding. Only the total amount is rounded to the specified number of decimal places.
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
    #decimals = 2;      // The number of decimal places (of the primary currency) to be rounded to.
    #taxRate = 0;
    #quantity = 1;
    #autoUpdate;        // A value indicating whether to automatically update/recalculate other fields like line total or order total.
    #locked = false;
    #$root;
    #$total;
    #res;

    constructor(options) {
        this.#$root = $(options.rootSelector || 'form:first');
        this.#$total = this.#$root.find('[data-tax-field="total"]');
        this.#autoUpdate = this.#$root.find('[data-tax-active]').length > 0;
        this.#pricesIncludeTax = options.pricesIncludeTax;
        this.#decimals = parseInt(options.decimals ?? 2) || 2;
        this.#res = options.res || {};

        const getTaxRate = options.taxRate === undefined;
        this.#taxRate = (parseFloat(getTaxRate ? this.#$root.find('[data-tax-field="taxrate"]').val() : options.taxRate) || 0) / 100;

        this.#bindEvents(getTaxRate);
    }

    lock(value) {
        this.#locked = value;
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

                this.#autoUpdate = this.#$root.find('[data-tax-active]').length > 0;

                if (active) {
                    this.#calculate($taxPair.find(this.#fieldSel()));
                }
            })
            .on('change', '[data-tax-field="quantity"]', (e) => {
                // Quantity changed.
                this.#quantity = parseFloat($(e.currentTarget).val()) || 1;
            })
            .on('change', '[data-tax-field="taxrate"]', (e) => {
                // Tax rate changed.
                if (getTaxRate) {
                    this.#taxRate = (parseFloat($(e.currentTarget).val()) || 0) / 100;
                }

                this.#$root.find(this.#fieldSel()).each((_, el) => {
                    this.#calculate($(el));
                });
            })
            .on('change', '[data-tax-field]:not([data-tax-field="gross"]):not([data-tax-field="net"])', (e) => {
                this.#recalculateOrder($(e.currentTarget));
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
        if (!$taxPair.length) {
            return;
        }

        this.#locked = true;
        try {
            if ($taxPair.is('[data-tax-active]')) {
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

                this.#updateAmount($taxPair.find(grossToNet ? '[data-tax-field="net"]' : '[data-tax-field="gross"]'), amount);
            }

            // Update line total price when unit price is changed or vice versa.
            this.#updateLineTotalOrUnitPrice($taxPair);

            // Updare order total, tax amount and rates.
            this.#recalculateOrder(el);
        }
        finally {
            this.#locked = false;
        }
    }

    // Update line total price when unit price is changed or vice versa, if at least one tax calculator is active.
    #updateLineTotalOrUnitPrice(el) {
        if (!this.#autoUpdate) {
            return;
        }

        let updateLineTotal;
        if (el.is('[data-tax-pair="unitprice"]')) {
            // Unit price changed -> update line total.
            updateLineTotal = true;
        }
        else if (el.is('[data-tax-pair="linetotal"]')) {
            // Line total changed -> update unit price.
            updateLineTotal = false;
        }
        else {
            return;
        }

        const $target = this.#$root.find(updateLineTotal ? '[data-tax-pair="linetotal"]' : '[data-tax-pair="unitprice"]');
        if (!$target.length || !$target.is('[data-tax-active]')) {
            return;
        }

        const gross = el.find('[data-tax-field="gross"]').val();
        if (!isNaN(gross)) {
            this.#updateAmount($target.find('[data-tax-field="gross"]'),
                updateLineTotal ? (gross * this.#quantity) : (gross / this.#quantity));
        }

        const net = el.find('[data-tax-field="net"]').val();
        if (!isNaN(net)) {
            this.#updateAmount($target.find('[data-tax-field="net"]'),
                updateLineTotal ? (net * this.#quantity) : (net / this.#quantity));
        }
    }

    // Recalculates the order and updates all fields, if at least one tax calculator is active.
    #recalculateOrder(el) {
        if (!this.#$total.length || !this.#autoUpdate) {
            // If no tax converter is active, the remaining fields are not updated automatically (pure manual editing mode).
            return;
        }

        try {
            const field = el.data('tax-field');
            const subtotal = this.#getAmounts('subtotal');
            const discount = this.#getAmounts('discount');
            const shipping = this.#getAmounts('shipping');
            const paymentFee = this.#getAmounts('paymentfee');
            const orderDiscount = this.#getAmount('orderdiscount');
            const creditBalance = this.#getAmount('creditbalance');
            const rounding = this.#getAmount('rounding');

            // Update tax amount and tax rates.
            let taxAmount;
            if (field === 'taxamount') {
                // Recalculate from changed tax value.
                taxAmount = this.#getAmount(el);
                this.#updateTaxRates(taxAmount);
            }
            else if (field === 'taxrates') {
                // Parse tax amount from tax rates field.
                taxAmount = this.#parseTaxRates().amount;
                this.#updateAmount('taxamount', taxAmount);
            }
            else {
                // Recalculate from difference between gross and net.
                taxAmount = (subtotal.gross - subtotal.net)
                    + (discount.gross - discount.net)
                    + (shipping.gross - shipping.net)
                    + (paymentFee.gross - paymentFee.net);
                this.#updateAmount('taxamount', taxAmount);
                this.#updateTaxRates(taxAmount);
            }

            const total = subtotal.gross
                - discount.gross
                + shipping.gross
                + paymentFee.gross
                - orderDiscount
                - creditBalance
                + rounding;

            this.#updateAmount(this.#$total, total.toFixed(this.#decimals));

            // TODO more...?
            // Update subtotal (net and gross) if the tax amount or tax rate changes.
            // Update subtotal (net and gross) if the total changes.
        }
        catch (e) {
            console.error(e);
        }
    }

    #parseTaxRates() {
        const $elTaxRates = this.#$root.find('[data-tax-field="taxrates"]');
        if ($elTaxRates.length) {
            let str = $elTaxRates.val();
            let arr = str.split(';');
            if (arr.length > 0) {
                str = arr[0];
            }

            arr = str.split(':');
            if (arr.length == 2) {
                const result = {
                    rate: parseFloat(arr[0].trim()),
                    amount: parseFloat(arr[1].trim())
                }

                if (isNaN(result.rate) || isNaN(result.amount)) {
                    const str = isNaN(result.rate) ? 'rate' : 'amount';
                    throw new Error(`Tax ${str} is not a number!`);
                }

                return result;
            }
        }

        throw new Error(`Invalid tax rates format!`);
    }

    // Updates the tax rates field, which is the tax amount per tax rate formatted string.
    #updateTaxRates(taxAmount) {
        try {
            taxAmount ??= this.#getAmount('taxamount');

            const $elTaxRates = this.#$root.find('[data-tax-field="taxrates"]');
            if ($elTaxRates.length) {
                let str = $elTaxRates.val();
                let arr = str.split(';');
                if (arr.length > 0) {
                    str = arr[0];
                }

                arr = str.split(':');
                if (arr.length == 2) {
                    // For some reason it always ends with a semicolon.
                    $elTaxRates.val(`${arr[0].trim()}:${taxAmount};`);
                }
            }
        }
        catch (e) {
            console.error(e);
        }
    }

    #fieldSel() {
        return this.#pricesIncludeTax ? '[data-tax-field="gross"]' : '[data-tax-field="net"]';
    }

    #getAmounts(kind) {
        const $taxPair = this.#$root.find(`[data-tax-pair="${kind}"]`);

        const result = {
            gross: parseFloat($taxPair.find('[data-tax-field="gross"]').val()),
            net: parseFloat($taxPair.find('[data-tax-field="net"]').val())
        };

        if (isNaN(result.gross) || isNaN(result.net)) {
            const str = isNaN(result.gross) ? 'Gross' : 'Net';
            throw new Error(`${str} amount for ${kind} is not a number!`);
        }

        return result;
    }

    #getAmount(nameOrElement) {
        const el = typeof nameOrElement === 'string' ? this.#$root.find(`[data-tax-field="${nameOrElement}"]`) : nameOrElement;
        const result = parseFloat(el.val());

        if (isNaN(result)) {
            throw new Error(`Amount for ${nameOrElement} is not a number!`);
        }

        return result;
    }

    #updateAmount(nameOrElement, value) {
        if (!isNaN(value)) {
            // INFO: Do not trigger('change')! Causes a stack overflow.
            // See the event handlers that smartstore.numberinput.js listens for.

            const el = typeof nameOrElement === 'string' ? this.#$root.find(`[data-tax-field="${nameOrElement}"]`) : nameOrElement;
            el.val(value).trigger('change.ni');
        }
    }
};
