(function ($) {
    'use strict';

    function update(control) {
        const input = control.querySelector('[data-fixed-or-percent-percentage]');
        $(control).find('[data-fixed-or-percent-toggle]')
            .toggleClass('active', input.checked).attr('aria-pressed', String(input.checked))
            .prop('disabled', input.disabled);
        control.querySelector('.numberinput-postfix').textContent = input.checked ? '%' : control.dataset.fixedOrPercentPostfix;
    }

    $.fn.fixedOrPercent = function () {
        return this.each(function () {
            update(this);
        });
    };
    $(document).on('click.fixedOrPercent', '[data-fixed-or-percent-toggle]', function () {
        const input = this.closest('[data-fixed-or-percent]').querySelector('[data-fixed-or-percent-percentage]');
        if (input.disabled) return;
        $(input).prop('checked', !input.checked).trigger('change');
    });
    $(document).on('change.fixedOrPercent', '[data-fixed-or-percent-percentage]', function () {
        update(this.closest('[data-fixed-or-percent]'));
    });
})(jQuery);
