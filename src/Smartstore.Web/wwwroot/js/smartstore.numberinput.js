/*
 * Author: Murat Cakir, Smartstore AG
 */

; (function ($) {
    "use strict"

    var triggerKeyPressed = false

    $.fn.numberInput = function (methodOrProps) {
        if (methodOrProps === "destroy") {
            this.each(function () {
                if (this["number-input"] && this.destroy) {
                    this.destroy();
                }
            });

            return this;
        }

        let g = Smartstore.globalization;
        let culture = g.culture;

        var props = {
            autoDelay: 500, // ms threshold before auto value change
            autoInterval: 50, // speed of auto value change
            autoFocus: true, // whether to focus input on stepper click
            autoSelect: true, // whether to select input value on focusin or stepper click
        }

        for (var option in methodOrProps) {
            // noinspection JSUnfilteredForInLoop
            props[option] = methodOrProps[option]
        }

        this.each(function () {
            if (this["number-input"]) {
                //console.warn("element", this, "is already a number-input");
                return this;
            }

            let $group = $(this).addClass("numberinput-initialized");
            let $input = $group.find(".numberinput");
            let $formatted = $group.find(".numberinput-formatted");

            if ($input.data("step-interval") >>> 0) {
                props.autoInterval = parseFloat($input.data("step-interval"));
            }

            if ($input.data("auto-focus") !== undefined) {
                props.autoFocus = toBool($input.data("auto-focus"));
            }

            if ($input.data("auto-select") !== undefined) {
                props.autoSelect = toBool($input.data("auto-select"));
            }

            this["number-input"] = true;

            var autoDelayHandler = null;
            var autoIntervalHandler = null;

            var min = $input.attr("min");
            var max = $input.attr("max");
            var step = $input.attr("step");
            min = isNaN(min) || min === "" ? -Infinity : parseFloat(min);
            max = isNaN(max) || max === "" ? Infinity : parseFloat(max);
            step = parseFloat(step) || 1;

            var decimals = parseInt($input.data("decimals")) || 0;
            var value = parseFloat($input[0].value);

            updateDisplay(value);

            this.destroy = function() {
                $group.removeClass("numberinput-initialized");
                this["number-input"] = undefined;
                resetTimer();
                $input.off(".ni");
                $group.off(".ni");
                $(document.body).off(".ni");
            }

            $input.on("paste.ni input.ni change.ni focusout.ni", function (e) {
                var newValue = parseValue($input[0].value);
                var focusOut = e.type === "focusout";
                setValue(newValue, focusOut);
                updateDisplay(newValue);
            });

            if (props.autoSelect) {
                $input.on("focusin.ni", function (e) {
                    $input[0].select();
                });
            }

            $group.on("mousedown.ni touchstart.ni", ".numberinput-stepper", function (e) {
                const isMouse = e.type === "mousedown";
                const up = this.matches(".numberinput-up");

                if (isMouse) {
                    if (e.button === 0) {
                        e.preventDefault();
                        onStep(up);
                    }
                }
                else {
                    if (e.cancelable) {
                        e.preventDefault();
                    }

                    onStep(up);
                }
            });

            $(document.body).on("mouseup.ni touchend.ni", function () {
                resetTimer();
            });

            function parseValue(customFormat) {
                return decimals > 0
                    ? parseFloat(customFormat)
                    : parseInt(customFormat);
            }

            function renderValue(number) {
                let minDigits = Math.min(culture.numberFormat.decimals, decimals);
                let numberFormat = new Intl.NumberFormat(culture.name, {
                    minimumFractionDigits: minDigits,
                    maximumFractionDigits: Math.max(minDigits, decimals),
                    useGrouping: true
                });
                return numberFormat.format(number);
            }

            function onStep(up) {
                const isActive = document.activeElement === $input[0];
                if (!isActive) {
                    if (props.autoFocus) {
                        $input[0].focus();
                    }
                    if (props.autoSelect) {
                        setTimeout(() => $input[0].select(), 0);
                    }
                }

                doStep(up);
                resetTimer();
                autoDelayHandler = setTimeout(function () {
                    autoIntervalHandler = setInterval(function () {
                        doStep(up);
                    }, props.autoInterval)
                }, props.autoDelay)

                if (isActive && props.autoSelect) {
                    $input[0].select();
                }      
            }

            function doStep(up) {
                try {
                    // Native stepping
                    if (up)
                        $input[0].stepUp()
                    else
                        $input[0].stepDown();
                }
                catch {
                    // Custom stepping
                    if (isNaN(value)) {
                        value = 0;
                    }

                    const incr = up ? step : -step;

                    setValue(Math.round(value / incr) * incr + incr, true);
                }

                $input.trigger("change");
            }

            function setValue(newValue, updateInput) {
                if (isNaN(newValue) || newValue === "") {
                    if (updateInput) $input[0].value = "";
                    value = NaN;
                }
                else {
                    newValue = parseFloat(newValue);
                    newValue = Math.min(Math.max(newValue, min), max);
                    //newValue = Math.round(newValue * Math.pow(10, decimals)) / Math.pow(10, decimals);
                    newValue = newValue.toFixed(decimals);
                    if (updateInput) $input[0].value = newValue;
                    value = newValue;
                }
            }

            function updateDisplay(newValue) {
                let text = isNaN(newValue) ? "" : renderValue(newValue);
                $formatted.text(text);
            }

            function resetTimer() {
                clearTimeout(autoDelayHandler)
                clearTimeout(autoIntervalHandler)
            }
        })

        return this;
    }

}(jQuery))