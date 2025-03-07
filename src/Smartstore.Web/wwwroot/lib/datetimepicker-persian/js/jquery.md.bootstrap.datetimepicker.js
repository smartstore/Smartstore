/*
* Bootstrap 4+ Persian Date Time Picker jQuery Plugin
* version : 3.11.5
* https://github.com/Mds92/MD.BootstrapPersianDateTimePicker
*
*
* Written By Mohammad Dayyan, Mordad 1397
* mds.soft@gmail.com - @mdssoft
*
* My weblog: mds-soft.persianblog.ir
*/

(function ($) {

    // #region jalali calendar

    function toJalali(gy, gm, gd) {
        return d2j(g2d(gy, gm, gd));
    }

    function toGregorian(jy, jm, jd) {
        return d2g(j2d(jy, jm, jd));
    }

    function isValidJalaliDate(jy, jm, jd) {
        return jy >= -61 && jy <= 3177 &&
            jm >= 1 && jm <= 12 &&
            jd >= 1 && jd <= jalaliMonthLength(jy, jm);
    }

    function isLeapJalaliYear(jy) {
        return jalCal(jy).leap === 0;
    }

    function jalaliMonthLength(jy, jm) {
        if (jm <= 6) return 31;
        if (jm <= 11) return 30;
        if (isLeapJalaliYear(jy)) return 30;
        return 29;
    }

    function jalCal(jy) {
        // Jalali years starting the 33-year rule.
        var breaks = [-61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210, 1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178],
            bl = breaks.length,
            gy = jy + 621,
            leapJ = -14,
            jp = breaks[0],
            jm,
            jump = 1,
            leap,
            n,
            i;

        if (jy < jp || jy >= breaks[bl - 1])
            throw new Error('Invalid Jalali year ' + jy);

        // Find the limiting years for the Jalali year jy.
        for (i = 1; i < bl; i += 1) {
            jm = breaks[i];
            jump = jm - jp;
            if (jy < jm)
                break;
            leapJ = leapJ + div(jump, 33) * 8 + div(mod(jump, 33), 4);
            jp = jm;
        }
        n = jy - jp;

        // Find the number of leap years from AD 621 to the beginning
        // of the current Jalali year in the Persian calendar.
        leapJ = leapJ + div(n, 33) * 8 + div(mod(n, 33) + 3, 4);
        if (mod(jump, 33) === 4 && jump - n === 4)
            leapJ += 1;

        // And the same in the Gregorian calendar (until the year gy).
        var leapG = div(gy, 4) - div((div(gy, 100) + 1) * 3, 4) - 150;

        // Determine the Gregorian date of Farvardin the 1st.
        var march = 20 + leapJ - leapG;

        // Find how many years have passed since the last leap year.
        if (jump - n < 6)
            n = n - jump + div(jump + 4, 33) * 33;
        leap = mod(mod(n + 1, 33) - 1, 4);
        if (leap === -1) leap = 4;

        return {
            leap: leap,
            gy: gy,
            march: march
        };
    }

    function j2d(jy, jm, jd) {
        var r = jalCal(jy);
        return g2d(r.gy, 3, r.march) + (jm - 1) * 31 - div(jm, 7) * (jm - 7) + jd - 1;
    }

    function d2j(jdn) {
        var gy = d2g(jdn).gy, // Calculate Gregorian year (gy).
            jy = gy - 621,
            r = jalCal(jy),
            jdn1F = g2d(gy, 3, r.march),
            jd,
            jm,
            k;

        // Find number of days that passed since 1 Farvardin.
        k = jdn - jdn1F;
        if (k >= 0) {
            if (k <= 185) {
                // The first 6 months.
                jm = 1 + div(k, 31);
                jd = mod(k, 31) + 1;
                return {
                    jy: jy,
                    jm: jm,
                    jd: jd
                };
            } else {
                // The remaining months.
                k -= 186;
            }
        } else {
            // Previous Jalali year.
            jy -= 1;
            k += 179;
            if (r.leap === 1)
                k += 1;
        }
        jm = 7 + div(k, 30);
        jd = mod(k, 30) + 1;
        return {
            jy: jy,
            jm: jm,
            jd: jd
        };
    }

    function g2d(gy, gm, gd) {
        var d = div((gy + div(gm - 8, 6) + 100100) * 1461, 4) +
            div(153 * mod(gm + 9, 12) + 2, 5) +
            gd - 34840408;
        d = d - div(div(gy + 100100 + div(gm - 8, 6), 100) * 3, 4) + 752;
        return d;
    }

    function d2g(jdn) {
        var j;
        j = 4 * jdn + 139361631;
        j = j + div(div(4 * jdn + 183187720, 146097) * 3, 4) * 4 - 3908;
        var i = div(mod(j, 1461), 4) * 5 + 308;
        var gd = div(mod(i, 153), 5) + 1;
        var gm = mod(div(i, 153), 12) + 1;
        var gy = div(j, 1461) - 100100 + div(8 - gm, 6);
        return {
            gy: gy,
            gm: gm,
            gd: gd
        };
    }

    function div(a, b) {
        return ~~(a / b);
    }

    function mod(a, b) {
        return a - ~~(a / b) * b;
    }

    //#endregion jalali calendar

    // #region variables

    var mdDatePickerFlag = 'data-mdpersiandatetimepicker',
        mdDatePickerFlagSelector = '[' + mdDatePickerFlag + ']',
        mdDatePickerGroupIdAttribute = 'data-mdpersiandatetimepicker-group',
        mdDatePickerElementFlag = 'data-mdpersiandatetimepicker-element',
        mdDatePickerElementSelector = '[' + mdDatePickerElementFlag + ']',
        mdDatePickerContainerFlag = 'data-mdpersiandatetimepicker-container',
        mdDatePickerContainerSelector = '[' + mdDatePickerContainerFlag + ']',
        mdPluginName = 'MdPersianDateTimePicker',
        triggerStart = false;

    var modalHtmlTemplate = `
<div class="modal fade mds-bootstrap-persian-datetime-picker-modal" tabindex="-1" role="dialog" 
  aria-labelledby="mdDateTimePickerModalLabel" aria-hidden="true" ${mdDatePickerElementFlag}>
  <div class="modal-dialog modal-xl modal-dialog-centered" data-buttonselector="">
    <div class="modal-content">
      <div class="modal-body" data-name="mds-datetimepicker-body">
        MD DateTimePicker Html
      </div>
    </div>
  </div>
</div>
`;


    var popoverHtmlTemplate = `
<div class="popover mds-bootstrap-persian-datetime-picker-popover" role="tooltip" ${mdDatePickerElementFlag}>    
    <div class="arrow"></div>    
    <h3 class="popover-header text-center" data-name="mds-datetimepicker-title"></h3>    
    <div class="popover-body p-0" data-name="mds-datetimepicker-body"></div>
</div>`;

    var popoverHeaderSelectYearHtmlTemplate = `
<table class="table table-sm table-borderless text-center p-0 m-0 {{rtlCssClass}}">
    <tr>
        <th>            
            <a href="javascript:void(0)" title="{{previousText}}" data-year="{{latestPreviousYear}}" data-yearrangebuttonchange="-1"> &lt; </a>
        </th>
        <th>
            {{yearsRangeText}}
        </th>
        <th>            
            <a href="javascript:void(0)" title="{{nextText}}" data-year="{{latestNextYear}}" data-yearrangebuttonchange="1"> &gt; </a>
        </th>
    </tr>       
</table>`;

    var dateTimePickerYearsToSelectHtmlTemplate = `
<table class="table table-sm text-center p-0 m-0">
    <tbody>
        {{yearsToSelectHtml}}
    </tbody>            
</table>`;

    var dateTimePickerHtmlTemplate = `
<div class="mds-bootstrap-persian-datetime-picker-container {{rtlCssClass}}" ${mdDatePickerContainerFlag}>

	<div class="select-year-inline-box w-0" data-name="dateTimePickerYearsButtonsContainer">        
    </div>
    <div class="select-year-box w-0" data-name="dateTimePickerYearsToSelectContainer">        
    </div>

    <table class="table table-sm text-center p-0 m-0">
        <thead>
            <tr {{selectedDateStringAttribute}}>
                <th colspan="100" data-selecteddatestring>{{selectedDateString}}</th>
            </tr>            
        </thead>
        <tbody>
            <tr>
                {{monthsTdHtml}}
            </tr>
        </tbody>
        <tfoot>
            <tr {{timePickerAttribute}}>
                <td colspan="100" class="border-0">
                    <table class="table table-sm table-borderless">
                        <tbody>
                            <tr>
                                <td>
                                    <input type="text" title="{{hourText}}" value="{{hour}}" maxlength="2" data-clock="hour" />
                                </td>
                                <td>:</td>
                                <td>
                                    <input type="text" title="{{minuteText}}" value="{{minute}}" maxlength="2" data-clock="minute" />
                                </td>
                                <td>:</td>
                                <td>
                                    <input type="text" title="{{secondText}}" value="{{second}}" maxlength="2" data-clock="second" />
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </td>
            </tr>
            <tr>
                <td colspan="100">
                    <button type="button" class="btn btn-light" title="{{goTodayText}}" data-go-today>{{todayDateString}}</button>
                </td>
            </tr>
        </tfoot>
    </table>
</div>`;

    var dateTimePickerMonthTableHtmlTemplate = `
<td class="border-0" style="{{monthTdStyle}}" {{monthTdAttribute}} data-td-month>
	<table class="table table-sm table-striped table-borderless">
		<thead>
			<tr {{monthNameAttribute}}>
				<th colspan="100" class="border-0">
					<table class="table table-sm table-borderless">
						<thead>
							<tr>
								<th>
									<button type="button" class="btn btn-light"> {{currentMonthInfo}} </button>
								</th>
							</tr>
						</thead>
					</table>
				</th>
			</tr>
			<tr {{theadSelectDateButtonTrAttribute}}>
                <td colspan="100" class="border-0">
                    <table class="table table-sm table-borderless">
                        <tr>
                            <th>
                                <button type="button" class="btn btn-light btn-sm" title="{{previousYearText}}" data-changedatebutton data-number="{{previousYearButtonDateNumber}}" {{previousYearButtonDisabledAttribute}}> &lt;&lt; </button>
                            </th>
                            <th>
                                <button type="button" class="btn btn-light btn-sm" title="{{previousMonthText}}" data-changedatebutton data-number="{{previousMonthButtonDateNumber}}" {{previousMonthButtonDisabledAttribute}}> &lt; </button>
                            </th>
                            <th style="width: 120px;">
                                <div class="dropdown">
                                    <button type="button" class="btn btn-light btn-sm dropdown-toggle" id="mdsBootstrapPersianDatetimePickerMonthSelectorButon"
                                        data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                        {{selectedMonthName}}
                                    </button>
                                    <div class="dropdown-menu" aria-labelledby="mdsBootstrapPersianDatetimePickerMonthSelectorButon">
                                        <a class="dropdown-item {{selectMonth1ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth1DateNumber}}">{{monthName1}}</a>
                                        <a class="dropdown-item {{selectMonth2ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth2DateNumber}}">{{monthName2}}</a>
                                        <a class="dropdown-item {{selectMonth3ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth3DateNumber}}">{{monthName3}}</a>
                                        <div class="dropdown-divider"></div>
                                        <a class="dropdown-item {{selectMonth4ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth4DateNumber}}">{{monthName4}}</a>
                                        <a class="dropdown-item {{selectMonth5ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth5DateNumber}}">{{monthName5}}</a>
                                        <a class="dropdown-item {{selectMonth6ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth6DateNumber}}">{{monthName6}}</a>
                                        <div class="dropdown-divider"></div>
                                        <a class="dropdown-item {{selectMonth7ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth7DateNumber}}">{{monthName7}}</a>
                                        <a class="dropdown-item {{selectMonth8ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth8DateNumber}}">{{monthName8}}</a>
                                        <a class="dropdown-item {{selectMonth9ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth9DateNumber}}">{{monthName9}}</a>
                                        <div class="dropdown-divider"></div>
                                        <a class="dropdown-item {{selectMonth10ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth10DateNumber}}">{{monthName10}}</a>
                                        <a class="dropdown-item {{selectMonth11ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth11DateNumber}}">{{monthName11}}</a>
                                        <a class="dropdown-item {{selectMonth12ButtonCssClass}}" data-changedatebutton data-number="{{dropDownMenuMonth12DateNumber}}">{{monthName12}}</a>
                                    </div>
                                </div>
                            </th>
                            <th style="width: 50px;">
                                <button type="button" class="btn btn-light btn-sm" select-year-button {{selectYearButtonDisabledAttribute}}>{{selectedYear}}</button>
                            </th>
                            <th>
                                <button type="button" class="btn btn-light btn-sm" title="{{nextMonthText}}" data-changedatebutton data-number="{{nextMonthButtonDateNumber}}" {{nextMonthButtonDisabledAttribute}}> &gt; </button>
                            </th>
                            <th>
                                <button type="button" class="btn btn-light btn-sm" title="{{nextYearText}}" data-changedatebutton data-number="{{nextYearButtonDateNumber}}" {{nextYearButtonDisabledAttribute}}> &gt;&gt; </button>
                            </th>
                        </tr>
                    </table>
                </td>
			</tr>
		</thead>
		<tbody class="days">
            <tr>
                <td class="{{weekDayShortName1CssClass}}">{{weekDayShortName1}}</td>
                <td>{{weekDayShortName2}}</td>
                <td>{{weekDayShortName3}}</td>
                <td>{{weekDayShortName4}}</td>
                <td>{{weekDayShortName5}}</td>
                <td>{{weekDayShortName6}}</td>
                <td class="{{weekDayShortName7CssClass}}">{{weekDayShortName7}}</td>
            </tr>
        {{daysHtml}}
		</tbody>
	</table>
</td>
    `;

    triggerChangeCalling = false;
    var previousYearTextPersian = 'سال قبل',
        previousMonthTextPersian = 'ماه قبل',
        previousTextPersian = 'قبلی',
        nextYearTextPersian = 'سال بعد',
        nextMonthTextPersian = 'ماه بعد',
        nextTextPersian = 'بعدی',
        hourTextPersian = 'ساعت',
        minuteTextPersian = 'دقیقه',
        secondTextPersian = 'ثانیه',
        goTodayTextPersian = 'برو به امروز',
        previousText = 'Previous',
        previousYearText = 'Previous Year',
        previousMonthText = 'Previous Month',
        nextText = 'Next',
        nextYearText = 'Next Year',
        nextMonthText = 'Next Month',
        goTodayText = 'Go Today',
        hourText = 'Hour',
        minuteText = 'Minute',
        secondText = 'Second',
        amPm = {
            am: 0,
            pm: 1,
            none: 2
        },
        shortDayNamesPersian = [
            'ش',
            'ی',
            'د',
            'س',
            'چ',
            'پ',
            'ج',
        ],
        shortDayNames = [
            'SU',
            'MO',
            'TU',
            'WE',
            'TH',
            'FR',
            'SA',
        ],
        monthNamesPersian = [
            'فروردین',
            'اردیبهشت',
            'خرداد',
            'تیر',
            'مرداد',
            'شهریور',
            'مهر',
            'آبان',
            'آذر',
            'دی',
            'بهمن',
            'اسفند'
        ],
        monthNames = [
            'January',
            'February',
            'March',
            'April',
            'May',
            'June',
            'July',
            'August',
            'September',
            'October',
            'November',
            'December'
        ],
        weekDayNames = [
            'Sunday',
            'Monday',
            'Tuesday',
            'Wednesday',
            'Thursday',
            'Friday',
            'Saturday'
        ],
        weekDayNamesPersian = [
            'یک شنبه',
            'دوشنبه',
            'سه شنبه',
            'چهارشنبه',
            'پنج شنبه',
            'جمعه',
            'شنبه'
        ];

    //#endregion

    // #region Functions

    function newGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    function isWithinMdModal($element) {
        return $element.parents('.modal' + mdDatePickerElementSelector + ':first').length > 0;
    }

    function getPopoverDescriber($element) {
        // المانی را بر میگرداند که کاربر پلاگین را روی آن فعال کرده است
        var $popoverDescriber = $element.parents(mdDatePickerFlagSelector + ':first'); // inline
        // not inline
        if ($popoverDescriber.length <= 0) {
            $popoverDescriber = $element.parents(mdDatePickerElementSelector + ':first');
            $popoverDescriber = $('[aria-describedby="' + $popoverDescriber.attr('id') + '"]');
        }
        return $popoverDescriber;
    }

    function getPopover($popoverDescriber) {
        return $('#' + $popoverDescriber.attr('aria-describedby'));
    }

    function isCalendarOpen($element) {
        // آیا تقویم باز شده است یا خیر
        return $element.attr('aria-describedby') != null;
    }

    function isPopoverDescriber($element) {
        return $element.attr('aria-describedby') != undefined;
    }

    function getSetting1($element) {
        // modal mode
        if (isWithinMdModal($element)) {
            var buttonSelector = $element.parents('[data-buttonselector]:first').attr('data-buttonselector');
            return $('[data-uniqueid="' + buttonSelector + '"]').data(mdPluginName);
        } else {
            return getPopoverDescriber($element).data(mdPluginName);
        }
    }

    function getSetting2($popoverDescriber) {
        return $popoverDescriber.data(mdPluginName);
    }

    function setPopoverHeaderHtml($element, isInLine, htmlString) {
        // $element = المانی که روی آن فعالیتی انجام شده و باید عنوان تقویم آن عوض شود
        if (!isInLine) {
            if (isPopoverDescriber($element)) {
                getPopover($element).find('[data-name="mds-datetimepicker-title"]').html(htmlString);
            } else {
                $element.parents(mdDatePickerElementSelector + ':first').find('[data-name="mds-datetimepicker-title"]').html(htmlString);
            }
        } else {
            var $inlineTitleBox = $element.parents(mdDatePickerFlagSelector + ':first').find('[data-name="dateTimePickerYearsButtonsContainer"]');
            $inlineTitleBox.html(htmlString);
            $inlineTitleBox.removeClass('w-0');
        }
    }

    function setSetting1($element, setting) {
        return getPopoverDescriber($element).data(mdPluginName, setting);
    }

    function updateCalendarHtml1($element, setting) {
        var calendarHtml = getDateTimePickerHtml(setting),
            $container = setting.inLine ?
                $element.parents(mdDatePickerFlagSelector + ':first') :
                $element.parents('[data-name="mds-datetimepicker-body"]:first');
        setPopoverHeaderHtml($element, setting.inLine, $(calendarHtml).find('[data-selecteddatestring]').text().trim());
        $container.html(calendarHtml);
    }

    function getSelectedDateTimeTextFormatted(setting) {
        if (setting.selectedDate == undefined) return '';
        if (setting.rangeSelector && setting.rangeSelectorStartDate != undefined && setting.rangeSelectorEndDate != undefined)
            return getDateTimeString(!setting.isGregorian ? getDateTimeJsonPersian1(setting.rangeSelectorStartDate) : getDateTimeJson1(setting.rangeSelectorStartDate), setting.textFormat, setting.isGregorian, setting.englishNumber) + ' - ' +
                getDateTimeString(!setting.isGregorian ? getDateTimeJsonPersian1(setting.rangeSelectorEndDate) : getDateTimeJson1(setting.rangeSelectorEndDate), setting.textFormat, setting.isGregorian, setting.englishNumber);
        return getDateTimeString(!setting.isGregorian ? getDateTimeJsonPersian1(setting.selectedDate) : getDateTimeJson1(setting.selectedDate), setting.textFormat, setting.isGregorian, setting.englishNumber);
    }

    function getSelectedDateTimeFormatted(setting) {
        if (setting.selectedDate == undefined) return '';
        if (setting.rangeSelector && setting.rangeSelectorStartDate != undefined && setting.rangeSelectorEndDate != undefined)
            return getDateTimeString(getDateTimeJson1(setting.rangeSelectorStartDate), setting.dateFormat, setting.isGregorian, setting.englishNumber) + ' - ' +
                getDateTimeString(getDateTimeJson1(setting.rangeSelectorEndDate), setting.dateFormat, setting.isGregorian, setting.englishNumber);
        return getDateTimeString(getDateTimeJson1(setting.selectedDate), setting.dateFormat, setting.isGregorian, setting.englishNumber);
    }

    function setSelectedData(setting) {
        var $targetText = $(setting.targetTextSelector);
        if ($targetText.length > 0) {
            switch ($targetText[0].tagName.toLowerCase()) {
                case 'input':
                    $targetText.val(getSelectedDateTimeTextFormatted(setting));
                    triggerChangeCalling = true;
                    $targetText.trigger('change');
                    break;
                default:
                    $targetText.text(getSelectedDateTimeTextFormatted(setting));
                    triggerChangeCalling = true;
                    $targetText.trigger('change');
                    break;
            }
        }
        var $targetDate = $(setting.targetDateSelector);
        if ($targetDate.length > 0) {
            switch ($targetDate[0].tagName.toLowerCase()) {
                case 'input':
                    $targetDate.val(toEnglishNumber(getSelectedDateTimeFormatted(setting)));
                    triggerChangeCalling = true;
                    $targetDate.trigger('change');
                    break;
                default:
                    $targetDate.text(toEnglishNumber(getSelectedDateTimeFormatted(setting)));
                    triggerChangeCalling = true;
                    $targetDate.trigger('change');
                    break;
            }
        }
    }

    function setSelectedRangeData(setting) {
        var $targetDate = $(setting.targetTextSelector),
            startDateTimeObject = setting.selectedRangeDate[0],
            endDateTimeObject = setting.selectedRangeDate[1];

        if (!startDateTimeObject)
            throw new Error(`Start Date of '${setting.targetTextSelector}' is not valid for range selector`);
        if (!endDateTimeObject)
            throw new Error(`End Date of '${setting.targetTextSelector}' is not valid for range selector`);

        setting.selectedDate = startDateTimeObject;
        setting.rangeSelectorStartDate = startDateTimeObject;
        setting.rangeSelectorEndDate = endDateTimeObject;

        if ($targetDate.length > 0) {
            switch ($targetDate[0].tagName.toLowerCase()) {
                case 'input':
                    $targetDate.val(getSelectedDateTimeTextFormatted(setting));
                    triggerChangeCalling = true;
                    $targetDate.trigger('change');
                    break;
                default:
                    $targetDate.text(getSelectedDateTimeTextFormatted(setting));
                    triggerChangeCalling = true;
                    $targetDate.trigger('change');
                    break;
            }
        }
    }

    function isNumber(n) {
        return !isNaN(parseFloat(n)) && isFinite(n);
    }

    function toPersianNumber(inputNumber1) {
        /* ۰ ۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹ */
        if (!inputNumber1) return '';
        var str1 = inputNumber1.toString().trim();
        if (!str1) return '';
        str1 = str1.replace(/0/img, '۰');
        str1 = str1.replace(/1/img, '۱');
        str1 = str1.replace(/2/img, '۲');
        str1 = str1.replace(/3/img, '۳');
        str1 = str1.replace(/4/img, '۴');
        str1 = str1.replace(/5/img, '۵');
        str1 = str1.replace(/6/img, '۶');
        str1 = str1.replace(/7/img, '۷');
        str1 = str1.replace(/8/img, '۸');
        str1 = str1.replace(/9/img, '۹');
        return str1;
    }

    function toEnglishNumber(inputNumber2) {
        if (!inputNumber2) return '';
        var str = inputNumber2.toString().trim();
        if (!str) return '';
        str = str.replace(/۰/img, '0');
        str = str.replace(/۱/img, '1');
        str = str.replace(/۲/img, '2');
        str = str.replace(/۳/img, '3');
        str = str.replace(/۴/img, '4');
        str = str.replace(/۵/img, '5');
        str = str.replace(/۶/img, '6');
        str = str.replace(/۷/img, '7');
        str = str.replace(/۸/img, '8');
        str = str.replace(/۹/img, '9');
        return str;
    }

    function getMonthName(monthIndex, isGregorian) {
        if (!isGregorian) return monthNamesPersian[monthIndex];
        return monthNames[monthIndex];
    }

    function addMonthToDateTimeJson(dateTimeJson, addedMonth, isGregorian) {
        // وقتی نیاز هست تا ماه یا روز به تاریخی اضافه کنم
        // پس از اضافه کردن ماه یا روز این متد را استفاده میکنم تا سال و ماه
        // با مقادیر جدید تصحیح و برگشت داده شوند
        var dateTimeJson1 = $.extend({}, dateTimeJson);
        dateTimeJson1.day = 1;
        dateTimeJson1.month += addedMonth;
        if (!isGregorian) {
            if (dateTimeJson1.month <= 0) {
                dateTimeJson1.month = 12;
                dateTimeJson1.year--;
            }
            if (dateTimeJson1.month > 12) {
                dateTimeJson1.year++;
                dateTimeJson1.month = 1;
            }
            return dateTimeJson1;
        }
        return getDateTimeJson1(getDateTime3(dateTimeJson1));
    }

    function addMonthToDateTime(dateTime, addedMonth, isGregorian) {
        var dateTimeJson = {};
        if (!isGregorian) {
            dateTimeJson = getDateTimeJsonPersian1(dateTime);
            dateTimeJson = addMonthToDateTimeJson(dateTimeJson, addedMonth, isGregorian);
            return getDateTime2(dateTimeJson);
        }
        dateTimeJson = getDateTimeJson1(dateTime);
        dateTimeJson = addMonthToDateTimeJson(dateTimeJson, addedMonth, isGregorian);
        return getDateTime3(dateTimeJson);
    }

    function getWeekDayName(englishWeekDayIndex, isGregorian) {
        if (!isGregorian) return weekDayNamesPersian[englishWeekDayIndex];
        return weekDayNames[englishWeekDayIndex];
    }

    function getWeekDayShortName(englishWeekDayIndex, isGregorian) {
        if (!isGregorian) return shortDayNamesPersian[englishWeekDayIndex];
        return shortDayNames[englishWeekDayIndex];
    }

    function getShortHour(hour) {
        var shortHour;
        if (hour > 12)
            shortHour = hour - 12;
        else
            shortHour = hour;
        return shortHour;
    }

    function getAmPm(hour, isGregorian) {
        var amPm;
        if (hour > 12) {
            if (isGregorian)
                amPm = 'PM';
            else
                amPm = 'ب.ظ';
        } else
            if (isGregorian)
                amPm = 'AM';
            else
                amPm = 'ق.ظ';
        return amPm;
    }

    function hideOthers($exceptThis) {
        $(mdDatePickerElementSelector).each(function () {
            var $thisPopover = $(this);
            if (!$exceptThis && $exceptThis.is($thisPopover)) return;
            hidePopover($thisPopover);
        });
    }

    function showPopover($element) {
        if (!$element) return;
        $element.popover('show');
    }

    function hidePopover($element) {
        if (!$element) return;
        $element.popover('hide');
        $element.modal('hide');
    }

    function convertToNumber1(dateTimeJson) {
        return Number(zeroPad(dateTimeJson.year) + zeroPad(dateTimeJson.month) + zeroPad(dateTimeJson.day));
    }

    function convertToNumber2(year, month, day) {
        return Number(zeroPad(year) + zeroPad(month) + zeroPad(day));
    }

    function convertToNumber3(dateTime) {
        return convertToNumber1(getDateTimeJson1(dateTime));
    }

    function convertToNumber4(dateTime) {
        return Number(zeroPad(dateTime.getFullYear()) + zeroPad(dateTime.getMonth()) + zeroPad(dateTime.getDate()));
    }

    function getDateTime1(yearPersian, monthPersian, dayPersian, hour, minute, second) {
        if (!isNumber(hour)) hour = 0;
        if (!isNumber(minute)) minute = 0;
        if (!isNumber(second)) second = 0;
        var gregorian = toGregorian(yearPersian, monthPersian, dayPersian);
        return new Date(gregorian.gy, gregorian.gm - 1, gregorian.gd, hour, minute, second);
    }

    function getDateTime2(dateTimeJsonPersian) {
        if (!dateTimeJsonPersian.hour) dateTimeJsonPersian.hour = 0;
        if (!dateTimeJsonPersian.minute) dateTimeJsonPersian.minute = 0;
        if (!dateTimeJsonPersian.second) dateTimeJsonPersian.second = 0;
        var gregorian = toGregorian(dateTimeJsonPersian.year, dateTimeJsonPersian.month, dateTimeJsonPersian.day);
        return new Date(gregorian.gy, gregorian.gm - 1, gregorian.gd, dateTimeJsonPersian.hour, dateTimeJsonPersian.minute, dateTimeJsonPersian.second);
    }

    function getDateTime3(dateTimeJson) {
        return new Date(dateTimeJson.year, dateTimeJson.month - 1, dateTimeJson.day, dateTimeJson.hour, dateTimeJson.minute, dateTimeJson.second);
    }

    function getDateTime4(dateNumber, dateTime, setting) {
        var dateTimeJson = getDateTimeJson2(dateNumber);
        if (!setting.isGregorian) {
            var dateTimeJsonPersian = getDateTimeJsonPersian1(dateTime);
            dateTimeJsonPersian.year = dateTimeJson.year;
            dateTimeJsonPersian.month = dateTimeJson.month;
            dateTimeJsonPersian.day = dateTimeJson.day;
            dateTime = getDateTime2(dateTimeJsonPersian);
        } else
            dateTime = new Date(dateTimeJson.year, dateTimeJson.month - 1, dateTimeJson.day,
                dateTime.getHours(), dateTime.getMinutes(), dateTime.getSeconds());
        return dateTime;
    }

    function getDateTimeJson1(dateTime) {
        return {
            year: dateTime.getFullYear(),
            month: dateTime.getMonth() + 1,
            day: dateTime.getDate(),
            hour: dateTime.getHours(),
            minute: dateTime.getMinutes(),
            second: dateTime.getSeconds(),
            dayOfWeek: dateTime.getDay()
        };
    }

    function getDateTimeJson2(dateNumber) {
        return {
            year: Math.floor(dateNumber / 10000),
            month: Math.floor(dateNumber / 100) % 100,
            day: dateNumber % 100,
            hour: 0,
            minute: 0,
            second: 0
        };
    }

    function getDateTimeJsonPersian1(dateTime) {
        var persianDate = toJalali(dateTime.getFullYear(), dateTime.getMonth() + 1, dateTime.getDate());
        return {
            year: persianDate.jy,
            month: persianDate.jm,
            day: persianDate.jd,
            hour: dateTime.getHours(),
            minute: dateTime.getMinutes(),
            second: dateTime.getSeconds(),
            dayOfWeek: dateTime.getDay(),
        };
    }

    function getDateTimeJsonPersian2(yearPersian, monthPersian, dayPersian, hour, minute, second) {
        if (!isNumber(hour)) hour = 0;
        if (!isNumber(minute)) minute = 0;
        if (!isNumber(second)) second = 0;
        var gregorian = toGregorian(yearPersian, monthPersian, dayPersian);
        return getDateTimeJsonPersian1(new Date(gregorian.gy, gregorian.gm - 1, gregorian.gd, hour, minute, second));
    }

    function isLeapYear(persianYear) {
        return isLeapJalaliYear(persianYear);
    }

    function getDaysInMonthPersian(year, month) {
        var numberOfDaysInMonth = 31;
        if (month > 6 && month < 12)
            numberOfDaysInMonth = 30;
        else if (month == 12)
            numberOfDaysInMonth = isLeapYear(year) ? 30 : 29;
        return numberOfDaysInMonth;
    }

    function getDaysInMonth(year, month) {
        return new Date(year, month + 1, 0).getDate();
    }

    function getClonedDate(dateTime) {
        return new Date(dateTime.getTime());
    }

    function zeroPad(nr, base) {
        if (nr == undefined || nr == '') return '00';
        if (base == undefined || base == '') base = '00';
        var len = (String(base).length - String(nr).length) + 1;
        return len > 0 ? new Array(len).join('0') + nr : nr;
    }

    function getDateTimeString(dateTimeJson, format, isGregorian, englishNumber) {

        if (isGregorian) englishNumber = true;

        /// فرمت های که پشتیبانی می شوند
        /// <para />
        /// yyyy: سال چهار رقمی
        /// <para />
        /// yy: سال دو رقمی
        /// <para />
        /// MMMM: نام فارسی ماه
        /// <para />
        /// MM: عدد دو رقمی ماه
        /// <para />
        /// M: عدد یک رقمی ماه
        /// <para />
        /// dddd: نام فارسی روز هفته
        /// <para />
        /// dd: عدد دو رقمی روز ماه
        /// <para />
        /// d: عدد یک رقمی روز ماه
        /// <para />
        /// HH: ساعت دو رقمی با فرمت 00 تا 24
        /// <para />
        /// H: ساعت یک رقمی با فرمت 0 تا 24
        /// <para />
        /// hh: ساعت دو رقمی با فرمت 00 تا 12
        /// <para />
        /// h: ساعت یک رقمی با فرمت 0 تا 12
        /// <para />
        /// mm: عدد دو رقمی دقیقه
        /// <para />
        /// m: عدد یک رقمی دقیقه
        /// <para />
        /// ss: ثانیه دو رقمی
        /// <para />
        /// s: ثانیه یک رقمی
        /// <para />
        /// fff: میلی ثانیه 3 رقمی
        /// <para />
        /// ff: میلی ثانیه 2 رقمی
        /// <para />
        /// f: میلی ثانیه یک رقمی
        /// <para />
        /// tt: ب.ظ یا ق.ظ
        /// <para />
        /// t: حرف اول از ب.ظ یا ق.ظ

        format = format.replace(/yyyy/mg, dateTimeJson.year);
        format = format.replace(/yy/mg, dateTimeJson.year % 100);
        format = format.replace(/MMMM/mg, getMonthName(dateTimeJson.month - 1, isGregorian));
        format = format.replace(/MM/mg, zeroPad(dateTimeJson.month));
        format = format.replace(/M/mg, dateTimeJson.month);
        format = format.replace(/dddd/mg, getWeekDayName(dateTimeJson.dayOfWeek, isGregorian));
        format = format.replace(/dd/mg, zeroPad(dateTimeJson.day));
        format = format.replace(/d/mg, dateTimeJson.day);
        format = format.replace(/HH/mg, zeroPad(dateTimeJson.hour));
        format = format.replace(/H/mg, dateTimeJson.hour);
        format = format.replace(/hh/mg, zeroPad(getShortHour(dateTimeJson.hour)));
        format = format.replace(/h/mg, zeroPad(dateTimeJson.hour));
        format = format.replace(/mm/mg, zeroPad(dateTimeJson.minute));
        format = format.replace(/m/mg, dateTimeJson.minute);
        format = format.replace(/ss/mg, zeroPad(dateTimeJson.second));
        format = format.replace(/s/mg, dateTimeJson.second);
        format = format.replace(/fff/mg, zeroPad(dateTimeJson.millisecond, '000'));
        format = format.replace(/ff/mg, zeroPad(dateTimeJson.millisecond / 10));
        format = format.replace(/f/mg, dateTimeJson.millisecond / 100);
        format = format.replace(/tt/mg, getAmPm(dateTimeJson.hour, isGregorian));
        format = format.replace(/t/mg, getAmPm(dateTimeJson.hour, isGregorian)[0]);

        if (!englishNumber) format = toPersianNumber(format);
        return format;
    }

    function getLastDayDateOfPreviousMonth(dateTime, isGregorian) {
        var dateTimeLocal = getClonedDate(dateTime);
        if (isGregorian) {
            var previousMonth = new Date(dateTimeLocal.getFullYear(), dateTimeLocal.getMonth() - 1, 1),
                daysInMonth = getDaysInMonth(previousMonth.getFullYear(), previousMonth.getMonth());
            return new Date(previousMonth.getFullYear(), previousMonth.getMonth(), daysInMonth);
        }
        var dateTimeJsonPersian = getDateTimeJsonPersian1(dateTimeLocal);
        dateTimeJsonPersian.month += -1;
        if (dateTimeJsonPersian.month <= 0) {
            dateTimeJsonPersian.month = 12;
            dateTimeJsonPersian.year--;
        } else if (dateTimeJsonPersian.month > 12) {
            dateTimeJsonPersian.year++;
            dateTimeJsonPersian.month = 1;
        }
        return getDateTime1(dateTimeJsonPersian.year, dateTimeJsonPersian.month, getDaysInMonthPersian(dateTimeJsonPersian.year, dateTimeJsonPersian.month));
    }

    function getFirstDayDateOfNextMonth(dateTime, isGregorian) {
        var dateTimeLocal = getClonedDate(dateTime);
        if (isGregorian) {
            var nextMonth = new Date(dateTimeLocal.getFullYear(), dateTimeLocal.getMonth() + 1, 1);
            return new Date(nextMonth.getFullYear(), nextMonth.getMonth(), 1);
        }
        var dateTimeJsonPersian = getDateTimeJsonPersian1(dateTimeLocal);
        dateTimeJsonPersian.month += 1;
        if (dateTimeJsonPersian.month <= 0) {
            dateTimeJsonPersian.month = 12;
            dateTimeJsonPersian.year--;
        }
        if (dateTimeJsonPersian.month > 12) {
            dateTimeJsonPersian.year++;
            dateTimeJsonPersian.month = 1;
        }
        return getDateTime1(dateTimeJsonPersian.year, dateTimeJsonPersian.month, 1);
    }

    function parsePersianDateTime(persianDateTimeInString, dateSeparatorPattern) {
        if (!dateSeparatorPattern) dateSeparatorPattern = "\/|-";
        dateSeparatorPattern = new RegExp(dateSeparatorPattern, 'img');
        persianDateTimeInString = toEnglishNumber(persianDateTimeInString);

        var month = 0,
            year = 0,
            day = 0,
            hour = 0,
            minute = 0,
            second = 0,
            millisecond = 0,
            amPmEnum = amPm.none,
            containMonthSeparator = dateSeparatorPattern.test(persianDateTimeInString);

        persianDateTimeInString = persianDateTimeInString.replace(/&nbsp;/img, ' ');
        persianDateTimeInString = persianDateTimeInString.replace(/\s+/img, '-');
        persianDateTimeInString = persianDateTimeInString.replace(/\\/img, '-');
        persianDateTimeInString = persianDateTimeInString.replace(/ك/img, 'ک');
        persianDateTimeInString = persianDateTimeInString.replace(/ي/img, 'ی');
        persianDateTimeInString = persianDateTimeInString.replace(dateSeparatorPattern, '-');
        persianDateTimeInString = '-' + persianDateTimeInString + '-';

        // بدست آوردن ب.ظ یا ق.ظ
        if (persianDateTimeInString.indexOf('ق.ظ') > -1)
            amPmEnum = amPm.AM;
        else if (persianDateTimeInString.indexOf('ب.ظ') > -1)
            amPmEnum = amPm.PM;

        if (persianDateTimeInString.indexOf(':') > -1) // رشته ورودی شامل ساعت نیز هست
        {
            persianDateTimeInString = persianDateTimeInString.replace(/-*:-*/img, ':');
            hour = (persianDateTimeInString.match(/-\d{1,2}(?=:)/img)[0]).replace(/\D+/, '');
            var minuteAndSecondAndMillisecondMatch = persianDateTimeInString.match(/:\d{1,2}(?=:?)/img);
            minute = minuteAndSecondAndMillisecondMatch[0].replace(/\D+/, '');
            if (minuteAndSecondAndMillisecondMatch[1] != undefined)
                second = minuteAndSecondAndMillisecondMatch[1].replace(/\D+/, '');
            if (minuteAndSecondAndMillisecondMatch[2] != undefined)
                millisecond = minuteAndSecondAndMillisecondMatch[2].replace(/\D+/, '');
        }

        if (containMonthSeparator) {
            var monthDayMath = persianDateTimeInString.match(/-\d{1,2}(?=-\d{1,2}[^:]|-)/img);

            // بدست آوردن ماه
            month = monthDayMath[0].replace(/\D+/, '');

            // بدست آوردن روز
            day = monthDayMath[1].replace(/\D+/, '');

            // بدست آوردن سال
            year = (persianDateTimeInString.match(/-\d{2,4}(?=-\d{1,2}[^:])/img)[0]).replace(/\D+/, '');
        } else {
            for (var i = 1; i < 12; i++) {
                var persianMonthName = getMonthName(i - 1, false);
                if (persianDateTimeInString.indexOf(persianMonthName) > -1) continue;
                month = i;
                break;
            }

            // بدست آوردن روز
            var dayMatch = persianDateTimeInString.match(/-\d{1,2}(?=-)/img);
            if (dayMatch != null) {
                day = dayMatch[0].replace(/\D+/, '');
                persianDateTimeInString = persianDateTimeInString.replace(new RegExp('-' + day + '(?=-)', 'img'), '-');
            }

            // بدست آوردن سال
            var yearMatch = persianDateTimeInString.match(/-\d{4}(?=-)/img);
            if (yearMatch != null)
                year = yearMatch[0].replace(/\D+/, '');
            else {
                yearMatch = persianDateTimeInString.match(/-\d{2,4}(?=-)/img);
                if (yearMatch != null)
                    year = yearMatch[0].replace(/\D+/, '');
            }
        }

        var numericYear = Number(year);
        var numericMonth = Number(month);
        var numericDay = Number(day);
        var numericHour = Number(hour);
        var numericMinute = Number(minute);
        var numericSecond = Number(second);
        var numericMillisecond = Number(millisecond);

        if (numericYear <= 0)
            numericYear = persianDateTime[0];

        if (numericMonth <= 0)
            numericMonth = persianDateTime[1];

        if (numericDay <= 0)
            numericDay = persianDateTime[2];

        switch (amPmEnum) {
            case amPm.PM:
                if (numericHour < 12)
                    numericHour = numericHour + 12;
                break;
            case amPm.AM:
            case amPm.None:
                break;
        }

        return getDateTime1(numericYear, numericMonth, numericDay, numericHour, numericMinute, numericSecond, numericMillisecond);
    }

    function parseGregorianDateTime(gregorianDateTimeString) {
        //بدست آوردن تاریخ قبلی که در تکست باکس وجود داشته
        gregorianDateTimeString = toEnglishNumber(gregorianDateTimeString);
        if (!gregorianDateTimeString) {
            var dateTime = new Date();
            dateTime.setHours(0);
            dateTime.setMinutes(0);
            dateTime.setSeconds(0);
            dateTime.setMilliseconds(0);
            return dateTime;
        }
        return new Date(gregorianDateTimeString);
    }

    function parseDateTime(value, setting) {
        if (!value) return undefined;
        if (setting.isGregorian) return parseGregorianDateTime(value);
        return parsePersianDateTime(value);
    }

    // Get Html of calendar

    function getYearsToSelectHtml(setting, yearToStart) {
        // بدست آوردن HTML انتخاب سال
        // yearToStart سال شروع

        var selectedDateToShow = getClonedDate(setting.selectedDateToShow),
            html = dateTimePickerYearsToSelectHtmlTemplate;

        var yearsToSelectHtml = '',
            todayDateTimeJson = {}, // year, month, day, hour, minute, second
            selectedDateTimeToShowJson = {},
            disableBeforeDateTimeJson,
            disableAfterDateTimeJson,
            counter = 1;

        if (setting.isGregorian) {
            selectedDateTimeToShowJson = getDateTimeJson1(selectedDateToShow);
            todayDateTimeJson = getDateTimeJson1(new Date());
            disableBeforeDateTimeJson = !setting.disableBeforeDate ? undefined : getDateTimeJson1(setting.disableBeforeDate);
            disableAfterDateTimeJson = !setting.disableAfterDate ? undefined : getDateTimeJson1(setting.disableAfterDate);
        } else {
            selectedDateTimeToShowJson = getDateTimeJsonPersian1(selectedDateToShow);
            todayDateTimeJson = getDateTimeJsonPersian1(new Date());
            disableBeforeDateTimeJson = !setting.disableBeforeDate ? undefined : getDateTimeJsonPersian1(setting.disableBeforeDate);
            disableAfterDateTimeJson = !setting.disableAfterDate ? undefined : getDateTimeJsonPersian1(setting.disableAfterDate);
        }

        // بررسی پراپرتی های از تاریخ، تا تاریخ
        if ((setting.fromDate || setting.toDate) && setting.groupId) {
            var $toDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-toDate]'),
                $fromDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-fromDate]');
            if (setting.fromDate) {
                var toDateSetting = getSetting2($toDateElement),
                    toDateSelectedDate = toDateSetting.selectedDate;
                disableAfterDateTimeJson = !toDateSelectedDate ? undefined : setting.isGregorian ? getDateTimeJson1(toDateSelectedDate) : getDateTimeJsonPersian1(toDateSelectedDate);
            } else if (setting.toDate) {
                var fromDateSetting = getSetting2($fromDateElement),
                    fromDateSelectedDate = fromDateSetting.selectedDate;
                disableBeforeDateTimeJson = !fromDateSelectedDate ? undefined : setting.isGregorian ? getDateTimeJson1(fromDateSelectedDate) : getDateTimeJsonPersian1(fromDateSelectedDate);
            }
        }
        counter = 1;
        var yearStart = yearToStart ? yearToStart : todayDateTimeJson.year - setting.yearOffset;
        var yearEnd = yearToStart ? yearToStart + setting.yearOffset * 2 : todayDateTimeJson.year + setting.yearOffset;
        for (var i = yearStart; i < yearEnd; i++) {
            if (setting.disableBeforeToday && i < todayDateTimeJson.year) continue;
            if (setting.disableAfterToday && i > todayDateTimeJson.year) continue;
            if (disableBeforeDateTimeJson != undefined && disableBeforeDateTimeJson.year != undefined && i < disableBeforeDateTimeJson.year) continue;
            if (disableAfterDateTimeJson != undefined && disableAfterDateTimeJson.year != undefined && i > disableAfterDateTimeJson.year) continue;
            var currentYearDateTimeJson = getDateTimeJson2(convertToNumber2(i, selectedDateTimeToShowJson.month, getDaysInMonthPersian(i, selectedDateTimeToShowJson.month))),
                currentYearDisabledAttr = '',
                yearText = setting.englishNumber ? i.toString() : toPersianNumber(i),
                yearDateNumber = convertToNumber2(i, selectedDateTimeToShowJson.month, 1);
            if (disableBeforeDateTimeJson != undefined && disableBeforeDateTimeJson.year != undefined && currentYearDateTimeJson.year < disableBeforeDateTimeJson.year)
                currentYearDisabledAttr = 'disabled';
            if (disableAfterDateTimeJson != undefined && disableAfterDateTimeJson.year != undefined && currentYearDateTimeJson.year > disableAfterDateTimeJson.year)
                currentYearDisabledAttr = 'disabled';
            if (setting.disableBeforeToday && currentYearDateTimeJson.year < todayDateTimeJson.year)
                currentYearDisabledAttr = 'disabled';
            if (setting.disableAfterToday && currentYearDateTimeJson.year > todayDateTimeJson.year)
                currentYearDisabledAttr = 'disabled';
            if (counter == 1) yearsToSelectHtml += '<tr>';
            yearsToSelectHtml += `
<td class="text-center" ${selectedDateTimeToShowJson.year == i ? 'selected-year' : ''}>
    <button class="btn btn-sm btn-light" type="button" data-changedatebutton data-number="${yearDateNumber}" ${currentYearDisabledAttr}>${yearText}</button>        
</td>
`;
            if (counter == 5) yearsToSelectHtml += '</tr>';
            counter++;
            if (counter > 5) counter = 1;
        }
        html = html.replace(/{{yearsToSelectHtml}}/img, yearsToSelectHtml);
        return {
            yearStart,
            yearEnd,
            html
        };
    }

    function getDateTimePickerHtml(setting) {
        var selectedDateToShow = getClonedDate(setting.selectedDateToShow),
            html = dateTimePickerHtmlTemplate;

        html = html.replace(/{{rtlCssClass}}/img, setting.isGregorian ? '' : 'rtl');
        html = html.replace(/{{selectedDateStringAttribute}}/img, setting.inLine ? '' : 'hidden');
        html = html.replace(/{{hourText}}/img, setting.isGregorian ? hourText : hourTextPersian);
        html = html.replace(/{{minuteText}}/img, setting.isGregorian ? minuteText : minuteTextPersian);
        html = html.replace(/{{secondText}}/img, setting.isGregorian ? secondText : secondTextPersian);
        html = html.replace(/{{goTodayText}}/img, setting.isGregorian ? goTodayText : goTodayTextPersian);
        html = html.replace(/{{timePickerAttribute}}/img, setting.enableTimePicker ? '' : 'hidden');

        var selectedDateString = '',
            todayDateString = '',
            todayDateTimeJson = {}, // year, month, day, hour, minute, second
            rangeSelectorStartDate = !setting.rangeSelector || !setting.rangeSelectorStartDate ? undefined : getClonedDate(setting.rangeSelectorStartDate),
            rangeSelectorEndDate = !setting.rangeSelector || !setting.rangeSelectorEndDate ? undefined : getClonedDate(setting.rangeSelectorEndDate),
            rangeSelectorStartDateJson = {},
            rangeSelectorEndDateJson = {},
            selectedDateTimeJson = {},
            selectedDateTimeToShowJson = {},
            disableBeforeDateTimeJson,
            disableAfterDateTimeJson;

        if (setting.isGregorian) {
            selectedDateTimeToShowJson = getDateTimeJson1(selectedDateToShow);
            todayDateTimeJson = getDateTimeJson1(new Date());
            rangeSelectorStartDateJson = rangeSelectorStartDate != undefined ? getDateTimeJson1(rangeSelectorStartDate) : undefined;
            rangeSelectorEndDateJson = rangeSelectorEndDate != undefined ? getDateTimeJson1(rangeSelectorEndDate) : undefined;
            selectedDateTimeJson = setting.selectedDate == undefined ? todayDateTimeJson : getDateTimeJson1(setting.selectedDate);
            disableBeforeDateTimeJson = !setting.disableBeforeDate ? undefined : getDateTimeJson1(setting.disableBeforeDate);
            disableAfterDateTimeJson = !setting.disableAfterDate ? undefined : getDateTimeJson1(setting.disableAfterDate);
        } else {
            selectedDateTimeToShowJson = getDateTimeJsonPersian1(selectedDateToShow);
            todayDateTimeJson = getDateTimeJsonPersian1(new Date());
            rangeSelectorStartDateJson = rangeSelectorStartDate != undefined ? getDateTimeJsonPersian1(rangeSelectorStartDate) : undefined;
            rangeSelectorEndDateJson = rangeSelectorEndDate != undefined ? getDateTimeJsonPersian1(rangeSelectorEndDate) : undefined;
            selectedDateTimeJson = setting.selectedDate == undefined ? todayDateTimeJson : getDateTimeJsonPersian1(setting.selectedDate);
            disableBeforeDateTimeJson = !setting.disableBeforeDate ? undefined : getDateTimeJsonPersian1(setting.disableBeforeDate);
            disableAfterDateTimeJson = !setting.disableAfterDate ? undefined : getDateTimeJsonPersian1(setting.disableAfterDate);
        }

        // بررسی پراپرتی های از تاریخ، تا تاریخ
        if ((setting.fromDate || setting.toDate) && setting.groupId) {
            var $toDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-toDate]'),
                $fromDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-fromDate]');
            if (setting.fromDate && $toDateElement.length > 0) {
                var toDateSetting = getSetting2($toDateElement),
                    toDateSelectedDate = toDateSetting.selectedDate;
                disableAfterDateTimeJson = !toDateSelectedDate ? undefined : setting.isGregorian ? getDateTimeJson1(toDateSelectedDate) : getDateTimeJsonPersian1(toDateSelectedDate);
            } else if (setting.toDate && $fromDateElement.length > 0) {
                var fromDateSetting = getSetting2($fromDateElement),
                    fromDateSelectedDate = fromDateSetting.selectedDate;
                disableBeforeDateTimeJson = !fromDateSelectedDate ? undefined : setting.isGregorian ? getDateTimeJson1(fromDateSelectedDate) : getDateTimeJsonPersian1(fromDateSelectedDate);
            }
        }

        if (setting.rangeSelector && rangeSelectorStartDateJson != undefined && rangeSelectorEndDateJson != undefined) {
            selectedDateString = `${getWeekDayName(rangeSelectorStartDateJson.dayOfWeek, setting.isGregorian)}، ${rangeSelectorStartDateJson.day} ${getMonthName(rangeSelectorStartDateJson.month - 1, setting.isGregorian)} ${rangeSelectorStartDateJson.year} - 
                ${getWeekDayName(rangeSelectorEndDateJson.dayOfWeek, setting.isGregorian)}، ${rangeSelectorEndDateJson.day} ${getMonthName(rangeSelectorEndDateJson.month - 1, setting.isGregorian)} ${rangeSelectorEndDateJson.year}`;
        } else
            selectedDateString = `${getWeekDayName(selectedDateTimeJson.dayOfWeek, setting.isGregorian)}، ${selectedDateTimeJson.day} ${getMonthName(selectedDateTimeJson.month - 1, setting.isGregorian)} ${selectedDateTimeJson.year}`;
        todayDateString = `${setting.isGregorian ? 'Today,' : 'امروز،'} ${todayDateTimeJson.day} ${getMonthName(todayDateTimeJson.month - 1, setting.isGregorian)} ${todayDateTimeJson.year}`;
        if (!setting.englishNumber) {
            selectedDateString = toPersianNumber(selectedDateString);
            todayDateString = toPersianNumber(todayDateString);
        }

        if (disableAfterDateTimeJson != undefined && disableAfterDateTimeJson.year <= selectedDateTimeToShowJson.year && disableAfterDateTimeJson.month < selectedDateTimeToShowJson.month)
            selectedDateToShow = setting.isGregorian ? new Date(disableAfterDateTimeJson.year, disableAfterDateTimeJson.month - 1, 1) : getDateTime1(disableAfterDateTimeJson.year, disableAfterDateTimeJson.month, disableAfterDateTimeJson.day);

        if (disableBeforeDateTimeJson != undefined && disableBeforeDateTimeJson.year >= selectedDateTimeToShowJson.year && disableBeforeDateTimeJson.month > selectedDateTimeToShowJson.month)
            selectedDateToShow = setting.isGregorian ? new Date(disableBeforeDateTimeJson.year, disableBeforeDateTimeJson.month - 1, 1) : getDateTime1(disableBeforeDateTimeJson.year, disableBeforeDateTimeJson.month, disableBeforeDateTimeJson.day);

        var monthsTdHtml = '',
            numberOfNextMonths = setting.monthsToShow[1] <= 0 ? 0 : setting.monthsToShow[1],
            numberOfPrevMonths = setting.monthsToShow[0] <= 0 ? 0 : setting.monthsToShow[0];
        numberOfPrevMonths *= -1;
        for (var i1 = numberOfPrevMonths; i1 < 0; i1++) {
            setting.selectedDateToShow = addMonthToDateTime(getClonedDate(selectedDateToShow), i1);
            monthsTdHtml += getDateTimePickerMonthHtml1(setting, false, true);
        }
        setting.selectedDateToShow = getClonedDate(selectedDateToShow);
        monthsTdHtml += getDateTimePickerMonthHtml1(setting, false, false);
        for (var i2 = 1; i2 <= numberOfNextMonths; i2++) {
            setting.selectedDateToShow = addMonthToDateTime(getClonedDate(selectedDateToShow), i2);
            monthsTdHtml += getDateTimePickerMonthHtml1(setting, true, false);
        }

        var totalMonthNumberToShow = Math.abs(numberOfPrevMonths) + 1 + numberOfNextMonths,
            monthTdStyle = totalMonthNumberToShow > 1 ? 'width: ' + (100 / totalMonthNumberToShow).toString() + '%;' : '';

        monthsTdHtml = monthsTdHtml.replace(/{{monthTdStyle}}/img, monthTdStyle);

        html = html.replace(/{{selectedDateString}}/img, selectedDateString);
        html = html.replace(/{{todayDateString}}/img, todayDateString);
        html = html.replace(/{{hour}}/img, selectedDateTimeToShowJson.hour);
        html = html.replace(/{{minute}}/img, selectedDateTimeToShowJson.minute);
        html = html.replace(/{{second}}/img, selectedDateTimeToShowJson.second);
        html = html.replace(/{{monthsTdHtml}}/img, monthsTdHtml);

        return html;
    }

    function getDateTimePickerMonthHtml1(setting, isNextMonth, isPrevMonth) {
        var selectedDateToShow = getClonedDate(setting.selectedDateToShow),
            selectedDateToShowTemp = getClonedDate(selectedDateToShow),
            selectedDateTime = setting.selectedDate != undefined ? getClonedDate(setting.selectedDate) : undefined,
            isNextOrPrevMonth = isNextMonth || isPrevMonth,
            html = dateTimePickerMonthTableHtmlTemplate;

        html = html.replace(/{{monthTdAttribute}}/img, isNextMonth ? 'data-next-month' : isPrevMonth ? 'data-prev-month' : '');
        html = html.replace(/{{monthNameAttribute}}/img, !isNextOrPrevMonth ? 'hidden' : '');
        html = html.replace(/{{theadSelectDateButtonTrAttribute}}/img, setting.inLine || !isNextOrPrevMonth ? '' : 'hidden');
        html = html.replace(/{{weekDayShortName1CssClass}}/img, setting.isGregorian ? 'text-danger' : '');
        html = html.replace(/{{weekDayShortName7CssClass}}/img, !setting.isGregorian ? 'text-danger' : '');
        html = html.replace(/{{previousYearText}}/img, setting.isGregorian ? previousYearText : previousYearTextPersian);
        html = html.replace(/{{previousMonthText}}/img, setting.isGregorian ? previousMonthText : previousMonthTextPersian);
        html = html.replace(/{{nextMonthText}}/img, setting.isGregorian ? nextMonthText : nextMonthTextPersian);
        html = html.replace(/{{nextYearText}}/img, setting.isGregorian ? nextYearText : nextYearTextPersian);
        html = html.replace(/{{monthName1}}/img, getMonthName(0, setting.isGregorian));
        html = html.replace(/{{monthName2}}/img, getMonthName(1, setting.isGregorian));
        html = html.replace(/{{monthName3}}/img, getMonthName(2, setting.isGregorian));
        html = html.replace(/{{monthName4}}/img, getMonthName(3, setting.isGregorian));
        html = html.replace(/{{monthName5}}/img, getMonthName(4, setting.isGregorian));
        html = html.replace(/{{monthName6}}/img, getMonthName(5, setting.isGregorian));
        html = html.replace(/{{monthName7}}/img, getMonthName(6, setting.isGregorian));
        html = html.replace(/{{monthName8}}/img, getMonthName(7, setting.isGregorian));
        html = html.replace(/{{monthName9}}/img, getMonthName(8, setting.isGregorian));
        html = html.replace(/{{monthName10}}/img, getMonthName(9, setting.isGregorian));
        html = html.replace(/{{monthName11}}/img, getMonthName(10, setting.isGregorian));
        html = html.replace(/{{monthName12}}/img, getMonthName(11, setting.isGregorian));
        html = html.replace(/{{weekDayShortName1}}/img, getWeekDayShortName(0, setting.isGregorian));
        html = html.replace(/{{weekDayShortName2}}/img, getWeekDayShortName(1, setting.isGregorian));
        html = html.replace(/{{weekDayShortName3}}/img, getWeekDayShortName(2, setting.isGregorian));
        html = html.replace(/{{weekDayShortName4}}/img, getWeekDayShortName(3, setting.isGregorian));
        html = html.replace(/{{weekDayShortName5}}/img, getWeekDayShortName(4, setting.isGregorian));
        html = html.replace(/{{weekDayShortName6}}/img, getWeekDayShortName(5, setting.isGregorian));
        html = html.replace(/{{weekDayShortName7}}/img, getWeekDayShortName(6, setting.isGregorian));

        var i = 0,
            j = 0,
            firstWeekDayNumber,
            cellNumber = 0,
            tdNumber = 0,
            selectedYear = 0,
            selectedDateNumber = 0,
            selectedMonthName = '',
            todayDateNumber,
            todayDateTimeJson = {}, // year, month, day, hour, minute, second
            dateTimeToShowJson = {}, // year, month, day, hour, minute, second
            numberOfDaysInCurrentMonth,
            $tr = $('<tr />'),
            $td = $('<td />'),
            daysHtml = '',
            currentDateNumber = 0,
            currentMonthInfo = '',
            previousMonthDateNumber = 0,
            nextMonthDateNumber = 0,
            previousYearDateNumber = 0,
            nextYearDateNumber = 0,
            disableBeforeDateTimeNumber = 0,
            disableAfterDateTimeNumber = 0,
            rangeSelectorStartDate = !setting.rangeSelector || setting.rangeSelectorStartDate == undefined ? undefined : getClonedDate(setting.rangeSelectorStartDate),
            rangeSelectorEndDate = !setting.rangeSelector || setting.rangeSelectorEndDate == undefined ? undefined : getClonedDate(setting.rangeSelectorEndDate),
            rangeSelectorStartDateNumber = 0,
            rangeSelectorEndDateNumber = 0,
            dayNumberInString = '0',
            dayOfWeek = '', // نام روز هفته
            monthsDateNumberAndAttr = {
                month1DateNumber: 0,
                month2DateNumber: 0,
                month3DateNumber: 0,
                month4DateNumber: 0,
                month5DateNumber: 0,
                month6DateNumber: 0,
                month7DateNumber: 0,
                month8DateNumber: 0,
                month9DateNumber: 0,
                month10DateNumber: 0,
                month11DateNumber: 0,
                month12DateNumber: 0,
                selectMonth1ButtonCssClass: '',
                selectMonth2ButtonCssClass: '',
                selectMonth3ButtonCssClass: '',
                selectMonth4ButtonCssClass: '',
                selectMonth5ButtonCssClass: '',
                selectMonth6ButtonCssClass: '',
                selectMonth7ButtonCssClass: '',
                selectMonth8ButtonCssClass: '',
                selectMonth9ButtonCssClass: '',
                selectMonth10ButtonCssClass: '',
                selectMonth11ButtonCssClass: '',
                selectMonth12ButtonCssClass: '',
            },
            holidaysDateNumbers = [],
            disabledDatesNumber = [],
            specialDatesNumber = [],
            disableBeforeDateTimeJson = {},
            disableAfterDateTimeJson = {},
            previousYearButtonDisabledAttribute = '',
            previousMonthButtonDisabledAttribute = '',
            selectYearButtonDisabledAttribute = '',
            nextMonthButtonDisabledAttribute = '',
            nextYearButtonDisabledAttribute = '';

        if (setting.isGregorian) {
            dateTimeToShowJson = getDateTimeJson1(selectedDateToShowTemp);
            todayDateTimeJson = getDateTimeJson1(new Date());
            disableBeforeDateTimeJson = !setting.disableBeforeDate ? undefined : getDateTimeJson1(setting.disableBeforeDate);
            disableAfterDateTimeJson = !setting.disableAfterDate ? undefined : getDateTimeJson1(setting.disableAfterDate);
            firstWeekDayNumber = new Date(dateTimeToShowJson.year, dateTimeToShowJson.month - 1, 1).getDay();
            selectedDateNumber = !selectedDateTime ? 0 : convertToNumber1(getDateTimeJson1(selectedDateTime));
            numberOfDaysInCurrentMonth = getDaysInMonth(dateTimeToShowJson.year, dateTimeToShowJson.month - 1);
            numberOfDaysInPreviousMonth = getDaysInMonth(dateTimeToShowJson.year, dateTimeToShowJson.month - 2);
            previousMonthDateNumber = convertToNumber1(getDateTimeJson1(getLastDayDateOfPreviousMonth(selectedDateToShowTemp, true)));
            nextMonthDateNumber = convertToNumber1(getDateTimeJson1(getFirstDayDateOfNextMonth(selectedDateToShowTemp, true)));
            selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            previousYearDateNumber = convertToNumber1(getDateTimeJson1(new Date(selectedDateToShowTemp.setFullYear(selectedDateToShowTemp.getFullYear() - 1))));
            selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            nextYearDateNumber = convertToNumber1(getDateTimeJson1(new Date(selectedDateToShowTemp.setFullYear(selectedDateToShowTemp.getFullYear() + 1))));
            selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            rangeSelectorStartDateNumber = !setting.rangeSelector || !rangeSelectorStartDate ? 0 : convertToNumber3(rangeSelectorStartDate);
            rangeSelectorEndDateNumber = !setting.rangeSelector || !rangeSelectorEndDate ? 0 : convertToNumber3(rangeSelectorEndDate);
            for (i = 1; i <= 12; i++) {
                monthsDateNumberAndAttr['month' + i.toString() + 'DateNumber'] = convertToNumber1(getDateTimeJson1(new Date(selectedDateToShowTemp.setMonth(i - 1))));
                selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            }
            for (i = 0; i < setting.holiDays.length; i++) {
                holidaysDateNumbers.push(convertToNumber1(getDateTimeJson1(setting.holiDays[i])));
            }
            for (i = 0; i < setting.disabledDates.length; i++) {
                disabledDatesNumber.push(convertToNumber1(getDateTimeJson1(setting.disabledDates[i])));
            }
            for (i = 0; i < setting.specialDates.length; i++) {
                specialDatesNumber.push(convertToNumber1(getDateTimeJson1(setting.specialDates[i])));
            }
        } else {
            dateTimeToShowJson = getDateTimeJsonPersian1(selectedDateToShowTemp);
            todayDateTimeJson = getDateTimeJsonPersian1(new Date());
            disableBeforeDateTimeJson = !setting.disableBeforeDate ? undefined : getDateTimeJsonPersian1(setting.disableBeforeDate);
            disableAfterDateTimeJson = !setting.disableAfterDate ? undefined : getDateTimeJsonPersian1(setting.disableAfterDate);
            firstWeekDayNumber = getDateTimeJsonPersian2(dateTimeToShowJson.year, dateTimeToShowJson.month, 1, 0, 0, 0).dayOfWeek;
            selectedDateNumber = !selectedDateTime ? 0 : convertToNumber1(getDateTimeJsonPersian1(selectedDateTime));
            numberOfDaysInCurrentMonth = getDaysInMonthPersian(dateTimeToShowJson.year, dateTimeToShowJson.month);
            numberOfDaysInPreviousMonth = getDaysInMonthPersian(dateTimeToShowJson.year - 1, dateTimeToShowJson.month - 1);
            previousMonthDateNumber = convertToNumber1(getDateTimeJsonPersian1(getLastDayDateOfPreviousMonth(selectedDateToShowTemp, false)));
            selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            nextMonthDateNumber = convertToNumber1(getDateTimeJsonPersian1(getFirstDayDateOfNextMonth(selectedDateToShowTemp, false)));
            selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            previousYearDateNumber = convertToNumber2(dateTimeToShowJson.year - 1, dateTimeToShowJson.month, dateTimeToShowJson.day);
            nextYearDateNumber = convertToNumber2(dateTimeToShowJson.year + 1, dateTimeToShowJson.month, dateTimeToShowJson.day);
            selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            rangeSelectorStartDateNumber = !setting.rangeSelector || !rangeSelectorStartDate ? 0 : convertToNumber1(getDateTimeJsonPersian1(rangeSelectorStartDate));
            rangeSelectorEndDateNumber = !setting.rangeSelector || !rangeSelectorEndDate ? 0 : convertToNumber1(getDateTimeJsonPersian1(rangeSelectorEndDate));
            for (i = 1; i <= 12; i++) {
                monthsDateNumberAndAttr['month' + i.toString() + 'DateNumber'] = convertToNumber2(dateTimeToShowJson.year, i, getDaysInMonthPersian(dateTimeToShowJson.year, i));
                selectedDateToShowTemp = getClonedDate(selectedDateToShow);
            }
            for (i = 0; i < setting.holiDays.length; i++) {
                holidaysDateNumbers.push(convertToNumber1(getDateTimeJsonPersian1(setting.holiDays[i])));
            }
            for (i = 0; i < setting.disabledDates.length; i++) {
                disabledDatesNumber.push(convertToNumber1(getDateTimeJsonPersian1(setting.disabledDates[i])));
            }
            for (i = 0; i < setting.specialDates.length; i++) {
                specialDatesNumber.push(convertToNumber1(getDateTimeJsonPersian1(setting.specialDates[i])));
            }
        }

        // بررسی پراپرتی های از تاریخ، تا تاریخ
        if ((setting.fromDate || setting.toDate) && setting.groupId) {
            var $toDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-toDate]'),
                $fromDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-fromDate]');
            if (setting.fromDate && $toDateElement.length > 0) {
                var toDateSetting = getSetting2($toDateElement),
                    toDateSelectedDate = toDateSetting.selectedDate;
                disableAfterDateTimeJson = !toDateSelectedDate ? undefined : setting.isGregorian ? getDateTimeJson1(toDateSelectedDate) : getDateTimeJsonPersian1(toDateSelectedDate);
            } else if (setting.toDate && $fromDateElement.length > 0) {
                var fromDateSetting = getSetting2($fromDateElement),
                    fromDateSelectedDate = fromDateSetting.selectedDate;
                disableBeforeDateTimeJson = !fromDateSelectedDate ? undefined : setting.isGregorian ? getDateTimeJson1(fromDateSelectedDate) : getDateTimeJsonPersian1(fromDateSelectedDate);
            }
        }

        todayDateNumber = convertToNumber1(todayDateTimeJson);
        selectedYear = setting.englishNumber ? dateTimeToShowJson.year : toPersianNumber(dateTimeToShowJson.year);
        disableBeforeDateTimeNumber = !disableBeforeDateTimeJson ? undefined : convertToNumber1(disableBeforeDateTimeJson);
        disableAfterDateTimeNumber = !disableAfterDateTimeJson ? undefined : convertToNumber1(disableAfterDateTimeJson);
        currentMonthInfo = getMonthName(dateTimeToShowJson.month - 1, setting.isGregorian) + ' ' + dateTimeToShowJson.year.toString();
        if (!setting.englishNumber) currentMonthInfo = toPersianNumber(currentMonthInfo);
        selectedMonthName = getMonthName(dateTimeToShowJson.month - 1, setting.isGregorian);

        if (setting.yearOffset <= 0) {
            previousYearButtonDisabledAttribute = 'disabled';
            nextYearButtonDisabledAttribute = 'disabled';
            selectYearButtonDisabledAttribute = 'disabled';
        }

        // روز های ماه قبل
        if (firstWeekDayNumber != 6) {
            if (setting.isGregorian) firstWeekDayNumber--;
            var previousMonthDateTimeJson = addMonthToDateTimeJson(dateTimeToShowJson, -1, setting.isGregorian);
            for (i = numberOfDaysInPreviousMonth - firstWeekDayNumber; i <= numberOfDaysInPreviousMonth; i++) {
                currentDateNumber = convertToNumber2(previousMonthDateTimeJson.year, previousMonthDateTimeJson.month, i);
                dayNumberInString = setting.englishNumber ? zeroPad(i) : toPersianNumber(zeroPad(i));
                $td = $('<td data-nm />')
                    .attr('data-number', currentDateNumber)
                    .html(dayNumberInString);
                if (setting.rangeSelector) {
                    if (currentDateNumber == rangeSelectorStartDateNumber || currentDateNumber == rangeSelectorEndDateNumber)
                        $td.addClass('selected-range-days-start-end');
                    else if (rangeSelectorStartDateNumber > 0 && rangeSelectorEndDateNumber > 0 && currentDateNumber > rangeSelectorStartDateNumber && currentDateNumber < rangeSelectorEndDateNumber)
                        $td.addClass('selected-range-days');
                }
                // روز جمعه
                if (!setting.isGregorian && tdNumber == 6)
                    $td.addClass('text-danger');
                // روز یکشنبه
                else if (setting.isGregorian && tdNumber == 0)
                    $td.addClass('text-danger');
                $tr.append($td);
                cellNumber++;
                tdNumber++;
                if (tdNumber >= 7) {
                    tdNumber = 0;
                    daysHtml += $tr[0].outerHTML;
                    isTrAppended = true;
                    $tr = $('<tr />');
                }
            }
        }

        // روزهای ماه جاری
        for (i = 1; i <= numberOfDaysInCurrentMonth; i++) {

            if (tdNumber >= 7) {
                tdNumber = 0;
                daysHtml += $tr[0].outerHTML;
                isTrAppended = true;
                $tr = $('<tr />');
            }

            // عدد روز
            currentDateNumber = convertToNumber2(dateTimeToShowJson.year, dateTimeToShowJson.month, i);
            dayNumberInString = setting.englishNumber ? zeroPad(i) : toPersianNumber(zeroPad(i));

            $td = $('<td data-day />')
                .attr('data-number', currentDateNumber)
                .html(dayNumberInString);

            // امروز
            if (currentDateNumber == todayDateNumber) {
                $td.attr('data-today', '');
                // اگر نام روز هفته انتخاب شده در تکس باکس قبل از تاریخ امروز باشد
                // نباید دیگر نام روز هفته تغییر کند
                if (!dayOfWeek)
                    dayOfWeek = getWeekDayName(tdNumber - 1 < 0 ? 0 : tdNumber - 1, setting.isGregorian);
            }

            // روز از قبل انتخاب شده
            if (!setting.rangeSelector && selectedDateNumber == currentDateNumber) {
                $td.attr('data-selectedday', '');
                dayOfWeek = getWeekDayName(tdNumber - 1 < 0 ? 0 : tdNumber - 1, setting.isGregorian);
            }

            // روزهای تعطیل
            for (j = 0; j < holidaysDateNumbers.length; j++) {
                if (holidaysDateNumbers[j] != currentDateNumber) continue;
                $td.addClass('text-danger');
                break;
            }

            // روز جمعه شمسی
            if (!setting.isGregorian && tdNumber == 6) {
                $td.addClass('text-danger');
            }
            // روز یکشنبه میلادی
            else if (setting.isGregorian && tdNumber == 0) {
                $td.addClass('text-danger');
            }

            // روزهای غیر فعال شده
            if (setting.disableBeforeToday) {
                if (currentDateNumber < todayDateNumber) $td.attr('disabled', '');
                if (nextMonthDateNumber < todayDateNumber)
                    nextMonthButtonDisabledAttribute = 'disabled';
                if (nextYearDateNumber < todayDateNumber)
                    nextYearButtonDisabledAttribute = 'disabled';
                if (previousMonthDateNumber < todayDateNumber)
                    previousMonthButtonDisabledAttribute = 'disabled';
                if (previousYearDateNumber < todayDateNumber)
                    previousYearButtonDisabledAttribute = 'disabled';
                for (j = 1; j <= 12; j++) {
                    if (monthsDateNumberAndAttr['month' + j.toString() + 'DateNumber'] < todayDateNumber)
                        monthsDateNumberAndAttr['selectMonth' + j.toString() + 'ButtonCssClass'] = 'disabled';
                }
            }
            if (setting.disableAfterToday) {
                if (currentDateNumber > todayDateNumber) $td.attr('disabled', '');
                if (nextMonthDateNumber > todayDateNumber)
                    nextMonthButtonDisabledAttribute = 'disabled';
                if (nextYearDateNumber > todayDateNumber)
                    nextYearButtonDisabledAttribute = 'disabled';
                if (previousMonthDateNumber > todayDateNumber)
                    previousMonthButtonDisabledAttribute = 'disabled';
                if (previousYearDateNumber > todayDateNumber)
                    previousYearButtonDisabledAttribute = 'disabled';
                for (j = 1; j <= 12; j++) {
                    if (monthsDateNumberAndAttr['month' + j.toString() + 'DateNumber'] > todayDateNumber)
                        monthsDateNumberAndAttr['selectMonth' + j.toString() + 'ButtonCssClass'] = 'disabled';
                }
            }
            if (disableAfterDateTimeNumber) {
                if (currentDateNumber > disableAfterDateTimeNumber) $td.attr('disabled', '');
                if (nextMonthDateNumber > disableAfterDateTimeNumber)
                    nextMonthButtonDisabledAttribute = 'disabled';
                if (nextYearDateNumber > disableAfterDateTimeNumber)
                    nextYearButtonDisabledAttribute = 'disabled';
                if (previousMonthDateNumber > disableAfterDateTimeNumber)
                    previousMonthButtonDisabledAttribute = 'disabled';
                if (previousYearDateNumber > disableAfterDateTimeNumber)
                    previousYearButtonDisabledAttribute = 'disabled';
                for (j = 1; j <= 12; j++) {
                    if (monthsDateNumberAndAttr['month' + j.toString() + 'DateNumber'] > disableAfterDateTimeNumber)
                        monthsDateNumberAndAttr['selectMonth' + j.toString() + 'ButtonCssClass'] = 'disabled';
                }
            }
            if (disableBeforeDateTimeNumber) {
                if (currentDateNumber < disableBeforeDateTimeNumber) $td.attr('disabled', '');
                if (nextMonthDateNumber < disableBeforeDateTimeNumber)
                    nextMonthButtonDisabledAttribute = 'disabled';
                if (nextYearDateNumber < disableBeforeDateTimeNumber)
                    nextYearButtonDisabledAttribute = 'disabled';
                if (previousMonthDateNumber < disableBeforeDateTimeNumber)
                    previousMonthButtonDisabledAttribute = 'disabled';
                if (previousYearDateNumber < disableBeforeDateTimeNumber)
                    previousYearButtonDisabledAttribute = 'disabled';
                for (j = 1; j <= 12; j++) {
                    if (monthsDateNumberAndAttr['month' + j.toString() + 'DateNumber'] < disableBeforeDateTimeNumber)
                        monthsDateNumberAndAttr['selectMonth' + j.toString() + 'ButtonCssClass'] = 'disabled';
                }
            }
            for (j = 0; j < disabledDatesNumber.length; j++) {
                if (currentDateNumber == disabledDatesNumber[j])
                    $td.attr('disabled', '');
            }
            for (j = 0; j < specialDatesNumber.length; j++) {
                if (currentDateNumber == specialDatesNumber[j])
                    $td.attr('data-special-date', '');
            }
            if (setting.disabledDays && setting.disabledDays.indexOf(tdNumber) >= 0) {
                $td.attr('disabled', '');
            }
            // \\

            if (setting.rangeSelector) {
                if (currentDateNumber == rangeSelectorStartDateNumber || currentDateNumber == rangeSelectorEndDateNumber)
                    $td.addClass('selected-range-days-start-end');
                else if (rangeSelectorStartDateNumber > 0 && rangeSelectorEndDateNumber > 0 && currentDateNumber > rangeSelectorStartDateNumber && currentDateNumber < rangeSelectorEndDateNumber)
                    $td.addClass('selected-range-days');
            }

            $tr.append($td);
            isTrAppended = false;

            tdNumber++;
            cellNumber++;
        }

        if (tdNumber >= 7) {
            tdNumber = 0;
            daysHtml += $tr[0].outerHTML;
            isTrAppended = true;
            $tr = $('<tr />');
        }

        // روزهای ماه بعد
        var nextMonthDateTimeJson = addMonthToDateTimeJson(dateTimeToShowJson, 1, setting.isGregorian);
        for (i = 1; i <= 42 - cellNumber; i++) {
            dayNumberInString = setting.englishNumber ? zeroPad(i) : toPersianNumber(zeroPad(i));
            currentDateNumber = convertToNumber2(nextMonthDateTimeJson.year, nextMonthDateTimeJson.month, i);
            $td = $('<td data-nm />')
                .attr('data-number', currentDateNumber)
                .html(dayNumberInString);
            if (setting.rangeSelector) {
                if (currentDateNumber == rangeSelectorStartDateNumber || currentDateNumber == rangeSelectorEndDateNumber)
                    $td.addClass('selected-range-days-start-end');
                else if (rangeSelectorStartDateNumber > 0 && rangeSelectorEndDateNumber > 0 && currentDateNumber > rangeSelectorStartDateNumber && currentDateNumber < rangeSelectorEndDateNumber)
                    $td.addClass('selected-range-days');
            }
            // روز جمعه
            if (!setting.isGregorian && tdNumber == 6)
                $td.addClass('text-danger');
            // روز یکشنبه
            else if (setting.isGregorian && tdNumber == 0)
                $td.addClass('text-danger');
            $tr.append($td);
            tdNumber++;
            if (tdNumber >= 7) {
                tdNumber = 0;
                daysHtml += $tr[0].outerHTML;
                isTrAppended = true;
                $tr = $('<tr />');
            }
        }

        if (!isTrAppended) {
            daysHtml += $tr[0].outerHTML;
            isTrAppended = true;
        }

        html = html.replace(/{{currentMonthInfo}}/img, currentMonthInfo);
        html = html.replace(/{{selectedYear}}/img, selectedYear);
        html = html.replace(/{{selectedMonthName}}/img, selectedMonthName);
        html = html.replace(/{{daysHtml}}/img, daysHtml);
        html = html.replace(/{{previousYearButtonDisabledAttribute}}/img, previousYearButtonDisabledAttribute);
        html = html.replace(/{{previousYearButtonDateNumber}}/img, previousYearDateNumber);
        html = html.replace(/{{previousMonthButtonDisabledAttribute}}/img, previousMonthButtonDisabledAttribute);
        html = html.replace(/{{previousMonthButtonDateNumber}}/img, previousMonthDateNumber);
        html = html.replace(/{{selectYearButtonDisabledAttribute}}/img, selectYearButtonDisabledAttribute);
        html = html.replace(/{{nextMonthButtonDisabledAttribute}}/img, nextMonthButtonDisabledAttribute);
        html = html.replace(/{{nextMonthButtonDateNumber}}/img, nextMonthDateNumber);
        html = html.replace(/{{nextYearButtonDisabledAttribute}}/img, nextYearButtonDisabledAttribute);
        html = html.replace(/{{nextYearButtonDateNumber}}/img, nextYearDateNumber);
        html = html.replace(/{{dropDownMenuMonth1DateNumber}}/img, monthsDateNumberAndAttr.month1DateNumber);
        html = html.replace(/{{dropDownMenuMonth2DateNumber}}/img, monthsDateNumberAndAttr.month2DateNumber);
        html = html.replace(/{{dropDownMenuMonth3DateNumber}}/img, monthsDateNumberAndAttr.month3DateNumber);
        html = html.replace(/{{dropDownMenuMonth4DateNumber}}/img, monthsDateNumberAndAttr.month4DateNumber);
        html = html.replace(/{{dropDownMenuMonth5DateNumber}}/img, monthsDateNumberAndAttr.month5DateNumber);
        html = html.replace(/{{dropDownMenuMonth6DateNumber}}/img, monthsDateNumberAndAttr.month6DateNumber);
        html = html.replace(/{{dropDownMenuMonth7DateNumber}}/img, monthsDateNumberAndAttr.month7DateNumber);
        html = html.replace(/{{dropDownMenuMonth8DateNumber}}/img, monthsDateNumberAndAttr.month8DateNumber);
        html = html.replace(/{{dropDownMenuMonth9DateNumber}}/img, monthsDateNumberAndAttr.month9DateNumber);
        html = html.replace(/{{dropDownMenuMonth10DateNumber}}/img, monthsDateNumberAndAttr.month10DateNumber);
        html = html.replace(/{{dropDownMenuMonth11DateNumber}}/img, monthsDateNumberAndAttr.month11DateNumber);
        html = html.replace(/{{dropDownMenuMonth12DateNumber}}/img, monthsDateNumberAndAttr.month12DateNumber);
        html = html.replace(/{{selectMonth1ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth1ButtonCssClass);
        html = html.replace(/{{selectMonth2ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth2ButtonCssClass);
        html = html.replace(/{{selectMonth3ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth3ButtonCssClass);
        html = html.replace(/{{selectMonth4ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth4ButtonCssClass);
        html = html.replace(/{{selectMonth5ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth5ButtonCssClass);
        html = html.replace(/{{selectMonth6ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth6ButtonCssClass);
        html = html.replace(/{{selectMonth7ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth7ButtonCssClass);
        html = html.replace(/{{selectMonth8ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth8ButtonCssClass);
        html = html.replace(/{{selectMonth9ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth9ButtonCssClass);
        html = html.replace(/{{selectMonth10ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth10ButtonCssClass);
        html = html.replace(/{{selectMonth11ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth11ButtonCssClass);
        html = html.replace(/{{selectMonth12ButtonCssClass}}/img, monthsDateNumberAndAttr.selectMonth12ButtonCssClass);

        return html;
    }

    function unbindEvents() {
        $(document).off('click', mdDatePickerContainerSelector + ' [data-day]');
        $(document).off('mouseenter', mdDatePickerContainerSelector + ' [data-day]');
        $(document).off('click', mdDatePickerContainerSelector + ' [data-changedatebutton]');
        $(document).off('blur', mdDatePickerContainerSelector + ' input[data-clock]');
        $(document).off('blur', mdDatePickerContainerSelector + ' input[data-clock]');
        $(document).off('click', mdDatePickerContainerSelector + ' [select-year-button]');
        $(document).off('click', '[data-yearrangebuttonchange]');
        $(document).off('click', mdDatePickerContainerSelector + ' [data-go-today]');
        $(document).off('click', 'html');
    }

    //#endregion

    // #region Events

    // کلیک روی روزها
    $(document).on('click', mdDatePickerContainerSelector + ' [data-day]', function (e) {
        var $this = $(this),
            disabled = $this.attr('disabled'),
            dateNumber = Number($this.attr('data-number')),
            setting = getSetting1($this),
            selectedDateJson = setting.selectedDate == undefined ? undefined : getDateTimeJson1(setting.selectedDate),
            selectedDateToShow = getClonedDate(setting.selectedDateToShow),
            selectedDateToShowJson = selectedDateToShow == undefined ? undefined : getDateTimeJson1(selectedDateToShow);
        if (disabled) {
            if (setting.onDayClick != undefined)
                setting.onDayClick({
                    selectedDate: setting.selectedDate,
                    disabled,
                    event: e,
                    selectedDateToShow,
                    rangeSelectorStartDate: setting.rangeSelectorStartDate,
                    rangeSelectorEndDate: setting.rangeSelectorEndDate,
                });
            return;
        }
        selectedDateToShow = getDateTime4(dateNumber, selectedDateToShow, setting);
        if (setting.rangeSelector) { // اگر رنج سلکتور فعال بود
            if (setting.rangeSelectorStartDate != undefined && setting.rangeSelectorEndDate != undefined) {
                setting.selectedRangeDate = [];
                setting.rangeSelectorStartDate = undefined;
                setting.rangeSelectorEndDate = undefined;
                $this.parents('table:last').find('td.selected-range-days-start-end,td.selected-range-days')
                    .removeClass('selected-range-days')
                    .removeClass('selected-range-days-start-end');
            }
            if (setting.rangeSelectorStartDate == undefined) {
                $this.addClass('selected-range-days-start-end');
                setting.rangeSelectorStartDate = getClonedDate(selectedDateToShow);
                setting.selectedDate = getClonedDate(selectedDateToShow);
                setting.selectedDateToShow = getClonedDate(selectedDateToShow);
            } else if (setting.rangeSelectorStartDate != undefined && setting.rangeSelectorEndDate == undefined) {
                $this.addClass('selected-range-days-start-end');
                setting.rangeSelectorEndDate = getClonedDate(selectedDateToShow);
                setSelectedData(setting);
            }
            setSetting1($this, setting);
            if (setting.rangeSelectorStartDate != undefined && setting.rangeSelectorEndDate != undefined) {
                setting.selectedRangeDate = [getClonedDate(setting.rangeSelectorStartDate), getClonedDate(setting.rangeSelectorEndDate)];
                if (!setting.inLine) {
                    hidePopover($(mdDatePickerElementSelector));
                } else updateCalendarHtml1($this, setting);
            }
            return;
        }
        setting.selectedDate = getClonedDate(selectedDateToShow);
        setting.selectedDateToShow = getClonedDate(selectedDateToShow);
        if (selectedDateJson != undefined) {
            selectedDateJson.hour = selectedDateToShowJson.hour;
            selectedDateJson.minute = selectedDateToShowJson.minute;
            selectedDateJson.second = selectedDateToShowJson.second;
            setting.selectedDate.setHours(selectedDateJson.hour);
            setting.selectedDate.setMinutes(selectedDateJson.minute);
            setting.selectedDate.setSeconds(selectedDateJson.second);
        }
        setSetting1($this, setting);
        setSelectedData(setting);
        if (!setting.inLine) {
            hidePopover($(mdDatePickerElementSelector));
        } else if (setting.inLine && (setting.toDate || setting.fromDate)) {
            // وقتی در حالت این لاین هستیم و ' ار تاریخ ' تا تاریخ ' داریم
            // وقتی روی روز یکی از تقویم ها کلیک می شود
            // باید تقویم دیگر نیز تغییر کند و روزهایی از آن غیر فعال شود
            var $toDateDayElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-toDate]').find('[data-day]:first'),
                $fromDateDayElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-fromDate]').find('[data-day]:first');
            if (setting.fromDate && $toDateDayElement.length > 0) {
                updateCalendarHtml1($toDateDayElement, getSetting1($toDateDayElement));
            } else if (setting.toDate && $fromDateDayElement.length > 0) {
                updateCalendarHtml1($fromDateDayElement, getSetting1($fromDateDayElement));
            }
            updateCalendarHtml1($this, setting);
        } else {
            updateCalendarHtml1($this, setting);
        }
        if (setting.onDayClick != undefined)
            setting.onDayClick({
                rangeSelector: setting.rangeSelector,
                selectedDate: setting.selectedDate,
                disabled,
                event: e,
                selectedDateToShow,
                rangeSelectorStartDate: setting.rangeSelectorStartDate,
                rangeSelectorEndDate: setting.rangeSelectorEndDate,
            });
    });

    // هاور روی روزها
    $(document).on('mouseenter', mdDatePickerContainerSelector + ' [data-day],' + mdDatePickerContainerSelector + ' [data-nm],' + mdDatePickerContainerSelector + ' [data-pm]', function () {
        var $this = $(this),
            $allTdDays = $this.parents('table:last').find('td[data-day]'),
            disabled = $this.attr('disabled'),
            dateNumber = Number($this.attr('data-number')),
            setting = getSetting1($this);
        if (disabled || !setting.rangeSelector || (setting.rangeSelectorStartDate != undefined && setting.rangeSelectorEndDate != undefined)) return;

        $allTdDays.removeClass('selected-range-days');

        var rangeSelectorStartDate = !setting.rangeSelectorStartDate ? undefined : getClonedDate(setting.rangeSelectorStartDate),
            rangeSelectorEndDate = !setting.rangeSelectorEndDate ? undefined : getClonedDate(setting.rangeSelectorEndDate),
            rangeSelectorStartDateNumber = 0,
            rangeSelectorEndDateNumber = 0;

        if (setting.isGregorian) {
            rangeSelectorStartDateNumber = !rangeSelectorStartDate ? 0 : convertToNumber3(rangeSelectorStartDate);
            rangeSelectorEndDateNumber = !rangeSelectorEndDate ? 0 : convertToNumber3(rangeSelectorEndDate);
        } else {
            rangeSelectorStartDateNumber = !rangeSelectorStartDate ? 0 : convertToNumber1(getDateTimeJsonPersian1(rangeSelectorStartDate));
            rangeSelectorEndDateNumber = !rangeSelectorEndDate ? 0 : convertToNumber1(getDateTimeJsonPersian1(rangeSelectorEndDate));
        }

        if (rangeSelectorStartDateNumber > 0 && dateNumber > rangeSelectorStartDateNumber) {
            for (var i1 = rangeSelectorStartDateNumber; i1 <= dateNumber; i1++) {
                $allTdDays.filter('[data-number="' + i1.toString() + '"]:not(.selected-range-days-start-end)').addClass('selected-range-days');
            }
        } else if (rangeSelectorEndDateNumber > 0 && dateNumber < rangeSelectorEndDateNumber) {
            for (var i2 = dateNumber; i2 <= rangeSelectorEndDateNumber; i2++) {
                $allTdDays.filter('[data-number="' + i2.toString() + '"]:not(.selected-range-days-start-end)').addClass('selected-range-days');
            }
        }

    });

    // کلیک روی دکمه هایی که تاریخ را تغییر می دهند
    $(document).on('click', mdDatePickerContainerSelector + ' [data-changedatebutton]', function () {
        var $this = $(this),
            disabled = $this.attr('disabled'),
            dateNumber = Number($this.attr('data-number')),
            setting = getSetting1($this),
            selectedDateToShow = getClonedDate(setting.selectedDateToShow);
        if (disabled) return;
        selectedDateToShow = getDateTime4(dateNumber, selectedDateToShow, setting);
        setting.selectedDateToShow = getClonedDate(selectedDateToShow);
        setSetting1($this, setting);
        updateCalendarHtml1($this, setting);
        if (setting.calendarViewOnChange != undefined)
            setting.calendarViewOnChange(setting.selectedDateToShow);
    });

    // عوض کردن ساعت
    $(document).on('blur', mdDatePickerContainerSelector + ' input[data-clock]', function () {
        var $this = $(this),
            $thisContainer = $this.parents(mdDatePickerContainerSelector + ':first'),
            $hour = $thisContainer.find('input[type="text"][data-clock="hour"]'),
            $minute = $thisContainer.find('input[type="text"][data-clock="minute"]'),
            $second = $thisContainer.find('input[type="text"][data-clock="second"]'),
            hour = Number($hour.val()),
            minute = Number($minute.val()),
            second = Number($second.val()),
            setting = getSetting1($this);

        if (!setting.enableTimePicker) return;

        if (setting.selectedDateToShow == undefined) setting.selectedDateToShow = new Date();
        hour = !isNumber(hour) ? setting.selectedDateToShow.getHours() : hour;
        minute = !isNumber(minute) ? setting.selectedDateToShow.getMinutes() : minute;
        second = !isNumber(second) ? setting.selectedDateToShow.getSeconds() : second;

        setting.selectedDateToShow = new Date(setting.selectedDateToShow.setHours(hour));
        setting.selectedDateToShow = new Date(setting.selectedDateToShow.setMinutes(minute));
        setting.selectedDateToShow = new Date(setting.selectedDateToShow.setSeconds(second));

        if (setting.selectedDate == undefined) setting.selectedDate = new Date();
        setting.selectedDate = new Date(setting.selectedDate.setHours(hour));
        setting.selectedDate = new Date(setting.selectedDate.setMinutes(minute));
        setting.selectedDate = new Date(setting.selectedDate.setSeconds(second));

        setSetting1($this, setting);
        setSelectedData(setting);
    });

    // کلیک روی سال انتخابی برای عوض کردن سال
    $(document).on('click', mdDatePickerContainerSelector + ' [select-year-button]', function () {
        var $this = $(this),
            setting = getSetting1($this),
            yearsToSelectObject = getYearsToSelectHtml(setting),
            yearsRangeText = ` ${yearsToSelectObject.yearStart} - ${yearsToSelectObject.yearEnd} `,
            popoverHeaderHtml = popoverHeaderSelectYearHtmlTemplate,
            dateTimePickerYearsToSelectHtml = yearsToSelectObject.html,
            $mdDatePickerContainerSelector = $this.parents(mdDatePickerContainerSelector + ':first'),
            $dateTimePickerYearsToSelectContainer = $mdDatePickerContainerSelector.find('[data-name="dateTimePickerYearsToSelectContainer"]');
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{rtlCssClass}}/img, setting.isGregorian ? '' : 'rtl');
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{yearsRangeText}}/img, setting.isGregorian || setting.englishNumber ? yearsRangeText : toPersianNumber(yearsRangeText));
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{previousText}}/img, setting.isGregorian ? previousText : previousTextPersian);
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{nextText}}/img, setting.isGregorian ? nextText : nextTextPersian);
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{latestPreviousYear}}/img, yearsToSelectObject.yearStart > yearsToSelectObject.yearEnd ? yearsToSelectObject.yearEnd : yearsToSelectObject.yearStart);
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{latestNextYear}}/img, yearsToSelectObject.yearStart > yearsToSelectObject.yearEnd ? yearsToSelectObject.yearStart : yearsToSelectObject.yearEnd);
        setPopoverHeaderHtml($this, setting.inLine, popoverHeaderHtml);
        $dateTimePickerYearsToSelectContainer.html(dateTimePickerYearsToSelectHtml);
        $dateTimePickerYearsToSelectContainer.removeClass('w-0');
        if (setting.inLine) {
            $dateTimePickerYearsToSelectContainer.addClass('inline');
        } else {
            $dateTimePickerYearsToSelectContainer.removeClass('inline');
        }
    });

    // کلیک روی دکمه های عوض کردن رنج سال انتخابی
    $(document).on('click', '[data-yearrangebuttonchange]', function () {
        var $this = $(this),
            setting = getSetting1($this),
            isNext = $this.attr('data-yearrangebuttonchange') == '1',
            yearStart = Number($this.attr('data-year')),
            yearsToSelectObject = getYearsToSelectHtml(setting, isNext ? yearStart : yearStart - setting.yearOffset * 2),
            yearsRangeText = ` ${yearsToSelectObject.yearStart} - ${yearsToSelectObject.yearEnd - 1} `,
            popoverHeaderHtml = popoverHeaderSelectYearHtmlTemplate,
            dateTimePickerYearsToSelectHtml = yearsToSelectObject.html;
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{rtlCssClass}}/img, setting.isGregorian ? '' : 'rtl');
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{yearsRangeText}}/img, setting.isGregorian ? yearsRangeText : toPersianNumber(yearsRangeText));
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{previousText}}/img, setting.isGregorian ? previousText : previousTextPersian);
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{nextText}}/img, setting.isGregorian ? nextText : nextTextPersian);
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{latestPreviousYear}}/img, yearsToSelectObject.yearStart > yearsToSelectObject.yearEnd ? yearsToSelectObject.yearEnd : yearsToSelectObject.yearStart);
        popoverHeaderHtml = popoverHeaderHtml.replace(/{{latestNextYear}}/img, yearsToSelectObject.yearStart > yearsToSelectObject.yearEnd ? yearsToSelectObject.yearStart : yearsToSelectObject.yearEnd);
        setPopoverHeaderHtml($this, setting.inLine, popoverHeaderHtml);
        $(mdDatePickerContainerSelector).find('[data-name="dateTimePickerYearsToSelectContainer"]').html(dateTimePickerYearsToSelectHtml);
    });

    // برو به امروز
    $(document).on('click', mdDatePickerContainerSelector + ' [data-go-today]', function () {
        var $this = $(this),
            setting = getSetting1($this);
        setting.selectedDateToShow = new Date();
        setSetting1($this, setting);
        updateCalendarHtml1($this, setting);
    });

    // مخفی کردن تقویم با کلیک روی جایی که تقویم نیست
    $('html').on('click', function (e) {
        if (triggerStart) return;
        var $target = $(e.target),
            $popoverDescriber = getPopoverDescriber($target);
        if ($popoverDescriber.length >= 1 || isWithinMdModal($target) || isCalendarOpen($target)) return;
        hidePopover($(mdDatePickerElementSelector));
    });

    //#endregion

    var methods = {
        init: function (options) {
            return this.each(function () {
                var $this = $(this),
                    setting = $.extend({
                        englishNumber: false,
                        placement: 'bottom',
                        trigger: 'click',
                        enableTimePicker: false,
                        targetTextSelector: '',
                        targetDateSelector: '',
                        toDate: false,
                        fromDate: false,
                        groupId: '',
                        disabled: false,
                        textFormat: '',
                        dateFormat: '',
                        isGregorian: false,
                        inLine: false,
                        selectedDate: undefined, // initial value
                        selectedDateToShow: new Date(),
                        monthsToShow: [0, 0],
                        yearOffset: 15,
                        holiDays: [],
                        disabledDates: [],
                        disabledDays: [],
                        specialDates: [],
                        disableBeforeToday: false,
                        disableAfterToday: false,
                        disableBeforeDate: undefined,
                        disableAfterDate: undefined,
                        rangeSelector: false,
                        rangeSelectorStartDate: undefined,
                        rangeSelectorEndDate: undefined,
                        modalMode: false,
                        calendarViewOnChange: () => { },
                        onDayClick: () => { }
                    }, options);
                $this.attr(mdDatePickerFlag, '');
                if (setting.targetDateSelector) {
                    var targetValue = $(setting.targetDateSelector).val();
                    if (targetValue) {
                        setting.selectedDate = new Date(targetValue);
                        setting.selectedDateToShow = getClonedDate(setting.selectedDate);
                    }
                } else if (setting.targetTextSelector) {
                    var textValue = $(setting.targetTextSelector).val();
                    if (textValue) {
                        setting.selectedDate = parseDateTime(textValue, setting);
                        setting.selectedDateToShow = getClonedDate(setting.selectedDate);
                    }
                }
                if (setting.rangeSelector) {
                    setting.fromDate = false;
                    setting.toDate = false;
                    setting.enableTimePicker = false;
                }
                if ((setting.fromDate || setting.toDate) && setting.groupId) {
                    $this.attr(mdDatePickerGroupIdAttribute, setting.groupId);
                    if (setting.toDate) $this.attr('data-toDate', '');
                    else if (setting.fromDate) $this.attr('data-fromDate', '');
                }
                if (setting.isGregorian) setting.englishNumber = true;
                if (setting.toDate && setting.fromDate) throw new Error(`MdPersianDateTimePicker => You can not set true 'toDate' and 'fromDate' together`);
                if (!setting.groupId && (setting.toDate || setting.fromDate)) throw new Error(`MdPersianDateTimePicker => When you set 'toDate' or 'fromDate' true, you have to set 'groupId'`);
                if (setting.disable) $this.attr('disabled', '');
                if (setting.enableTimePicker && !setting.textFormat) setting.textFormat = 'yyyy/MM/dd   HH:mm:ss';
                else if (!setting.enableTimePicker && !setting.textFormat) setting.textFormat = 'yyyy/MM/dd';
                if (setting.enableTimePicker && !setting.dateFormat) setting.dateFormat = 'yyyy/MM/dd   HH:mm:ss';
                else if (!setting.enableTimePicker && !setting.dateFormat) setting.dateFormat = 'yyyy/MM/dd';
                var uniqueId = newGuid();
                $this.data(mdPluginName, setting);
                $this.attr('data-uniqueid', uniqueId);
                if (setting.rangeSelector && setting.selectedRangeDate != undefined) {
                    setSelectedRangeData(setting);
                    triggerChangeCalling = false;
                } else if (setting.selectedDate != undefined) {
                    setSelectedData(setting);
                    triggerChangeCalling = false;
                }
                // نمایش تقویم
                if (setting.inLine) {
                    $this.append(getDateTimePickerHtml(setting));
                } else if (!setting.modalMode) {
                    $this.popover({
                        container: 'body',
                        content: '',
                        html: true,
                        placement: setting.placement,
                        title: ' ',
                        trigger: 'manual',
                        template: popoverHtmlTemplate,
                        sanitize: false,
                    }).on(setting.trigger, function () {
                        triggerStart = true;
                        $this = $(this);
                        setting = $this.data(mdPluginName);
                        if (setting.disabled || isCalendarOpen($this)) {
                            triggerStart = false;
                            return;
                        }
                        hideOthers($this);
                        showPopover($this);
                        setTimeout(function () {
                            setting.selectedDateToShow = setting.selectedDate != undefined ? getClonedDate(setting.selectedDate) : getClonedDate(setting.selectedDateToShow);
                            var calendarHtml = getDateTimePickerHtml(setting);
                            setPopoverHeaderHtml($this, setting.inLine, $(calendarHtml).find('[data-selecteddatestring]').text().trim());
                            getPopover($this).find('[data-name="mds-datetimepicker-body"]').html(calendarHtml);
                            $this.popover('update');
                            triggerStart = false;
                        }, 10);
                    });
                } else if (setting.modalMode) {
                    $('body').append(modalHtmlTemplate);
                    $this.on('click', function () {
                        if (setting.disabled) {
                            return;
                        }
                        setting.selectedDateToShow = setting.selectedDate != undefined ? getClonedDate(setting.selectedDate) : new Date();
                        var calendarHtml = getDateTimePickerHtml(setting);
                        $(mdDatePickerElementSelector).find('[data-name="mds-datetimepicker-body"]').html(calendarHtml);
                        $(mdDatePickerElementSelector).find('[data-buttonselector]').attr('data-buttonselector', uniqueId);
                        $(mdDatePickerElementSelector).modal('show');
                    });
                }
                $(document).on('change', setting.targetTextSelector, function () {
                    if (triggerChangeCalling) {
                        setTimeout(function () {
                            triggerChangeCalling = false;
                        }, 100);
                        return;
                    }
                    var $this1 = $(this),
                        value1 = $this1.val();
                    if (!value1) {
                        $this.MdPersianDateTimePicker('clearDate');
                        return;
                    }
                    try {
                        if (!setting.rangeSelector)
                            $this.MdPersianDateTimePicker('setDate', parseDateTime(value1, setting));
                        else {
                            let dateValues = value1.split(' - ');
                            $this.MdPersianDateTimePicker('setDateRange', parseDateTime(dateValues[0], setting), parseDateTime(dateValues[1], setting));
                        }
                    } catch (e) {
                        setSelectedData(setting);
                    }
                });
            });
        },
        getText: function () {
            var textArray = [];
            this.each(function () {
                textArray.push(getSelectedDateTimeTextFormatted(getSetting2($(this))));
            });
            if (textArray.length > 1) return textArray;
            return textArray[0];
        },
        getDate: function () {
            var dateArray = [];
            this.each(function () {
                dateArray.push(getSetting2($(this)).selectedDate);
            });
            if (dateArray.length > 1) return dateArray;
            return dateArray[0];
        },
        getDateRange: function () {
            var dateRangeArray = [];
            this.each(function () {
                var setting = getSetting2($(this));
                if (setting.rangeSelector) {
                    dateRangeArray.push([setting.rangeSelectorStartDate, setting.rangeSelectorEndDate]);
                    return;
                }
                if (!setting.toDate && !setting.fromDate || !setting.groupId) return [];
                var fromDateSetting = getSetting2($('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-fromDate]')),
                    toDateSetting = getSetting2($('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-toDate]'));
                dateRangeArray.push([fromDateSetting.selectedDate, toDateSetting.selectedDate]);
            });
            if (dateRangeArray.length > 1) return dateRangeArray;
            return dateRangeArray[0];
        },
        setDate: function (dateTimeObject) {
            if (dateTimeObject == undefined) throw new Error('MdPersianDateTimePicker => setDate => مقدار ورودی نا معتبر است');
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                setting.selectedDate = getClonedDate(dateTimeObject);
                setSetting1($this, setting);
                setSelectedData(setting);
            });
        },
        setOption: function (name, value) {
            if (!name) throw new Error('MdPersianDateTimePicker => setOption => name parameter مقدار ورودی نا معتبر است');
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                setting[name] = value;
                setSetting1($this, setting);
            });
        },
        setDateRange: function (startDateTimeObject, endDateTimeObject) {
            if (startDateTimeObject == undefined || endDateTimeObject == undefined) throw new Error('MdPersianDateTimePicker => setDateRange => مقدار ورودی نا معتبر است');
            if (convertToNumber4(startDateTimeObject) > convertToNumber4(endDateTimeObject)) throw new Error('MdPersianDateTimePicker => setDateRange => مقدار ورودی نا معتبر است, تاریخ شروع باید بزرگتر از تاریخ پایان باشد');
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                if (setting.rangeSelector) {
                    setting.selectedDate = startDateTimeObject;
                    setting.selectedRangeDate = [startDateTimeObject, endDateTimeObject];
                    setting.rangeSelectorStartDate = startDateTimeObject;
                    setting.rangeSelectorEndDate = endDateTimeObject;
                    setSetting1($this, setting);
                    setSelectedData(setting);
                } else if ((setting.fromDate || setting.toDate) && setting.groupId) {
                    var $toDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-toDate]'),
                        $fromDateElement = $('[' + mdDatePickerGroupIdAttribute + '="' + setting.groupId + '"][data-fromDate]');
                    if ($fromDateElement.length > 0) {
                        var fromDateSetting = getSetting2($fromDateElement);
                        fromDateSetting.selectedDate = startDateTimeObject;
                        setSetting1($fromDateElement, fromDateSetting);
                        setSelectedData(fromDateSetting);
                    }
                    if ($toDateElement.length > 0) {
                        var toDateSetting = getSetting2($toDateElement);
                        toDateSetting.selectedDate = endDateTimeObject;
                        setSetting1($toDateElement, toDateSetting);
                        setSelectedData(toDateSetting);
                    }
                }
            });
        },
        clearDate: function () {
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                setting.selectedDate = undefined;
                setting.selectedRangeDate = [];
                setting.rangeSelectorStartDate = undefined;
                setting.rangeSelectorEndDate = undefined;
                setSetting1($this, setting);
                setSelectedData(setting);
            });
        },
        setDatePersian: function (dateTimeObjectJson) {
            if (dateTimeObjectJson == undefined) throw new Error('MdPersianDateTimePicker => setDatePersian => ورودی باید از نوه جی سان با حداقل پراپرتی های year, month, day باشد');
            dateTimeObjectJson.hour = !dateTimeObjectJson.hour ? 0 : dateTimeObjectJson.hour;
            dateTimeObjectJson.minute = !dateTimeObjectJson.hour ? 0 : dateTimeObjectJson.minute;
            dateTimeObjectJson.second = !dateTimeObjectJson.second ? 0 : dateTimeObjectJson.second;
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                setting.selectedDate = getDateTime2(dateTimeObjectJson);
                setSetting1($this, setting);
                setSelectedData(setting);
            });
        },
        hide: function () {
            return this.each(function () {
                hidePopover($(this));
            });
        },
        show: function () {
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                $(this).trigger(setting.trigger);
            });
        },
        disable: function (isDisable) {
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                setting.disabled = isDisable;
                setSetting1($this, setting);
                if (isDisable) $this.attr('disabled', '');
                else $this.removeAttr('disabled');
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                if (setting.disable) {
                    $this.removeAttr('disabled');
                }
                if (setting.inLine) {
                    $this.find(mdDatePickerContainerSelector).remove();
                }
                $this.removeAttr(mdDatePickerFlag)
                    .removeAttr('data-toDate')
                    .removeAttr('data-fromDate');
                $this.off(setting.trigger).popover('dispose');
                //if ($(mdDatePickerFlagSelector).length <= 1) {
                // $(document).off('change', setting.targetTextSelector);
                // unbindEvents();
                //}
                $this.removeData(mdPluginName);
            });
        },
        changeType: function (isGregorian, englishNumber) {
            return this.each(function () {
                var $this = $(this),
                    setting = getSetting2($this);
                hidePopover($this);
                setting.isGregorian = isGregorian;
                setting.englishNumber = englishNumber;
                if (setting.isGregorian) setting.englishNumber = true;
                setSetting1($this, setting);
                setSelectedData(setting);
            });
        }
    };

    $.fn.MdPersianDateTimePicker = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist in jquery.Bootstrap-PersianDateTimePicker');
            return false;
        }
    };

})(jQuery);