(function ($, window, document, undefined) {

    function RangeSlider(element, options) {
        const self = this;

        this.element = element;
        const el = this.el = $(element);
        const slider = this.slider = el.find('.form-control-range[data-target]').first();
        const hiddenInput = $(slider.data('target'));
        const resetBtn = el.find('.range-reset');
        const badge = el.find('.range-badge');
        const isNullable = el.data('nullable') === true;
        this.options = $.extend({}, options);

        function setUnspecified(reset) {
            el.toggleClass('unspecified', reset);
            resetBtn.toggleClass('d-none', reset);
            badge.toggleClass('d-none', !reset);
        }

        function refreshBubblePosition(e, bubble) {
            const currentBubble = bubble || el.find('.range-value').first();
            const ready = el.hasClass('ready');

            if (currentBubble.length === 0 || el.length === 0) {
                el.addClass('ready');
                return;
            }

            if (!ready && !el.is(':visible')) {
                return;
            }

            if (!ready) {
                // calc Y position only once (when visible but not ready yet)
                const labelTop = Math.floor((slider.position().top + (slider.height() / 2)) - (currentBubble.height() / 2));
                currentBubble.css('top', labelTop + 'px');
                el.addClass('ready');
            }

            // Always refresh X position
            const min = parseFloat(slider.prop('min'));
            const max = parseFloat(slider.prop('max'));
            const range = max - min;
            const ratio = slider.width() / range;
            const n = parseFloat(slider.val()) - min;

            if (Smartstore.globalization.culture.isRTL) {
                currentBubble.css('right', (n * ratio) + 'px').attr('data-placement', n > range / 2 ? 'right' : 'left');
            }
            else {
                currentBubble.css('left', (n * ratio) + 'px').attr('data-placement', n > range / 2 ? 'left' : 'right');
            }

        }

        function updateSlider(e, syncInput) {
            // Move invariant value from slider to an associated hidden field
            // as formatted value. Client validation will fail otherwise.
            const val = slider.val();
            const bubble = el.find('.range-value').first();
            const fmt = el.data('format');

            el
                .css('--slider-value', val)
                .find('.range-value-inner')
                .text(fmt === '{0}' ? fmt.format(val) : eval(fmt.format(val)));

            if (self.initialized && syncInput !== false) {
                const g = Smartstore.globalization;
                const nf = g.culture.numberFormat;
                const formatted = val.replace('.', nf["."]);
                hiddenInput.val(formatted).trigger('change');
            }

            refreshBubblePosition(e, bubble);
            el.trigger('rangeslider:change', [val, el.hasClass('unspecified')]);
        }

        this.isUnspecified = function () {
            return isNullable && el.hasClass('unspecified');
        };

        this.setValue = function (value) {
            slider.val(value).trigger('input').trigger('change');
        };

        this.reset = function () {
            if (!isNullable) {
                return;
            }

            setUnspecified(true);
            hiddenInput.val('').trigger('change');
            slider.val(el.data('default-value'));
            updateSlider(null, false);
        };

        this.init = function () {
            if (slider.length === 0) {
                return;
            }

            slider.on('input', (e) => {
                if (self.isUnspecified()) {
                    setUnspecified(false);
                }

                updateSlider(e);
            });

            badge.on('click', () => {
                self.setValue(slider.val());
            });

            resetBtn.on('click', (e) => {
                e.preventDefault();
                self.reset();
            });

            // align bubble
            el.on('mouseenter', refreshBubblePosition);

            updateSlider();
        };

        this.initialized = false;
        this.init();
        this.initialized = true;
    }

    $.fn.rangeSlider = function (options) {
        return this.each(function () {
            if (!$.data(this, 'rangeSlider')) {
                $.data(this, 'rangeSlider', new RangeSlider(this, options));
            }
        });
    };

})(jQuery, this, document);

