;
(function ($) {

    $.fn.colorpickerWrapper = function (options) {
        options = options || {};

        return this.each(function () {
            let el = $(this);

            if (el.data("colorpicker")) {
                // skip process if element is colorpicker already
                return;
            }

            el.colorpicker({
                autoInputFallback: false,
                autoHexInputFallback: false,
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
                }
            });

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
