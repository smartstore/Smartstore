/**
 * Original author and copyright: Stefan Haack (https://shaack.com)
 * Repository: https://github.com/shaack/bootstrap-input-spinner
 * License: MIT, see file 'LICENSE'
 * Modified by: Murat Cakir, Smartstore AG
 */

; (function ($) {
    "use strict"

    let g = Smartstore.globalization;

    var RawEditor = function (props, element) {
        this.parse = function (customFormat) {
            let decimals = element.getAttribute("data-decimals") || 0;
            return decimals > 0
                ? parseFloat(customFormat)
                : parseInt(customFormat);
        }
        this.render = function (number) {
            let decimals = parseInt(element.getAttribute("data-decimals")) || 0;
            let numberFormat = new Intl.NumberFormat(g.culture.name, {
                minimumFractionDigits: Math.min(g.culture.numberFormat.decimals, decimals),
                maximumFractionDigits: g.culture.numberFormat.decimals,
                useGrouping: true
            });
            return numberFormat.format(number)
        }
    };

    var triggerKeyPressed = false

    $.fn.numberInput = function (methodOrProps) {
        if (methodOrProps === "destroy") {
            this.each(function () {
                if (this["number-input"]) {
                    this.destroyInputSpinner()
                } else {
                    console.warn("element", this, "is no number-input")
                }
            })
            return this
        }

        var props = {
            decrementButton: "<strong>&minus;</strong>", // button text
            incrementButton: "<strong>&plus;</strong>", // ..
            groupClass: "", // css class of the resulting input-group
            buttonsClass: "btn-outline-secondary",
            buttonsWidth: "2.5rem",
            textAlign: "center", // alignment of the entered number
            autoDelay: 500, // ms threshold before auto value change
            autoInterval: 50, // speed of auto value change
            buttonsOnly: false, // set this `true` to disable the possibility to enter or paste the number via keyboard
            keyboardStepping: true, // set this to `false` to disallow the use of the up and down arrow keys to step
            locale: navigator.language, // the locale, per default detected automatically from the browser
            editor: RawEditor, // the editor (parsing and rendering of the input)
            template: // the template of the input
                '<div class="input-group ${groupClass}">' +
                '<button style="min-width: ${buttonsWidth}" class="btn btn-decrement ${buttonsClass} btn-minus" type="button">${decrementButton}</button>' +
                '<input type="text" inputmode="decimal" style="text-align: ${textAlign}" class="form-control form-control-text-input"/>' +
                '<button style="min-width: ${buttonsWidth}" class="btn btn-increment ${buttonsClass} btn-plus" type="button">${incrementButton}</button>' +
                '</div>'
        }

        for (var option in methodOrProps) {
            // noinspection JSUnfilteredForInLoop
            props[option] = methodOrProps[option]
        }

        var html = props.template
            .replace(/\${groupClass}/g, props.groupClass)
            .replace(/\${buttonsWidth}/g, props.buttonsWidth)
            .replace(/\${buttonsClass}/g, props.buttonsClass)
            .replace(/\${decrementButton}/g, props.decrementButton)
            .replace(/\${incrementButton}/g, props.incrementButton)
            .replace(/\${textAlign}/g, props.textAlign)

        this.each(function () {
            if (this["number-input"]) {
                console.warn("element", this, "is already a number-input")
            }
            else {
                var $group = $(this);
                var $decr = $group.find(".numberinput-down");
                var $incr = $group.find(".numberinput-up");
                var $input = $group.find(".numberinput");
                var $formatted = $group.find(".numberinput-formatted");

                $group[0]["number-input"] = true;
                $group[0].numberInputEditor = new props.editor(props, $input[0]);

                var autoDelayHandler = null
                var autoIntervalHandler = null

                var min = $input.attr("min");
                var max = $input.attr("max");
                var step = $input.attr("step");
                min = isNaN(min) || min === "" ? -Infinity : parseFloat(min);
                max = isNaN(max) || max === "" ? Infinity : parseFloat(max);
                step = parseFloat(step) || 1;

                var value = parseFloat($input[0].value);

                updateDisplay(value);

                $input.on("paste input change focusout", function (e) {
                    var newValue = $group[0].numberInputEditor.parse($input[0].value);
                    var focusOut = e.type === "focusout";
                    setValue(newValue, focusOut);
                    updateDisplay(newValue);
                });

                $input.on("focusin", function (e) {
                    $input[0].select();
                });

                //// --> type=number
                //$input.on("keydown", function (e) {
                //    if (props.keyboardStepping) {
                //        if (e.which === 38) { // up arrow pressed
                //            e.preventDefault();
                //            if (!$decr.prop("disabled")) {
                //                stepHandling(step);
                //            }
                //        } else if (e.which === 40) { // down arrow pressed
                //            e.preventDefault();
                //            if (!$incr.prop("disabled")) {
                //                stepHandling(-step);
                //            }
                //        }
                //    }
                //});

                //// --> type=number
                //$input.on("keyup", function (e) {
                //    // up/down arrow released
                //    if (props.keyboardStepping && (e.which === 38 || e.which === 40)) {
                //        e.preventDefault();
                //        resetTimer();
                //    }
                //});

                //// --> type=number
                //$input.on("keypress", function (e) {
                //    // Allow numbers only
                //    var key = e.keyCode || e.which;
                //    if (key === 8 || key === 45 || key === 46) {
                //        return true;
                //    }
                //    else if (key < 48 || key > 57) {
                //        return false;
                //    }

                //    return true;
                //});

                onPointerDown($decr[0], function () {
                    if (!$decr.prop("disabled")) {
                        doStep(false);
                    }
                })
                onPointerDown($incr[0], function () {
                    if (!$incr.prop("disabled")) {
                        doStep(true);
                    }
                })
                onPointerUp(document.body, function () {
                    resetTimer();
                })
            }

            function doStep(up) {
                const isActive = document.activeElement === $input[0];
                if (!isActive) {
                    $input[0].focus();
                    setTimeout(() => $input[0].select(), 0);
                }

                try {
                    if (up) $input[0].stepUp()
                    else $input[0].stepDown();
                }
                catch {
                    stepHandling(up ? step : -step);
                }

                if (isActive) {
                    $input[0].select();
                }      
            }

            function setValue(newValue, updateInput) {
                if (isNaN(newValue) || newValue === "") {
                    if (updateInput) $input[0].value = "";
                    value = NaN;
                }
                else {
                    let decimals = $input.data("decimals") || 0;

                    newValue = parseFloat(newValue);
                    newValue = Math.min(Math.max(newValue, min), max);
                    //newValue = Math.round(newValue * Math.pow(10, decimals)) / Math.pow(10, decimals);
                    newValue = newValue.toFixed(decimals);
                    if (updateInput) $input[0].value = newValue;
                    value = newValue;
                }
            }

            function updateDisplay(newValue) {
                if (isNaN(newValue)) {
                    $formatted.text("");
                }
                else {
                    $formatted.text($group[0].numberInputEditor.render(newValue));
                }
            }

            function destroy() {
                $original.prop("required", $input.prop("required"));
                observer.disconnect();
                resetTimer();
                $input.off("paste input change focusout");
                $inputGroup.remove();
                $original.show();
                $original[0]["number-input"] = undefined;
                if ($label[0]) {
                    $label.attr("for", $original.attr("id"));
                }
            }

            function dispatchEvent($element, type) {
                if (type) {
                    setTimeout(function () {
                        var event
                        if (typeof (Event) === 'function') {
                            event = new Event(type, { bubbles: true })
                        } else { // IE
                            event = document.createEvent('Event')
                            event.initEvent(type, true, true)
                        }
                        $element[0].dispatchEvent(event)
                    })
                }
            }

            function stepHandling(step) {
                calcStep(step);
                resetTimer();
                autoDelayHandler = setTimeout(function () {
                    autoIntervalHandler = setInterval(function () {
                        calcStep(step);
                    }, props.autoInterval)
                }, props.autoDelay)
            }

            function calcStep(step) {
                if (isNaN(value)) {
                    value = 0;
                }

                setValue(Math.round(value / step) * step + step, true);
                dispatchEvent($input, "change");
            }

            function resetTimer() {
                clearTimeout(autoDelayHandler)
                clearTimeout(autoIntervalHandler)
            }
        })

        return this
    }

    function onPointerUp(el, callback) {
        el?.addEventListener("mouseup", function (e) {
            callback(e)
        })
        el?.addEventListener("touchend", function (e) {
            callback(e)
        })
    }

    function onPointerDown(el, callback) {
        el?.addEventListener("mousedown", function (e) {
            if (e.button === 0) {
                e.preventDefault()
                callback(e)
            }
        })
        el?.addEventListener("touchstart", function (e) {
            if (e.cancelable) {
                e.preventDefault()
            }
            callback(e)
        })
    }

}(jQuery))