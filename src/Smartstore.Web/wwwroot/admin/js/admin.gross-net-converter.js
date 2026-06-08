Smartstore.Admin.grossNetConverter = (() => {
    let pricesIncludeTax = true;
    let taxRate = 0;
    let getTaxRate = true;

    return {
        initialize: (options) => {
            pricesIncludeTax = options.pricesIncludeTax;

            if (options.taxRate !== undefined) {
                taxRate = (parseFloat(options.taxRate) || 0) / 100;
                getTaxRate = taxRate === 0;
            }            

            // Enable/disable the gross/net conversion.
            $('.gross-net-converter').on('click', '.btn-conversion-toggler', (e) => {
                const T = window.grossNetConverterRes;
                const $el = $(e.currentTarget);
                const enable = $el.hasClass('conversion-disabled');

                $el.find('i')
                    .toggleClass('fa-lock', enable)
                    .toggleClass('fa-unlock-keyhole text-muted', !enable);

                $el.toggleClass('conversion-enabled', enable)
                    .toggleClass('conversion-disabled', !enable)
                    .attr('title', T[enable ? 'Admin.Common.GrossNetConversion.Disable' : 'Admin.Common.GrossNetConversion.Enable']);

                if (enable) {
                    convert($el);
                }
            });
        }
    };

    function convert(el) {
        const $ctn = el.closest('.gross-net-converter');
        let grossToNet = pricesIncludeTax;

        if (el.hasClass('conversion-gross')) {
            // Gross field updated.
            grossToNet = true;
        }
        else if (el.hasClass('conversion-net')) {
            // Net field updated.
            grossToNet = false;
        }

        if (getTaxRate) {
            // Get current tax rate.
            const rawRate = el.hasClass('conversion-taxrate') ? el.val() : $ctn.find('.conversion-taxrate').val();
            taxRate = (parseFloat(rawRate) || 0) / 100;
        }

        const amount = parseFloat($ctn.find(grossToNet ? '.conversion-gross' : '.conversion-net').val()) || 0;
        //console.log(`grossToNet:${grossToNet} rate:${taxRate} amount:${amount}`);

        if (grossToNet) {
            $ctn.find('.conversion-net').val(amount / (1 + taxRate));
        }
        else {
            $ctn.find('.conversion-gross').val(amount * (1 + taxRate));
        }
    }

})();
