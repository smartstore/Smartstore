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
    #locked = false;
    #$root;
    #res;

    constructor(options) {
        this.#$root = $(options.rootSelector || 'form:first');
        this.#pricesIncludeTax = options.pricesIncludeTax;
        this.#decimals = parseInt(options.decimals ?? 2) || 2;
        this.#res = options.res || {};
        
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

        const ctx = this.#getContext(el);
        let elTrigger;

        this.#locked = true;
        try {
            if (ctx.name === 'taxrate') {
                // Tax rate changed. Invalidate cached tax rate.
                ctx.$area.data('tax-rate', -1);

                this.#updateAllPairs(ctx);
            }
            else if (ctx.name === 'quantity') {
                this.#updateLineTotalOrUnitPrice(ctx, ctx.$area.find('[data-tax-pair="unitprice"]'), true);
            }
            else if (ctx.name === 'gross' || ctx.name === 'net') {
                this.#updatePair(ctx, el, ctx.name);
            }

            // Update order total, tax amount, rates etc.
            elTrigger = this.#calculateOrder(ctx);
        }
        catch (e) {
            console.error(e);
        }
        finally {
            this.#locked = false;
        }

        if (elTrigger && ctx.autoUpdate) {
            // Total changed -> subtotal updated -> finally update all the rest.
            elTrigger.trigger('change');
        }
    }

    #updateAllPairs(ctx) {
        const name = this.#pricesIncludeTax ? 'gross' : 'net';

        this.#getField(ctx, name).each((_, el) => {
            this.#updatePair(ctx, $(el), name);
        });
    }

    #updatePair(ctx, el, name) {
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
                throw new Error(`Invalid field name "${name}" for updating tax pair. Expected "gross" or "net".`);
            }

            let amount = parseFloat(el.val());
            if (!isNaN(amount)) {
                const taxRate = this.#getTaxRate(ctx);
                amount = grossToNet ? (amount / (1 + taxRate)) : (amount * (1 + taxRate));

                //console.log(`taxRate:${taxRate} grossToNet:${grossToNet} amount:${amount}`);
                this.#updateNumber(ctx, $pair.find(grossToNet ? '[data-tax-field="net"]' : '[data-tax-field="gross"]'), amount);
            }
        }

        if (ctx.autoUpdate) {
            if ($pair.is('[data-tax-pair="unitprice"]')) {
                // Unit price changed -> update line total.
                this.#updateLineTotalOrUnitPrice(ctx, $pair, true);
            }
            else if ($pair.is('[data-tax-pair="linetotal"]')) {
                // Line total changed -> update unit price.
                this.#updateLineTotalOrUnitPrice(ctx, $pair, false);
            }
        }
    }

    // Update line total price when unit price is changed or vice versa, if at least one tax calculator is active.
    #updateLineTotalOrUnitPrice(ctx, pair, lineTotal) {
        const $target = ctx.$area.find(lineTotal ? '[data-tax-pair="linetotal"]' : '[data-tax-pair="unitprice"]');
        if (!$target.length || !$target.is('[data-tax-active]')) {
            return;
        }

        // TODO: (mg) discount missing/ignored.
        const quantity = this.#getNumber(ctx, 'quantity');
        const gross = pair.find('[data-tax-field="gross"]').val();
        const net = pair.find('[data-tax-field="net"]').val();

        if (!isNaN(gross)) {
            this.#updateNumber(ctx, $target.find('[data-tax-field="gross"]'),
                lineTotal ? (gross * quantity) : (gross / quantity));
        }

        if (!isNaN(net)) {
            this.#updateNumber(ctx, $target.find('[data-tax-field="net"]'),
                lineTotal ? (net * quantity) : (net / quantity));
        }
    }

    // Calculates the order and updates all fields, if at least one tax calculator is active.
    #calculateOrder(ctx) {
        const $total = this.#getField(ctx, 'total');

        if (!$total.length || !ctx.autoUpdate) {
            // Do not update. We are in pure manual editing mode, if there is no tax converter active.
            return;
        }

        try {
            const subtotal = this.#getPair(ctx, 'subtotal');
            const subtotalDiscount = this.#getPair(ctx, 'subtotaldiscount');
            const shipping = this.#getPair(ctx, 'shipping');
            const paymentFee = this.#getPair(ctx, 'paymentfee');
            const orderDiscount = this.#getNumber(ctx, 'orderdiscount');
            const creditBalance = this.#getNumber(ctx, 'creditbalance');
            const rounding = this.#getNumber(ctx, 'rounding');

            // Update tax amount and tax rates.
            let taxAmount;
            if (ctx.name === 'taxamount') {
                // Calculate from changed tax value.
                taxAmount = this.#getNumber(ctx, ctx.$el);
                this.#updateTaxesString(ctx, taxAmount);

                // INFO: In accounting, the tax amount is usually the result of a calculation, not the source.
                // If the user changes the tax amount, the system should treat this as a "tax adjustment entry"
                // rather than attempting to recalculate the subtotal.
            }
            else if (ctx.name === 'taxrates') {
                // Parse tax amount and rate from tax rates string field.
                const rates = this.#parseTaxRates(ctx);
                taxAmount = rates.amount;
                this.#updateNumber(ctx, 'taxamount', taxAmount);

                const newRate = rates.rate > 1 ? (rates.rate / 100) : rates.rate;
                const currentRate = this.#getTaxRate(ctx, true);

                if (isNaN(currentRate) || currentRate !== newRate) {
                    // Tax rate changed -> update all tax pairs.
                    ctx.$area.data('tax-rate', newRate);

                    this.#updateAllPairs(ctx);

                    // Trigger a recalculation of the total.
                    return subtotal.$pair.find('[data-tax-field="gross"]');
                }
            }
            else {
                // Calculate from difference between gross and net.
                taxAmount = (subtotal.gross - subtotal.net)
                    + (subtotalDiscount.gross - subtotalDiscount.net)
                    + (shipping.gross - shipping.net)
                    + (paymentFee.gross - paymentFee.net);
                this.#updateNumber(ctx, 'taxamount', taxAmount);
                this.#updateTaxesString(ctx, taxAmount);
            }

            if (ctx.name === 'total') {
                if (subtotal.$pair.is('[data-tax-active]')) {
                    // Update subtotal from changed total value.
                    const total = this.#getNumber(ctx, $total);
                    const $target = subtotal.$pair.find('[data-tax-field="gross"]');

                    const subtotalGross = total
                        + subtotalDiscount.gross
                        - shipping.gross
                        - paymentFee.gross
                        + orderDiscount
                        + creditBalance
                        - rounding;

                    this.#updateNumber(ctx, $target, subtotalGross);

                    // Trigger a recalculation of the total.
                    return $target;
                }
            }
            else {
                // Update total.
                const total = subtotal.gross
                    - subtotalDiscount.gross
                    + shipping.gross
                    + paymentFee.gross
                    - orderDiscount
                    - creditBalance
                    + rounding;

                this.#updateNumber(ctx, $total, total.toFixed(this.#decimals));
            }
        }
        catch (e) {
            console.error(e);
        }
    }

    // Returns the tax rate for the current area as a value between 0 and 1.
    #getTaxRate(ctx, cachedOnly) {
        // Perf: Cache the tax rate for current area to avoid parsing it multiple times.
        const rawRate = ctx.$area.data('tax-rate');
        let taxRate = +rawRate; // Convert to number (NaN if not a number).

        if (cachedOnly === true || (taxRate >= 0 && taxRate <= 1)) {
            return taxRate;
        }

        const $elTaxRate = this.#getField(ctx, 'taxrate');
        taxRate = ($elTaxRate.length ? this.#getNumber(ctx, $elTaxRate) : this.#parseTaxRates(ctx).rate) / 100;

        ctx.$area.data('tax-rate', taxRate);
        return taxRate;
    }

    // Parses the tax rates field, which is the tax amount per tax rate formatted string.
    #parseTaxRates(ctx) {
        const $elTaxRates = this.#getField(ctx, 'taxrates');
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
                    const errTaxName = isNaN(result.rate) ? 'rate' : 'amount';
                    throw new Error(`Tax ${errTaxName} is not a number!`);
                }

                return result;
            }
        }

        throw new Error(`Invalid tax rates format!`);
    }

    // Updates the tax rates field, which is the tax amount per tax rate formatted string.
    #updateTaxesString(ctx, taxAmount) {
        try {
            taxAmount ??= this.#getNumber(ctx, 'taxamount');

            const $elTaxRates = this.#getField(ctx, 'taxrates');
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

    // Returns the context of the element, including the element itself and the closest tax area.
    #getContext(el) {
        let $area = el.closest('[data-tax-area]');
        $area = $area.length ? $area : this.#$root;

        return {
            $el: el,
            $area: $area,
            name: el.data('tax-field') || '',
            autoUpdate: $area.find('[data-tax-active]').length > 0
        };
    }

    // Returns a tax pair including its gross and net values.
    #getPair(ctx, kind) {
        const $pair = ctx.$area.find(`[data-tax-pair="${kind}"]`);

        return {
            $pair: $pair,
            gross: this.#getNumber(ctx, $pair.find('[data-tax-field="gross"]')),
            net: this.#getNumber(ctx, $pair.find('[data-tax-field="net"]'))
        };
    }

    #getField(ctx, name) {
        return ctx.$area.find(`[data-tax-field="${name}"]`);
    }

    // Returns the numeric value of a field. Throws an error if the value is not a number.
    #getNumber(ctx, nameOrElement) {
        const el = typeof nameOrElement === 'string' ? this.#getField(ctx, nameOrElement) : nameOrElement;
        const result = parseFloat(el.val());

        if (isNaN(result)) {
            throw new Error(`Value of "${el.data("tax-field")}" is not a number!`);
        }

        return result;
    }

    // Updates the numeric value of a field.
    #updateNumber(ctx, nameOrElement, value) {
        if (!isNaN(value)) {
            // INFO: Do not trigger('change')! Causes a stack overflow.
            // See the event handlers that smartstore.numberinput.js listens for.
            const el = typeof nameOrElement === 'string' ? this.#getField(ctx, nameOrElement) : nameOrElement;
            el.val(value).trigger('change.ni');
        }
    }
};
