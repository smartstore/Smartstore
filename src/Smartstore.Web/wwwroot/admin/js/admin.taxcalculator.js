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
    #taxRate = 0;       // Tax rate value between 0 and 1.
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

        this.#taxRate = options.taxRate;
        if (this.#taxRate === undefined) {
            const $elTaxRate = this.#$root.find('[data-tax-field="taxrate"]');
            this.#taxRate = ($elTaxRate.length ? this.#getNumber($elTaxRate) : this.#parseTaxRates().rate) / 100;
        }
        
        this.#bindEvents();
    }

    lock(value) {
        this.#locked = value;
    }

    #bindEvents() {
        this.#$root
            .on('click', '.btn-tax-lock', (e) => {
                // Toggler icon clicked to enable/disable the tax calculation.
                const $btn = $(e.currentTarget);
                const $pair = $btn.closest('[data-tax-pair]');
                const activate = !$pair.is('[data-tax-active]');

                $btn.find('i')
                    .toggleClass('fa-lock', activate)
                    .toggleClass('fa-lock-open text-muted', !activate);

                $btn.attr('title', this.#res['Admin.Common.TaxCalculator.' + (activate ? 'Disable' : 'Enable')]);
                activate ? $pair.attr('data-tax-active', '') : $pair.removeAttr('data-tax-active');

                this.#autoUpdate = this.#$root.find('[data-tax-active]').length > 0;

                if (activate) {
                    $pair.find(this.#pricesIncludeTax ? '[data-tax-field="gross"]' : '[data-tax-field="net"]').trigger('change');
                }
            })
            .on('change', '[data-tax-field]', (e) => {
                this.#handleChange($(e.currentTarget));
            });
    }

    #handleChange(el) {
        if (this.#locked) {
            return;
        }

        const name = el.data('tax-field');
        if (name === 'quantity') {
            // Quantity changed. Do nothing because it affects server-side calculation (AJAX call required).
            return;
        }

        let elTrigger;

        this.#locked = true;
        try {
            if (name === 'taxrate') {
                // Tax rate changed.
                this.#updateTaxRate(this.#getNumber(el));
            }
            else if (name === 'gross' || name === 'net') {
                this.#updatePair(el, name);
            }

            // Update order total, tax amount, rates etc.
            elTrigger = this.#calculateOrder(el, name);
        }
        catch (e) {
            console.error(e);
        }
        finally {
            this.#locked = false;
        }

        if (this.#autoUpdate && elTrigger) {
            // Total changed -> subtotal updated -> finally update all the rest.
            elTrigger.trigger('change');
        }
    }

    #updatePair(el, name) {
        const $pair = el.closest('[data-tax-pair]');
        if (!$pair.length) {
            return;
        }

        if ($pair.is('[data-tax-active]')) {
            let grossToNet = this.#pricesIncludeTax;
            if (name === 'gross') {
                // Gross changed.
                grossToNet = true;
            }
            else if (name === 'net') {
                // Net changed.
                grossToNet = false;
            }
            else {
                throw new Error(`Invalid field name "${name} for updating tax pair. Expected "gross" or "net".`);
            }

            let amount = parseFloat(el.val());
            if (!isNaN(amount)) {
                amount = grossToNet ? (amount / (1 + this.#taxRate)) : (amount * (1 + this.#taxRate));

                //console.log(`taxRate:${this.#taxRate} grossToNet:${grossToNet} amount:${amount}`);
                this.#updateNumber($pair.find(grossToNet ? '[data-tax-field="net"]' : '[data-tax-field="gross"]'), amount);
            }
        }

        if ($pair.is('[data-tax-pair="unitprice"]')) {
            // Unit price changed -> update line total.
            this.#updateLineTotalOrUnitPrice($pair, true);
        }
        else if ($pair.is('[data-tax-pair="linetotal"]')) {
            // Line total changed -> update unit price.
            this.#updateLineTotalOrUnitPrice($pair, false);
        }
    }

    // Update line total price when unit price is changed or vice versa, if at least one tax calculator is active.
    #updateLineTotalOrUnitPrice(pair, lineTotal) {
        if (!this.#autoUpdate) {
            return;
        }

        const $target = this.#$root.find(lineTotal ? '[data-tax-pair="linetotal"]' : '[data-tax-pair="unitprice"]');
        if (!$target.length || !$target.is('[data-tax-active]')) {
            return;
        }

        const quantity = this.#getNumber('quantity');
        const gross = pair.find('[data-tax-field="gross"]').val();
        const net = pair.find('[data-tax-field="net"]').val();

        if (!isNaN(gross)) {
            this.#updateNumber($target.find('[data-tax-field="gross"]'),
                lineTotal ? (gross * quantity) : (gross / quantity));
        }

        if (!isNaN(net)) {
            this.#updateNumber($target.find('[data-tax-field="net"]'),
                lineTotal ? (net * quantity) : (net / quantity));
        }
    }

    // Calculates the order and updates all fields, if at least one tax calculator is active.
    #calculateOrder(el, name) {
        if (!this.#$total.length || !this.#autoUpdate) {
            // Do not update, if no tax converter is active (pure manual editing mode).
            return;
        }

        try {
            const subtotal = this.#getPair('subtotal');
            const discount = this.#getPair('discount');
            const shipping = this.#getPair('shipping');
            const paymentFee = this.#getPair('paymentfee');
            const orderDiscount = this.#getNumber('orderdiscount');
            const creditBalance = this.#getNumber('creditbalance');
            const rounding = this.#getNumber('rounding');

            // Update tax amount and tax rates.
            let taxAmount;
            if (name === 'taxamount') {
                // Calculate from changed tax value.
                taxAmount = this.#getNumber(el);
                this.#updateTaxRates(taxAmount);

                // INFO: In accounting, the tax amount is usually the result of a calculation, not the source.
                // If the user changes the tax amount, the system should treat this as a "tax adjustment entry"
                // rather than attempting to recalculate the subtotal.
            }
            else if (name === 'taxrates') {
                // Parse tax amount from tax rates field.
                const rates = this.#parseTaxRates();
                taxAmount = rates.amount;
                this.#updateNumber('taxamount', taxAmount);

                // Update tax rate if changed.
                if (this.#updateTaxRate(rates.rate)) {
                    return subtotal.$pair.find('[data-tax-field="gross"]');
                }
            }
            else {
                // Calculate from difference between gross and net.
                taxAmount = (subtotal.gross - subtotal.net)
                    + (discount.gross - discount.net)
                    + (shipping.gross - shipping.net)
                    + (paymentFee.gross - paymentFee.net);
                this.#updateNumber('taxamount', taxAmount);
                this.#updateTaxRates(taxAmount);
            }

            if (name === 'total') {
                if (subtotal.$pair.is('[data-tax-active]')) {
                    // Calculate subtotal from changed total value.
                    const total = this.#getNumber(this.#$total);
                    const $target = subtotal.$pair.find('[data-tax-field="gross"]');

                    const subtotalGross = total
                        + discount.gross
                        - shipping.gross
                        - paymentFee.gross
                        + orderDiscount
                        + creditBalance
                        - rounding;

                    this.#updateNumber($target, subtotalGross);
                    return $target;
                }
            }
            else {
                // Calculate total.
                const total = subtotal.gross
                    - discount.gross
                    + shipping.gross
                    + paymentFee.gross
                    - orderDiscount
                    - creditBalance
                    + rounding;

                this.#updateNumber(this.#$total, total.toFixed(this.#decimals));
            }
        }
        catch (e) {
            console.error(e);
        }
    }

    // Parses the tax rates field, which is the tax amount per tax rate formatted string.
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
            taxAmount ??= this.#getNumber('taxamount');

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

    // Updates the tax rate and recalculates all tax pairs fields if the tax rate has changed.
    #updateTaxRate(taxRate) {
        if (taxRate > 1) {
            taxRate = taxRate / 100;
        }

        if (taxRate === this.#taxRate) {
            return false;
        }

        this.#taxRate = taxRate;

        const name = this.#pricesIncludeTax ? 'gross' : 'net';
        this.#$root.find(`[data-tax-field="${name}"]`).each((_, el2) => {
            this.#updatePair($(el2), name);
        });

        return true;
    }

    // Returns a tax pair including its gross and net values.
    #getPair(kind) {
        const $pair = this.#$root.find(`[data-tax-pair="${kind}"]`);

        return {
            $pair: $pair,
            gross: this.#getNumber($pair.find('[data-tax-field="gross"]')),
            net: this.#getNumber($pair.find('[data-tax-field="net"]'))
        };
    }

    // Returns the numeric value of a field. Throws an error if the value is not a number.
    #getNumber(nameOrElement) {
        const el = typeof nameOrElement === 'string' ? this.#$root.find(`[data-tax-field="${nameOrElement}"]`) : nameOrElement;
        const result = parseFloat(el.val());

        if (isNaN(result)) {
            const name = el.data("tax-field");
            throw new Error(`Value of ${name} is not a number!`);
        }

        return result;
    }

    // Updates the numeric value of a field.
    #updateNumber(nameOrElement, value) {
        if (!isNaN(value)) {
            // INFO: Do not trigger('change')! Causes a stack overflow.
            // See the event handlers that smartstore.numberinput.js listens for.

            const el = typeof nameOrElement === 'string' ? this.#$root.find(`[data-tax-field="${nameOrElement}"]`) : nameOrElement;
            el.val(value).trigger('change.ni');
        }
    }
};
