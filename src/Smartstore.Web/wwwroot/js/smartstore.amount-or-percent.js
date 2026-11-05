(function ($) {
    function update(control) {
        control.querySelectorAll('[data-amount-percentage]').forEach(input => {
            const label = input.closest('label');
            label.classList.toggle('active', input.checked);
            label.classList.toggle('disabled', input.disabled);
        });
    }

    $.fn.amountOrPercent = function () {
        return this.each(function () {
            update(this);
        });
    };
    $(document).on('change', '[data-amount-percentage]', function () {
        const control = this.closest('[data-amount-or-percent]');
        // Bootstrap toggles the active class after firing change.
        queueMicrotask(() => update(control));
    });
})(jQuery);
