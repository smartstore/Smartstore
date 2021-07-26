/* smartstore.system.js
-------------------------------------------------------------- */

(function ($) {

    function detectTouchscreen() {
        let result = false;
        if (window.PointerEvent && ('maxTouchPoints' in navigator)) {
            // if Pointer Events are supported, just check maxTouchPoints
            if (navigator.maxTouchPoints > 0) {
                result = true;
            }
        }
        else {
            // no Pointer Events...
            if (window.matchMedia && window.matchMedia("(any-pointer:coarse)").matches) {
                // check for any-pointer:coarse which mostly means touchscreen
                result = true;
            }
            else if (window.TouchEvent || ('ontouchstart' in window)) {
                // last resort - check for exposed touch events API / event handler
                result = true;
            }
        }

        return result;
    }

    Modernizr.touchevents = detectTouchscreen();

    if (Modernizr.touchevents) {
        window.document.documentElement.classList.remove("no-touchevents");
        window.document.documentElement.classList.add("touchevents");
    }

    // #region String.prototype

    let strProto = String.prototype;
    let rgBlank = /^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g;
    let rgHtmlSpecialChars = /[<>&"'`]/g;

    let unescapeHtmlCharsMap = {
        '<': /(&lt;)|(&#x0*3c;)|(&#0*60;)/gi,
        '>': /(&gt;)|(&#x0*3e;)|(&#0*62;)/gi,
        '&': /(&amp;)|(&#x0*26;)|(&#0*38;)/gi,
        '"': /(&quot;)|(&#x0*22;)|(&#0*34;)/gi,
        "'": /(&#x0*27;)|(&#0*39;)/gi,
        '`': /(&#x0*60;)|(&#0*96;)/gi,
    };
    let escapeHtmlCharsMap = {
        '<': '&lt;',
        '>': '&gt;',
        '&': '&amp;',
        '"': '&quot;',
        "'": '&#x27;',
        '`': '&#x60;',
    };
    let htmlChars = Object.keys(unescapeHtmlCharsMap);

    function reduceUnescapedString(str, key) {
        return str.replace(unescapeHtmlCharsMap[key], key);
    }

    function replaceSpecialChar(char) {
        return escapeHtmlCharsMap[char];
    }

    strProto.trim = strProto.trim || function () {
        return this.replace(rgBlank, '');
    };

    strProto.startsWith = strProto.startsWith || function (searchString, position) {
        position = position || 0;
        return this.indexOf(searchString, position) === position;
    };

    strProto.endsWith = strProto.endsWith || function (searchString, position) {
        var subjectString = this.toString();
        if (typeof position !== 'number' || !isFinite(position) || Math.floor(position) !== position || position > subjectString.length) {
            position = subjectString.length;
        }
        position -= searchString.length;
        var lastIndex = subjectString.indexOf(searchString, position);
        return lastIndex !== -1 && lastIndex === position;
    };

    strProto.isEmpty = function () {
        return this.length === 0 || rgBlank.test(this);
    };

    strProto.hasValue = function () {
        return !rgBlank.test(this);
    };

    strProto.truncate = function (length, end) {
        end = end || "…";
        length = ~~length;
        return this.length > length ? this.substr(0, length - end.length) + end : this;
    };

    strProto.grow = function (val, delimiter) {
        if (val.isEmpty()) {
            return this;
        }

        return (this.hasValue() ? this + delimiter : "") + val;
    };

    strProto.escapeHtml = function () {
        return this.replace(rgHtmlSpecialChars, replaceSpecialChar);
    };

    strProto.unescapeHtml = function () {
        return htmlChars.reduce(reduceUnescapedString, this);
    };

    strProto.format = function () {
        function getType(o) {
            const t = typeof o;

            if (t === "number" || t === "boolean") {
                return t;
            }
            if (o instanceof Date) {
                return "date";
            }
            if (isNaN(o) && !isNaN(Date.parse(o))) {
                return "date";
            }
            return null;
        }

        let g = Smartstore.globalization;
        let s = this, args = arguments;

        for (var i = 0, len = args.length; i < len; i++) {
            let rg = new RegExp("\\{" + i + "(:([^\\}]+))?\\}", "gm");
            let arg = args[i];
            let type = getType(arg), formatter;

            if (g) {
                if (type === "number") {
                    formatter = g.formatNumber;
                }
                else if (type === "date") {
                    formatter = g.formatDate;
                }

                if (formatter) {
                    let match = rg.exec(s);
                    if (match) {
                        arg = formatter(arg, match[2]);
                    }
                }
            }

            s = s.replace(rg, function () {
                return arg;
            });
        }

        return s;
    };

    // #endregion

    // define noop funcs for window.console in order
    // to prevent scripting errors
    var c = window.console = window.console || {};
    var funcs = ['log', 'debug', 'info', 'warn', 'error', 'assert', 'dir', 'dirxml', 'group', 'groupEnd', 'time', 'timeEnd', 'count', 'trace', 'profile', 'profileEnd'],
        flen = funcs.length,
        noop = function () { };
    while (flen) {
        if (!c[funcs[--flen]]) {
            c[funcs[flen]] = noop;
        }
    }

    // define default secure-casts
    jQuery.extend(window, {

        toBool: function (val) {
            var defVal = typeof arguments[1] === "boolean" ? arguments[1] : false;
            var t = typeof val;
            if (t === "boolean") {
                return val;
            }
            else if (t === "string") {
                switch (val.toLowerCase()) {
                    case "1": case "true": case "yes": case "on": case "checked":
                        return true;
                    case "0": case "false": case "no": case "off":
                        return false;
                    default:
                        return defVal;
                }
            }
            else if (t === "number") {
                return Boolean(val);
            }
            else if (t === "null" || t === "undefined") {
                return defVal;
            }
            return defVal;
        },

        toStr: function (val) {
            var defVal = typeof arguments[1] === "string" ? arguments[1] : "";
            if (!val || val === "[NULL]") {
                return defVal;
            }
            return String(val) || defVal;
        },

        toInt: function (val) {
            var x = parseInt(val);
            if (isNaN(x)) {
                var defVal = 0;
                if (arguments.length > 1) {
                    var arg = arguments[1];
                    defVal = arg === null || typeof arg === "number" ? arg : 0;
                }
                return defVal;
            }

            return x;
        },

        toFloat: function (val) {
            var x = parseFloat(val);
            if (isNaN(x)) {
                var defVal = 0;
                if (arguments.length > 1) {
                    var arg = arguments[1];
                    defVal = arg === null || typeof arg === "number" ? arg : 0;
                }
                return defVal;
            }
            return x;
        },

        requestAnimationFrame: window.requestAnimationFrame ||
            window.webkitRequestAnimationFrame ||
            window.mozRequestAnimationFrame ||
            window.msRequestAnimationFrame ||
            window.oRequestAnimationFrame || function (callback) {
                setTimeout(callback, 10);
            },

        cancelAnimationFrame: window.cancelAnimationFrame ||
            window.webkitCancelAnimationFrame ||
            window.mozCancelAnimationFrame ||
            window.msCancelAnimationFrame ||
            window.oCancelAnimationFrame,

        requestIdleCallback: window.requestIdleCallback || function (cb) {
            var start = Date.now();
            return setTimeout(function () {
                cb({
                    didTimeout: false,
                    timeRemaining: function () {
                        return Math.max(0, 50 - (Date.now() - start));
                    },
                });
            }, 1);
        },

        cancelIdleCallback: window.cancelIdleCallback || function (id) { clearTimeout(id); }
    });

    // provide main app namespace
    window.Smartstore = {};
})(jQuery);
