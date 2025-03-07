!function () { function a(a, b) { return a - b * Math.floor(a / b) } function b(a) { return a % 4 === 0 && !(a % 100 === 0 && a % 400 !== 0) } function c(a, c, d) { return l - 1 + 365 * (a - 1) + Math.floor((a - 1) / 4) + -Math.floor((a - 1) / 100) + Math.floor((a - 1) / 400) + Math.floor((367 * c - 362) / 12 + (2 >= c ? 0 : b(a) ? -1 : -2) + d) } function d(d) { var e, f, g, h, i, j, k, m, n, o, p, q; e = Math.floor(d - .5) + .5, f = e - l, g = Math.floor(f / 146097), h = a(f, 146097), i = Math.floor(h / 36524), j = a(h, 36524), k = Math.floor(j / 1461), m = a(j, 1461), n = Math.floor(m / 365), o = 400 * g + 100 * i + 4 * k + n, 4 != i && 4 != n && o++, p = e - c(o, 1, 1), q = e < c(o, 3, 1) ? 0 : b(o) ? 1 : 2; var r = Math.floor((12 * (p + q) + 373) / 367), s = e - c(o, r, 1) + 1; return [o, r, s] } function e(b, c, d) { var e, f; return e = b - (b >= 0 ? 474 : 473), f = 474 + a(e, 2820), d + (7 >= c ? 31 * (c - 1) : 30 * (c - 1) + 6) + Math.floor((682 * f - 110) / 2816) + 365 * (f - 1) + 1029983 * Math.floor(e / 2820) + (m - 1) } function f(b) { var c, d, f, g, h, i, j, k, l, m; return b = Math.floor(b) + .5, g = b - e(475, 1, 1), h = Math.floor(g / 1029983), i = a(g, 1029983), 1029982 == i ? j = 2820 : (k = Math.floor(i / 366), l = a(i, 366), j = Math.floor((2134 * k + 2816 * l + 2815) / 1028522) + k + 1), c = j + 2820 * h + 474, 0 >= c && c--, m = b - e(c, 1, 1) + 1, d = Math.ceil(186 >= m ? m / 31 : (m - 6) / 30), f = b - e(c, d, 1) + 1, [c, d, f] } function g(a) { return a.replace(/[۰-۹]/g, function (a) { return String.fromCharCode(a.charCodeAt(0) - 1728) }) } function h(a) { return 10 > a ? "0" + a : a } function i(a, b, c) { if (b > 12 || 0 >= b) { var d = Math.floor((b - 1) / 12); a += d, b -= 12 * d } return e(a, b, c) } function j(a, b) { var c = /^(\d|\d\d|\d\d\d\d)(?:([-\/])(\d{1,2})(?:\2(\d|\d\d|\d\d\d\d))?)?(([ T])(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d+))?)?(Z|([+-])(\d{2})(?::?(\d{2}))?)?)?$/, e = c.exec(a); if (e) { var f = e[2], g = e[6], h = +e[1], j = +e[3] || 1, k = +e[4] || 1, l = "/" != f && " " != e[6], m = +e[7] || 0, o = +e[8] || 0, p = +e[9] || 0, q = 1e3 * +("0." + (e[10] || "0")), r = e[11], s = l && (r || !e[5]), t = ("-" == e[12] ? -1 : 1) * (60 * (+e[13] || 0) + (+e[14] || 0)); if ((!r && "T" != g || l) && k >= 1e3 != h >= 1e3) { if (k >= 1e3) { if ("-" == f) return; k = +e[1], h = k } if (b) { var u = d(i(h, j, k)); h = u[0], j = u[1], k = u[2] } var v = new n(h, j - 1, k, m, o, p, q); return s && v.setUTCMinutes(v.getUTCMinutes() - v.getTimezoneOffset() + t), v } } } function k(a, b, c, e, f, h, l) { if ("string" == typeof a) { if (convert = void 0 != b ? b : !0, this._d = j(g(a), convert), !this._d) throw "Cannot parse date string" } else if (0 == arguments.length) this._d = new n; else if (1 == arguments.length) this._d = new n(a instanceof k ? a._d : a); else { var m = d(i(a, (b || 0) + 1, c || 1)); this._d = new n(m[0], m[1] - 1, m[2], e || 0, f || 0, h || 0, l || 0) } this._date = this._d, this._cached_date_ts = null, this._cached_date = [0, 0, 0], this._cached_utc_date_ts = null, this._cached_utc_date = [0, 0, 0] } var l = 1721425.5, m = 1948320.5, n = window.Date; k.prototype = { _persianDate: function () { return this._cached_date_ts != +this._d && (this._cached_date_ts = +this._d, this._cached_date = f(c(this._d.getFullYear(), this._d.getMonth() + 1, this._d.getDate()))), this._cached_date }, _persianUTCDate: function () { return this._cached_utc_date_ts != +this._d && (this._cached_utc_date_ts = +this._d, this._cached_utc_date = f(c(this._d.getUTCFullYear(), this._d.getUTCMonth() + 1, this._d.getUTCDate()))), this._cached_utc_date }, _setPersianDate: function (a, b, c) { var e = this._persianDate(); e[a] = b, void 0 !== c && (e[2] = c); var f = d(i(e[0], e[1], e[2])); this._d.setFullYear(f[0]), this._d.setMonth(f[1] - 1, f[2]) }, _setUTCPersianDate: function (a, b, c) { var e = this._persianUTCDate(); void 0 !== c && (e[2] = c), e[a] = b; var f = d(i(e[0], e[1], e[2])); this._d.setUTCFullYear(f[0]), this._d.setUTCMonth(f[1] - 1, f[2]) } }, k.prototype.getDate = function () { return this._persianDate()[2] }, k.prototype.getMonth = function () { return this._persianDate()[1] - 1 }, k.prototype.getFullYear = function () { return this._persianDate()[0] }, k.prototype.getUTCDate = function () { return this._persianUTCDate()[2] }, k.prototype.getUTCMonth = function () { return this._persianUTCDate()[1] - 1 }, k.prototype.getUTCFullYear = function () { return this._persianUTCDate()[0] }, k.prototype.setDate = function (a) { this._setPersianDate(2, a) }, k.prototype.setFullYear = function (a) { this._setPersianDate(0, a) }, k.prototype.setMonth = function (a, b) { this._setPersianDate(1, a + 1, b) }, k.prototype.setUTCDate = function (a) { this._setUTCPersianDate(2, a) }, k.prototype.setUTCFullYear = function (a) { this._setUTCPersianDate(0, a) }, k.prototype.setUTCMonth = function (a, b) { this._setUTCPersianDate(1, a + 1, b) }, k.prototype.toLocaleString = function () { return this.getFullYear() + "/" + h(this.getMonth() + 1) + "/" + h(this.getDate()) + " " + h(this.getHours()) + ":" + h(this.getMinutes()) + ":" + h(this.getSeconds()) }, k.now = n.now, k.parse = function (a) { new k(a).getTime() }, k.UTC = function (a, b, c, e, f, g, h) { var j = d(i(a, b + 1, c)); return n.UTC(j[0], j[1] - 1, j[2], e || 0, f || 0, g || 0, h || 0) }; var o, p = "getHours getMilliseconds getMinutes getSeconds getTime getUTCDay getUTCHours getTimezoneOffset getUTCMilliseconds getUTCMinutes getUTCSeconds setHours setMilliseconds setMinutes setSeconds setTime setUTCHours setUTCMilliseconds setUTCMinutes setUTCSeconds toDateString toISOString toJSON toString toLocaleDateString toLocaleTimeString toTimeString toUTCString valueOf getDay".split(" "), q = function (a) { return function () { return this._d[a].apply(this._d, arguments) } }; for (o = 0; o < p.length; o++)k.prototype[p[o]] = q(p[o]); window.pDate = k }();

//! moment.js locale configuration
//! locale : Persian [fa]
//! author : Ebrahim Byagowi : https://github.com/ebraminio

;(function (global, factory) {
   typeof exports === 'object' && typeof module !== 'undefined'
       && typeof require === 'function' ? factory(require('../moment')) :
   typeof define === 'function' && define.amd ? define(['../moment'], factory) :
   factory(global.moment)
}(this, (function (moment) { 'use strict';


var symbolMap = {
    '1': '۱',
    '2': '۲',
    '3': '۳',
    '4': '۴',
    '5': '۵',
    '6': '۶',
    '7': '۷',
    '8': '۸',
    '9': '۹',
    '0': '۰'
};
var numberMap = {
    '۱': '1',
    '۲': '2',
    '۳': '3',
    '۴': '4',
    '۵': '5',
    '۶': '6',
    '۷': '7',
    '۸': '8',
    '۹': '9',
    '۰': '0'
};

var fa = moment.defineLocale('fa', {
    months : 'ژانویه_فوریه_مارس_آوریل_مه_ژوئن_ژوئیه_اوت_سپتامبر_اکتبر_نوامبر_دسامبر'.split('_'),
    monthsShort : 'ژانویه_فوریه_مارس_آوریل_مه_ژوئن_ژوئیه_اوت_سپتامبر_اکتبر_نوامبر_دسامبر'.split('_'),
    weekdays : 'یک\u200cشنبه_دوشنبه_سه\u200cشنبه_چهارشنبه_پنج\u200cشنبه_جمعه_شنبه'.split('_'),
    weekdaysShort : 'یک\u200cشنبه_دوشنبه_سه\u200cشنبه_چهارشنبه_پنج\u200cشنبه_جمعه_شنبه'.split('_'),
    weekdaysMin : 'ی_د_س_چ_پ_ج_ش'.split('_'),
    weekdaysParseExact : true,
    longDateFormat : {
        LT : 'HH:mm',
        LTS : 'HH:mm:ss',
        L : 'DD/MM/YYYY',
        LL : 'D MMMM YYYY',
        LLL : 'D MMMM YYYY HH:mm',
        LLLL : 'dddd, D MMMM YYYY HH:mm'
    },
    meridiemParse: /قبل از ظهر|بعد از ظهر/,
    isPM: function (input) {
        return /بعد از ظهر/.test(input);
    },
    meridiem : function (hour, minute, isLower) {
        if (hour < 12) {
            return 'قبل از ظهر';
        } else {
            return 'بعد از ظهر';
        }
    },
    calendar : {
        sameDay : '[امروز ساعت] LT',
        nextDay : '[فردا ساعت] LT',
        nextWeek : 'dddd [ساعت] LT',
        lastDay : '[دیروز ساعت] LT',
        lastWeek : 'dddd [پیش] [ساعت] LT',
        sameElse : 'L'
    },
    relativeTime : {
        future : 'در %s',
        past : '%s پیش',
        s : 'چند ثانیه',
        m : 'یک دقیقه',
        mm : '%d دقیقه',
        h : 'یک ساعت',
        hh : '%d ساعت',
        d : 'یک روز',
        dd : '%d روز',
        M : 'یک ماه',
        MM : '%d ماه',
        y : 'یک سال',
        yy : '%d سال'
    },
    preparse: function (string) {
        return string.replace(/[۰-۹]/g, function (match) {
            return numberMap[match];
        }).replace(/،/g, ',');
    },
    postformat: function (string) {
        return string.replace(/\d/g, function (match) {
            return symbolMap[match];
        }).replace(/,/g, '،');
    },
    dayOfMonthOrdinalParse: /\d{1,2}م/,
    ordinal : '%dم',
    week : {
        dow : 6, // Saturday is the first day of the week.
        doy : 12 // The week that contains Jan 1st is the first week of the year.
    }
});

return fa;

})));
