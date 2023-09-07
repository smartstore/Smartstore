;
(function ($) {

    var swatchColors = null;

    function getSwatchColors() {
        if (swatchColors == null) {
            const palette = [
                "blue", "indigo", "purple", "pink", "red", "orange", "yellow", "green", "teal", "cyan",
                "primary-bg-subtle", "warning-bg-subtle", "success-bg-subtle", "danger-bg-subtle", "info-bg-subtle", "white", "light", "gray", "gray-dark", "black"
            ];

            const rootStyle = getComputedStyle(document.documentElement);
            const varNames = palette.map(x => '--' + x);
            const varValues = varNames.map(x => rootStyle.getPropertyValue(x));

            swatchColors = _.object(varNames.map(x => 'var(' + x + ')'), varValues);
        }

        return swatchColors;
    }

    let defaults = {
        autoInputFallback: false,
        autoHexInputFallback: false,
        swatches: true,
        useAlpha: true,
        format: null,
        horizontal: true,
        fallbackColor: false,
        color: false,
        debug: false,
        slidersHorz: {
            saturation: {
                maxLeft: 200,
                maxTop: 150
            },
            hue: {
                maxLeft: 170,
                maxTop: 0
            },
            alpha: {
                maxLeft: 170,
                maxTop: 0
            }
        },
        extensions: [
            {
                name: 'preview',
                options: { showText: false }
            }
        ]
    };

    const swatchExtension = {
        name: 'swatches',
        options: {
            swatchTemplate: '<span class="colorpicker-swatch"><a href="javascript:;" class="colorpicker-swatch--inner"></a></span>',
            colors: getSwatchColors(),
            namesAsValues: true
        }
    };

    $.fn.colorpickerWrapper = function (options) {
        return this.each(function () {
            let el = $(this);

            if (el.data("colorpicker")) {
                // skip process if element is colorpicker already
                return;
            }

            let opts = $.extend(true, {}, defaults, options, el.data());

            if (opts.swatches) {
                opts.extensions.push(swatchExtension);
            }

            el.colorpicker(opts);

            let colorpicker = el.data("colorpicker");
            let sliderHandler = colorpicker.sliderHandler;

            colorpicker.picker.on('mousedown touchstart', '.colorpicker-guide', function (e) {
                // Fix for "Moving outside the picker makes the guides keep moving because mouseup is not fired".
                colorpicker.picker.off('mousemove.colorpicker touchmove.colorpicker mouseup.colorpicker touchend.colorpicker');

                function released(e) {
                    sliderHandler.released.apply(sliderHandler, e);
                    $(window.document.body).off('mousemove.colorpicker touchmove.colorpicker mouseup.colorpicker touchend.colorpicker');
                }

                $(window.document.body).on({
                    'mousemove.colorpicker': sliderHandler.moved.bind(sliderHandler),
                    'touchmove.colorpicker': sliderHandler.moved.bind(sliderHandler),
                    'mouseup.colorpicker': released,
                    'touchend.colorpicker': released
                });
            });

            // #region deprecated
            //let colorHandler = colorpicker.colorHandler;
            //let colorHandler_getColorString = colorHandler.getColorString;
            //colorHandler.getColorString = function () {
            //    if (!this.hasColor()) {
            //        return '';
            //    }

            //    let format = this.color.alpha == 1 ? "hex" : this.format;
            //    let result = this.color.string(format);

            //    if (format === "hex") {
            //        result = result.toLowerCase();
            //    }

            //    return result;
            //}
            // #endregion
        });
    }

}(jQuery));
