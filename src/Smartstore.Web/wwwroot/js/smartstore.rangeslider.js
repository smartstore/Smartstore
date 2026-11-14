(function ($, window, document, undefined) {

    function RangeSlider(element, options) {
        const self = this;

        this.element = element;
        const el = this.el = $(element);
        const slider = this.slider = el.find('.form-range-input[data-target]').first();
        const hiddenInput = $(slider.data('target'));
        const resetBtn = el.find('.range-reset');
        const badge = el.find('.range-badge');
        const isNullable = el.data('nullable') === true;
        this.options = $.extend({}, options);
        // Conversion separates native track coordinates from stored model values.
        const toPosition = this.options.toPosition || (value => value);
        const fromPosition = this.options.fromPosition || (value => value);
        const fmt = el.attr('data-format') ?? '{0}';
        const valueDecoder = document.createElement('textarea');
        let bubble;

        function setUnspecified(reset) {
            el.toggleClass('unspecified', reset);
            resetBtn.toggleClass('d-none', reset);
            badge.toggleClass('d-none', !reset);
        }

        function updateSlider(e, syncInput) {
            // Move invariant value from slider to an associated hidden field
            // as formatted value. Client validation will fail otherwise.
            const val = String(fromPosition(slider.val()));
            const min = Number(slider.prop('min'));
            const max = Number(slider.prop('max'));
            const fill = max > min ? Math.max(0, Math.min(1, (Number(slider.val()) - min) / (max - min))) : 0;
            el.css({ '--slider-value': slider.val(), '--range-fill': fill });
            if (fmt) {
                const text = fmt === '{0}' ? val : eval(fmt.format(val));
                // Existing expression formats can contain HTML entities, but never render markup.
                valueDecoder.innerHTML = String(text);
                const decoded = valueDecoder.value;
                bubble?.find('.range-value-inner').text(decoded);
                slider.attr('aria-valuetext', decoded);
            }

            if (self.initialized && syncInput !== false) {
                const g = Smartstore.globalization;
                const nf = g.culture.numberFormat;
                const formatted = val.replace('.', nf["."]);
                hiddenInput.val(formatted).trigger('change');
            }

            el.trigger('rangeslider:change', [val, el.hasClass('unspecified')]);
        }

        this.isUnspecified = function () {
            return isNullable && el.hasClass('unspecified');
        };

        this.setValue = function (value) {
            slider.val(toPosition(value)).trigger('input').trigger('change');
        };

        this.reset = function () {
            if (!isNullable) {
                return;
            }

            setUnspecified(true);
            hiddenInput.val('').trigger('change');
            slider.val(toPosition(el.data('default-value')));
            updateSlider(null, false);
        };

        this.init = function () {
            if (slider.length === 0) {
                return;
            }

            if (this.options.toPosition) {
                slider.val(toPosition(slider.attr('value')));
                if (!self.isUnspecified()) {
                    hiddenInput.val(String(fromPosition(slider.val())).replace('.', Smartstore.globalization.culture.numberFormat["."]));
                }
            }

            // Labels generated for the model field should focus the visible, uniquely identified input.
            if (slider[0].id && hiddenInput[0]?.id) {
                document.querySelectorAll('label[for="' + CSS.escape(hiddenInput[0].id) + '"]').forEach(label => {
                    label.htmlFor = slider[0].id;
                });
            }

            if (fmt && el.data('bubble') === true) {
                bubble = $('<div class="range-value" aria-hidden="true"><div class="range-value-inner text-nowrap"></div></div>');
                slider.after(bubble);
            }

            const options = slider[0].list?.options;
            if (options?.length) {
                const min = Number(slider.prop('min') || 0);
                const max = Number(slider.prop('max') || 100);
                const labels = new Map();
                for (const option of options) {
                    const value = Number(option.value);
                    if (!option.value.trim() || !Number.isFinite(value) || value < min || value > max || labels.has(value)) continue;
                    // Keep the first label at each position; labels are always inserted as text.
                    valueDecoder.innerHTML = option.label;
                    labels.set(value, valueDecoder.value);
                }
                if (labels.size && Number.isFinite(min) && Number.isFinite(max) && max > min) {
                    // Include empty boundary intervals so inner-only ticks retain their true positions.
                    const positions = [...new Set([min, ...labels.keys(), max])].sort((a, b) => a - b);
                    const columns = positions.slice(1).map((value, i) => `${100 * ((value - positions[i]) / (max - min))}fr`);
                    const ticks = $('<div class="form-range-ticks" aria-hidden="true"></div>')
                        .css('grid-template-columns', [...columns, '0'].join(' '));
                    positions.forEach((value, i) => {
                        if (!labels.has(value)) return;
                        const tick = $('<div class="form-range-tick"></div>')
                            .attr('data-value', labels.get(value))
                            .css('grid-column', i + 1)
                            .toggleClass('form-range-tick-start', value === min)
                            .toggleClass('form-range-tick-end', value === max)
                            .appendTo(ticks);
                        $('<span class="form-range-tick-label"></span>').text(labels.get(value)).appendTo(tick);
                    });
                    slider.parent().append(ticks);
                }
            }

            slider.on('input', (e) => {
                if (self.isUnspecified()) {
                    setUnspecified(false);
                }

                updateSlider(e);
            });

            badge.on('click', () => {
                self.setValue(fromPosition(slider.val()));
            });

            resetBtn.on('click', (e) => {
                e.preventDefault();
                self.reset();
            });

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
