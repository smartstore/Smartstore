/*
 * Smartstore date/time picker, adapted from Tempus Dominus.
 * Copyright Jonathan Peterson, MIT licensed.
 */
(() => {
    'use strict';

    const namespace = window.Smartstore = window.Smartstore || {};
    const instances = new WeakMap();
    const modes = ['days', 'months', 'years', 'decades'];
    const icons = {
        time: 'clock', date: 'calendar', up: 'chevron-up', down: 'chevron-down',
        previous: 'chevron-left', next: 'chevron-right', today: 'calendar-check',
        clear: 'trash', close: 'close'
    };
    const defaults = {
        format: false, locale: null, timeZone: '', extraFormats: false, useStrict: false,
        dayViewHeaderFormat: 'MMMM YYYY', stepping: 1, minDate: false, maxDate: false,
        defaultDate: false, useCurrent: true, keepOpen: false, keepInvalid: false,
        collapse: true, sideBySide: false, calendarWeeks: false, viewMode: 'days',
        toolbarPlacement: 'default', buttons: { showToday: false, showClear: false, showClose: false },
        animation: { duration: 160, easing: 'cubic-bezier(0.3, 0.15, 0.7, 0.85)' },
        widgetPositioning: { horizontal: 'right', vertical: 'auto' }, widgetParent: null,
        inline: false, ignoreReadonly: false, focusOnShow: true, allowInputToggle: false,
        disabledDates: false, enabledDates: false, daysOfWeekDisabled: false,
        disabledHours: false, enabledHours: false, disabledTimeIntervals: false,
        allowMultidate: false, multidateSeparator: ',', debug: false, icons,
        tooltips: {
            today: 'Go to today', clear: 'Clear selection', close: 'Close the picker',
            selectMonth: 'Select Month', prevMonth: 'Previous Month', nextMonth: 'Next Month',
            selectYear: 'Select Year', prevYear: 'Previous Year', nextYear: 'Next Year',
            selectDecade: 'Select Decade', prevDecade: 'Previous Decade', nextDecade: 'Next Decade',
            prevCentury: 'Previous Century', nextCentury: 'Next Century',
            pickHour: 'Pick Hour', incrementHour: 'Increment Hour', decrementHour: 'Decrement Hour',
            pickMinute: 'Pick Minute', incrementMinute: 'Increment Minute', decrementMinute: 'Decrement Minute',
            pickSecond: 'Pick Second', incrementSecond: 'Increment Second', decrementSecond: 'Decrement Second',
            togglePeriod: 'Toggle Period', selectTime: 'Select Time', selectDate: 'Select Date'
        }
    };

    function node(tag, className, text, attributes = {}) {
        const element = document.createElement(tag);
        if (className) element.className = className;
        if (text !== undefined && text !== null) element.textContent = text;
        for (const [key, value] of Object.entries(attributes)) element.setAttribute(key, value);
        return element;
    }

    function merge(options, base = defaults) {
        const result = { ...base, ...options };
        for (const key of ['buttons', 'icons', 'tooltips', 'widgetPositioning']) {
            result[key] = { ...base[key], ...options[key] };
        }
        const animation = options.animation === undefined ? base.animation : options.animation;
        result.animation = animation === false ? false : { ...defaults.animation, ...base.animation, ...animation };
        return result;
    }

    class DateTimePicker {
        constructor(element, options = {}) {
            if (!(element instanceof Element)) throw new TypeError('A date/time picker element is required.');
            if (typeof window.moment !== 'function') throw new Error('The date/time picker requires Moment.');
            if (instances.has(element)) return instances.get(element);

            this.element = element;
            this.input = element.matches('input') ? element : element.querySelector('input');
            if (!this.input) throw new Error('The date/time picker requires an input.');
            this.component = element.querySelector('[data-toggle="datetimepicker"]');
            this.events = new AbortController();
            this.widget = null;
            this._dates = [];
            this.destroyed = false;
            this._options = merge(options);
            this._options.locale ||= moment.locale();
            this._configure();
            this._viewDate = this.getMoment();
            this._emptyDate = this._viewDate.clone();
            this._timeView = 'picker';
            this._showTime = options.viewMode === 'time';
            this._bind();
            instances.set(element, this);
            if (this.input.value.trim()) this._readInput();
            else if (this._options.defaultDate && !this.input.placeholder) this.date(this._options.defaultDate);
            if (this._options.viewDate) this.viewDate(this._options.viewDate);
            if (this._options.inline) this.show();
        }

        static getInstance(element) { return instances.get(element); }

        _configure() {
            const options = this._options;
            if (!moment.locales().includes(options.locale)) throw new TypeError('Unknown date/time picker locale.');
            if (!Number.isFinite(options.stepping) || options.stepping < 1) throw new TypeError('Stepping must be a positive number.');
            if (options.timeZone && !moment.tz) throw new Error('A time zone requires Moment Timezone.');
            const locale = moment.localeData(options.locale);
            this.actualFormat = (options.format || 'L LT').replace(/(\[[^\[]*])|(\\)?(LTS|LT|LL?L?L?|l{1,4})/g,
                token => locale.longDateFormat(token) || token);
            this.parseFormats = [...(options.extraFormats || []), this.actualFormat];
            this.use24Hours = this.actualFormat.toLowerCase().indexOf('a') < 1 && this.actualFormat.replace(/\[.*?]/g, '').indexOf('h') < 1;
            this._minimumMode = this._isEnabled('d') ? 0 : this._isEnabled('M') ? 1 : 2;
            this.currentViewMode = Math.max(this._minimumMode, Math.max(0, modes.indexOf(options.viewMode)));
            for (const key of ['minDate', 'maxDate']) {
                if (options[key]) {
                    options[key] = this._parse(options[key]);
                    if (!options[key].isValid()) throw new TypeError(`${key} is invalid.`);
                }
            }
            if (options.minDate && options.maxDate && options.minDate.isAfter(options.maxDate)) {
                throw new TypeError('minDate must not be after maxDate.');
            }
        }

        _bind() {
            const signal = this.events.signal;
            this.component?.addEventListener('click', event => { event.preventDefault(); event.stopPropagation(); this.toggle(); }, { signal });
            this.input.addEventListener('change', event => { this._readInput(); event.stopPropagation(); }, { signal });
            this.input.addEventListener('keydown', event => this._keydown(event), { signal });
            this.input.addEventListener('blur', () => {
                if (!this._options.debug && !this._options.inline) this.hide();
            }, { signal });
            this.input.addEventListener('focus', () => {
                if (this._options.allowInputToggle) this.show();
            }, { signal });
            document.addEventListener('pointerdown', event => {
                if (!this._options.inline && !this._options.debug && !this.element.contains(event.target) && !this.widget?.contains(event.target)) this.hide();
            }, { signal });
            window.addEventListener('resize', () => this._place(), { signal });
            window.addEventListener('scroll', () => this._place(), { signal, capture: true, passive: true });
        }

        _isEnabled(unit) {
            const format = this.actualFormat.replace(/\[.*?]/g, '');
            return ({ y: /Y/, M: /M/, d: /d/i, h: /h/i, m: /m/, s: /s/ })[unit]?.test(format) || false;
        }

        _hasDate() { return ['y', 'M', 'd'].some(unit => this._isEnabled(unit)); }
        _hasTime() { return ['h', 'm', 's'].some(unit => this._isEnabled(unit)); }

        getMoment(value) {
            let result;
            if (value === undefined) result = moment();
            else if (moment.isMoment(value)) result = value.clone();
            else if (typeof value === 'string') {
                result = value === 'now' ? moment() : moment(value, this.parseFormats, this._options.locale, this._options.useStrict);
            }
            else result = moment(value);
            if (this._options.timeZone) result.tz(this._options.timeZone);
            return result.locale(this._options.locale);
        }

        _parse(value) {
            return this._options.parseInputDate ? this._options.parseInputDate(value).clone() : this.getMoment(value);
        }

        _lastDate() { return (this._dates.at(-1) || this._emptyDate).clone(); }

        _contains(values, value, unit) {
            if (Array.isArray(values)) return values.some(item => unit === 'd' ? this._parse(item).isSame(value, 'd') : Number(item) === value.hour());
            return values[unit === 'd' ? value.format('YYYY-MM-DD') : value.format('H')] === true;
        }

        _isValid(value, unit) {
            const options = this._options;
            if (!value?.isValid()) return false;
            if (options.minDate && value.isBefore(options.minDate, unit)) return false;
            if (options.maxDate && value.isAfter(options.maxDate, unit)) return false;
            if (unit === 'd') {
                if (options.disabledDates && this._contains(options.disabledDates, value, 'd')) return false;
                if (options.enabledDates && !this._contains(options.enabledDates, value, 'd')) return false;
                if (options.daysOfWeekDisabled && options.daysOfWeekDisabled.includes(value.day())) return false;
            }
            if (['h', 'm', 's'].includes(unit)) {
                if (options.disabledHours && this._contains(options.disabledHours, value, 'h')) return false;
                if (options.enabledHours && !this._contains(options.enabledHours, value, 'h')) return false;
                if (options.disabledTimeIntervals && options.disabledTimeIntervals.some(([start, end]) => value.isBetween(this._parse(start), this._parse(end)))) return false;
            }
            return true;
        }

        _emit(type, data = {}) {
            this.element.dispatchEvent(new CustomEvent(`smartdatetimepicker:${type}`, { bubbles: true, detail: data }));
        }

        date(value, index = 0) {
            if (arguments.length === 0) return this._dates[0]?.clone() || null;
            const oldDate = this._dates[index]?.clone() || null;
            if (value === null || value === false || value === '') {
                this._dates.splice(index, 1);
                if (!this._dates.length) {
                    this._viewDate = this.getMoment();
                    this._emptyDate = this._viewDate.clone();
                }
                this._writeInput();
                this._update();
                if (oldDate) this._emit('change', { date: false, oldDate });
                return this;
            }
            const candidate = this._parse(value);
            if (this._options.stepping !== 1 && candidate.isValid()) {
                candidate.minutes(Math.round(candidate.minutes() / this._options.stepping) * this._options.stepping).seconds(0);
            }
            if (!this._isValid(candidate)) {
                if (!this._options.keepInvalid) this._writeInput();
                else this._emit('change', { date: candidate.clone(), oldDate });
                this._emit('error', { date: candidate, oldDate });
                return this;
            }
            this._dates[index] = candidate;
            this._viewDate = candidate.clone();
            this._writeInput();
            this._update();
            if (!oldDate || !candidate.isSame(oldDate)) this._emit('change', { date: candidate.clone(), oldDate });
            return this;
        }

        _writeInput() {
            this.input.value = this._dates.map(value => value.format(this.actualFormat)).join(this._options.multidateSeparator);
            this.element.dataset.date = this.input.value;
            this.input.dispatchEvent(new Event('input', { bubbles: true }));
        }

        _readInput() {
            const text = this.input.value.trim();
            if (!text) { this.clear(); return; }
            if (!this._options.allowMultidate) { this.date(text); return; }
            const values = text.split(this._options.multidateSeparator).map(value => this._parse(value.trim()));
            if (values.every(value => this._isValid(value))) {
                const oldDate = this.date();
                this._dates = values;
                this._viewDate = this._lastDate();
                this._writeInput();
                this._update();
                this._emit('change', { date: this._lastDate(), oldDate });
            }
            else {
                if (!this._options.keepInvalid) this._writeInput();
                this._emit('error', { date: values.find(value => !this._isValid(value)), oldDate: this.date() });
            }
        }

        clear() {
            const oldDate = this.date();
            this._dates = [];
            this._viewDate = this.getMoment();
            this._emptyDate = this._viewDate.clone();
            this._writeInput();
            this._update();
            if (oldDate) this._emit('change', { date: false, oldDate });
            return this;
        }

        options(value) {
            if (arguments.length === 0) return merge(this._options);
            this._options = merge(value, this._options);
            this._configure();
            this._viewDate.locale(this._options.locale);
            this._dates = this._dates.map(date => date.clone().locale(this._options.locale));
            this._writeInput();
            this._update();
            return this;
        }

        viewDate(value) {
            if (arguments.length === 0) return this._viewDate.clone();
            const candidate = value ? this._parse(value) : this._lastDate();
            if (!candidate.isValid()) throw new TypeError('viewDate is invalid.');
            this._viewDate = candidate;
            this._update();
            this._emit('update', { viewDate: candidate.clone(), change: undefined });
            return this;
        }

        _icon(role) {
            const host = node('span', `dtp-icon dtp-icon-${role}`, null, { 'aria-hidden': 'true' });
            host.append(node('sm-icon', null, null, { name: this._options.icons[role], 'aria-hidden': 'true' }));
            return host;
        }

        _action(tag, action, title, text, className = '') {
            const element = node(tag, className, text, { 'data-action': action, title });
            if (action === 'previous' || action === 'next' || action.startsWith('increment') || action.startsWith('decrement')) element.classList.add('dtp-step-control');
            if (tag === 'a') { element.href = '#'; element.tabIndex = -1; }
            if (tag === 'button') { element.type = 'button'; element.tabIndex = -1; }
            return element;
        }

        _header(mode) {
            const names = ['Month', 'Year', 'Decade', 'Century'];
            const tips = this._options.tooltips;
            const row = node('tr');
            const previous = this._action('th', 'previous', tips[`prev${names[mode]}`], null, 'prev');
            const next = this._action('th', 'next', tips[`next${names[mode]}`], null, 'next');
            previous.append(this._icon('previous'));
            next.append(this._icon('next'));
            const title = this._action('th', 'pickerSwitch', tips[`select${names[mode]}`] || '', '', 'picker-switch');
            title.colSpan = this._options.calendarWeeks ? 6 : 5;
            const year = this._viewDate.year();
            const first = mode === 2 ? Math.floor(year / 10) * 10 : Math.floor(year / 100) * 100;
            title.textContent = mode === 0 ? this._viewDate.format(this._options.dayViewHeaderFormat) : mode === 1 ? this._viewDate.format('YYYY') : `${first}-${first + (mode === 2 ? 9 : 90)}`;
            const unit = mode === 0 ? 'M' : 'y';
            const step = [1, 1, 10, 100][mode];
            previous.classList.toggle('disabled', !this._isValid(this._viewDate.clone().subtract(step, unit), unit));
            next.classList.toggle('disabled', !this._isValid(this._viewDate.clone().add(step, unit), unit));
            row.append(previous, title, next);
            const head = node('thead');
            head.append(row);
            return head;
        }

        _dateTemplate() {
            const container = node('div', 'datepicker');
            modes.forEach((name, mode) => {
                const view = node('div', `datepicker-${name}`);
                if (mode > 0) view.classList.add('datepicker-periods');
                view.style.display = mode === this.currentViewMode ? '' : 'none';
                const table = node('table', mode === 0 ? 'table table-sm' : 'table-condensed');
                const head = this._header(mode);
                const body = node('tbody');
                if (mode === 0) {
                    const weekdays = node('tr');
                    if (this._options.calendarWeeks) weekdays.append(node('th', 'cw', '#'));
                    const weekday = this._viewDate.clone().startOf('week');
                    for (let i = 0; i < 7; i++) {
                        weekdays.append(node('th', 'dow', weekday.format('dd')));
                        weekday.add(1, 'd');
                    }
                    head.append(weekdays);
                    const day = this._viewDate.clone().startOf('month').startOf('week').startOf('day');
                    for (let week = 0; week < 6; week++) {
                        const row = node('tr');
                        if (this._options.calendarWeeks) row.append(node('td', 'cw', day.week()));
                        for (let i = 0; i < 7; i++) {
                            const cell = this._action('td', 'selectDay', day.format('L'), null, 'day');
                            cell.append(node('span', 'dtp-day', day.date()));
                            cell.dataset.day = day.format('YYYY-MM-DD');
                            cell.classList.toggle('old', day.isBefore(this._viewDate, 'M'));
                            cell.classList.toggle('new', day.isAfter(this._viewDate, 'M'));
                            cell.classList.toggle('active', this._dates.some(value => value.isSame(day, 'd')));
                            cell.classList.toggle('disabled', !this._isValid(day, 'd'));
                            cell.classList.toggle('today', day.isSame(this.getMoment(), 'd'));
                            cell.classList.toggle('weekend', day.day() === 0 || day.day() === 6);
                            row.append(cell);
                            day.add(1, 'd');
                        }
                        body.append(row);
                    }
                }
                else {
                    const row = node('tr');
                    const cell = node('td', null, null, { colspan: this._options.calendarWeeks ? 8 : 7 });
                    const grid = node('div', 'dtp-period-grid');
                    const year = this._viewDate.year();
                    const first = mode === 2 ? Math.floor(year / 10) * 10 - 1 : Math.floor(year / 100) * 100 - 10;
                    for (let i = 0; i < 12; i++) {
                        const value = mode === 1 ? this._viewDate.clone().month(i).startOf('month') : this._viewDate.clone().year(first + i * (mode === 3 ? 10 : 1)).startOf('year');
                        const text = mode === 1 ? value.format('MMM') : value.format('YYYY');
                        const item = this._action('span', mode === 1 ? 'selectMonth' : mode === 2 ? 'selectYear' : 'selectDecade', text, text, name.slice(0, -1));
                        item.dataset.value = mode === 1 ? i : mode === 3 ? value.year() + (i === 0 ? 16 : 6) : value.year();
                        item.classList.toggle('active', this._dates.some(date => mode === 3 ? date.isAfter(value) && date.year() <= value.year() + 11 : date.isSame(value, mode === 1 ? 'M' : 'y')));
                        const includesLimit = mode === 3 && ['minDate', 'maxDate'].some(key => this._options[key] && this._options[key].isAfter(value, 'y') && this._options[key].year() <= value.year() + 11);
                        item.classList.toggle('disabled', !this._isValid(value, mode === 1 ? 'M' : 'y') && !includesLimit);
                        item.classList.toggle('old', mode > 1 && (i === 0 || i === 11));
                        grid.append(item);
                    }
                    cell.append(grid);
                    row.append(cell);
                    body.append(row);
                }
                table.append(head, body);
                view.append(table);
                container.append(view);
            });
            return container;
        }

        _timeTemplate() {
            const container = node('div', 'timepicker');
            const view = node('div', 'timepicker-picker');
            view.style.display = this._timeView === 'picker' ? '' : 'none';
            const table = node('table', 'table-condensed');
            const rows = [node('tr'), node('tr'), node('tr')];
            const value = this._lastDate();
            const units = [['h', 'Hours', 'hour', this.use24Hours ? 'HH' : 'hh'], ['m', 'Minutes', 'minute', 'mm'], ['s', 'Seconds', 'second', 'ss']].filter(([unit]) => this._isEnabled(unit));
            units.forEach(([unit, action, name, format], index) => {
                if (index) rows.forEach((row, i) => row.append(node('td', 'separator', i === 1 ? ':' : '')));
                const top = node('td');
                const bottom = node('td');
                const up = this._action('a', `increment${action}`, this._options.tooltips[`increment${name[0].toUpperCase() + name.slice(1)}`], null, 'btn');
                const down = this._action('a', `decrement${action}`, this._options.tooltips[`decrement${name[0].toUpperCase() + name.slice(1)}`], null, 'btn');
                up.append(this._icon('up'));
                down.append(this._icon('down'));
                top.append(up);
                bottom.append(down);
                const middle = node('td');
                const number = this._action('span', `show${action}`, this._options.tooltips[`pick${name[0].toUpperCase() + name.slice(1)}`], value.format(format), `timepicker-part timepicker-${name}`);
                number.dataset.timeComponent = action.toLowerCase();
                middle.append(number);
                rows[0].append(top);
                rows[1].append(middle);
                rows[2].append(bottom);
            });
            if (!this.use24Hours) {
                rows[0].append(node('td', 'separator'));
                const periodCell = node('td');
                periodCell.append(this._action('button', 'togglePeriod', this._options.tooltips.togglePeriod, value.format('A'), 'btn btn-primary'));
                rows[1].append(periodCell);
                rows[2].append(node('td', 'separator'));
            }
            table.append(...rows);
            view.append(table);
            container.append(view);
            for (const [unit, action, name] of units) {
                const grid = node('div', `timepicker-${action.toLowerCase()}`);
                grid.style.display = this._timeView === action.toLowerCase() ? '' : 'none';
                const table = node('table', 'table-condensed');
                const body = node('tbody');
                const maximum = unit === 'h' ? (this.use24Hours ? 24 : 12) : 60;
                const step = unit === 'h' ? 1 : unit === 'm' ? (this._options.stepping === 1 ? 5 : this._options.stepping) : 5;
                let row;
                let column = 0;
                for (let i = 0; i < maximum; i += step) {
                    if (column++ % 4 === 0) { row = node('tr'); body.append(row); }
                    const number = unit === 'h' && !this.use24Hours ? i + (value.hour() >= 12 ? 12 : 0) : i;
                    const candidate = value.clone().set(unit === 'h' ? 'hour' : name, number);
                    const text = unit === 'h' && !this.use24Hours ? String(i || 12).padStart(2, '0') : String(i).padStart(2, '0');
                    const cell = this._action('td', `select${name[0].toUpperCase() + name.slice(1)}`, text, text, name);
                    cell.dataset.value = number;
                    cell.classList.toggle('disabled', !this._isValid(candidate, unit));
                    row.append(cell);
                }
                table.append(body);
                grid.append(table);
                container.append(grid);
            }
            return container;
        }

        _toolbar() {
            const bar = node('li', `picker-switch${this._options.collapse ? ' accordion-toggle' : ''}`);
            const row = node('tr');
            const actions = [];
            if (this._options.buttons.showToday) actions.push(['today', 'today', 'today']);
            if (this._hasDate() && this._hasTime() && !this._options.sideBySide) actions.push(['togglePicker', this._showTime ? 'date' : 'time', this._showTime ? 'selectDate' : 'selectTime']);
            if (this._options.buttons.showClear) actions.push(['clear', 'clear', 'clear']);
            if (this._options.buttons.showClose) actions.push(['close', 'close', 'close']);
            for (const [action, icon, tip] of actions) {
                const cell = node('td');
                const link = this._action('a', action, this._options.tooltips[tip]);
                link.append(this._icon(icon));
                cell.append(link);
                row.append(cell);
            }
            if (actions.length) {
                const table = node('table', 'table-condensed');
                const body = node('tbody');
                body.append(row);
                table.append(body);
                bar.append(table);
            }
            return bar;
        }

        _motionSettings() {
            if (!this.widget?.animate || this._options.animation === false || window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return null;
            const settings = this._options.animation;
            return settings.duration > 0 ? { duration: settings.duration, easing: settings.easing, fill: 'both' } : null;
        }

        _cancelMotion() {
            const motion = this._motion;
            if (!motion) return;
            this._motion = null;
            for (const animation of motion.animations) animation.cancel();
            motion.cleanup();
        }

        _captureMotion() {
            const settings = this._motionSettings();
            if (!settings) return null;
            const content = this._motion?.content || this.widget.firstElementChild;
            const height = this.widget.getBoundingClientRect().height;
            const top = getComputedStyle(this.widget).top;
            const opacity = getComputedStyle(content).opacity;
            this._cancelMotion();
            return { settings, content, height, top, opacity };
        }

        _trackMotion(animations, content, cleanup) {
            const motion = { animations, content, cleanup };
            this._motion = motion;
            Promise.all(animations.map(animation => animation.finished)).then(() => {
                if (this._motion !== motion) return;
                this._motion = null;
                cleanup();
            }, () => {
                if (this._motion === motion) this._cancelMotion();
            });
        }

        _animateChange(snapshot, direction, horizontal = false) {
            if (!snapshot || !this.widget) return;
            const widget = this.widget;
            const content = widget.firstElementChild;
            const height = widget.getBoundingClientRect().height;
            const contentHeight = content.getBoundingClientRect().height;
            const clip = node('div', 'dtp-transition');
            Object.assign(clip.style, { position: 'relative', overflow: 'hidden', height: `${contentHeight}px` });
            content.replaceWith(clip);
            clip.append(content, snapshot.content);
            // Retain the outgoing view only for its fade, without duplicate interactive controls.
            snapshot.content.setAttribute('aria-hidden', 'true');
            snapshot.content.inert = true;
            Object.assign(snapshot.content.style, { position: 'absolute', inset: '0 auto auto 0', width: '100%', pointerEvents: 'none' });
            widget.style.height = `${height}px`;
            const translate = distance => horizontal ? `translateX(${distance}px)` : `translateY(${distance}px)`;
            const animations = [
                widget.animate([{ height: `${snapshot.height}px`, top: snapshot.top }, { height: `${height}px`, top: widget.style.top }], snapshot.settings),
                clip.animate([{ height: `${Math.max(0, contentHeight + snapshot.height - height)}px` }, { height: `${contentHeight}px` }], snapshot.settings),
                content.animate([{ opacity: 0, transform: translate(direction * 8) }, { opacity: 1, transform: 'none' }], snapshot.settings),
                snapshot.content.animate([{ opacity: snapshot.opacity, transform: 'none' }, { opacity: 0, transform: translate(-direction * 6) }], snapshot.settings)
            ];
            this._trackMotion(animations, content, () => {
                for (const animation of animations) animation.cancel();
                snapshot.content.remove();
                if (clip.parentNode === widget) clip.replaceWith(content);
                widget.style.removeProperty('height');
                if (this.widget === widget) this._place();
            });
        }

        _animateOpen() {
            const settings = this._motionSettings();
            if (!settings) return;
            const animation = this.widget.animate([{ opacity: 0 }, { opacity: 1 }], settings);
            this._trackMotion([animation], this.widget.firstElementChild, () => animation.cancel());
        }

        _update() {
            if (!this.widget) return;
            this._cancelMotion();
            const widget = this.widget;
            widget.replaceChildren();
            const hasDate = this._hasDate();
            const hasTime = this._hasTime();
            const sideBySide = this._options.sideBySide && hasDate && hasTime;
            const collapsed = this._options.collapse && hasDate && hasTime;
            widget.classList.toggle('usetwentyfour', this.use24Hours);
            widget.classList.toggle('timepicker-sbs', sideBySide);
            const content = node('ul', 'list-unstyled');
            const toolbar = this._toolbar();
            if (this._options.toolbarPlacement === 'top') content.append(toolbar);
            if (sideBySide) {
                const row = node('div', 'row');
                const date = this._dateTemplate();
                const time = this._timeTemplate();
                date.classList.add('col-md-6');
                time.classList.add('col-md-6');
                row.append(date, time);
                content.append(row);
                if (this._options.toolbarPlacement !== 'top') content.append(toolbar);
            }
            else {
                for (const time of [false, true]) {
                    if (time && this._options.toolbarPlacement === 'default') content.append(toolbar);
                    if (!(time ? hasTime : hasDate)) continue;
                    const section = node('li', collapsed ? `collapse${this._showTime === time ? ' show' : ''}` : 'show');
                    section.append(time ? this._timeTemplate() : this._dateTemplate());
                    content.append(section);
                }
                if (this._options.toolbarPlacement === 'bottom') content.append(toolbar);
            }
            widget.append(content);
            if (!this._options.inline) widget.append(node('div', 'arrow', null, { 'aria-hidden': 'true' }));
            this._place();
        }

        _doAction(event) {
            const target = event.target.closest('[data-action]');
            if (!target || !this.widget.contains(target)) return;
            event.preventDefault();
            if (target.classList.contains('disabled')) return;
            const action = target.dataset.action;
            const animate = ['previous', 'next', 'pickerSwitch', 'selectMonth', 'selectYear', 'selectDecade', 'togglePicker', 'showHours', 'showMinutes', 'showSeconds', 'selectHour', 'selectMinute', 'selectSecond'].includes(action);
            const snapshot = animate ? this._captureMotion() : null;
            const direction = action === 'previous' || action === 'pickerSwitch' || action.startsWith('select') || (action === 'togglePicker' && this._showTime) ? -1 : 1;
            const value = this._lastDate();
            if (action === 'previous' || action === 'next') {
                const unit = this.currentViewMode === 0 ? 'M' : 'y';
                this._viewDate.add((action === 'previous' ? -1 : 1) * [1, 1, 10, 100][this.currentViewMode], unit);
                this._emit('update', { change: unit, viewDate: this._viewDate.clone() });
            }
            else if (action === 'pickerSwitch') this.currentViewMode = Math.min(3, this.currentViewMode + 1);
            else if (action === 'selectDay') {
                const day = moment(target.dataset.day, 'YYYY-MM-DD', this._options.locale, true);
                value.year(day.year()).month(day.month()).date(day.date());
                if (this._options.allowMultidate) {
                    const index = this._dates.findIndex(date => date.isSame(value, 'd'));
                    this.date(index < 0 ? value : null, index < 0 ? this._dates.length : index);
                }
                else this.date(value);
                if (!this._hasTime() && !this._options.keepOpen && !this._options.inline && !this._options.allowMultidate) this.hide();
            }
            else if (['selectMonth', 'selectYear', 'selectDecade'].includes(action)) {
                const number = Number(target.dataset.value);
                if (action === 'selectMonth') this._viewDate.month(number);
                else this._viewDate.year(number);
                if (this.currentViewMode === this._minimumMode) {
                    value.year(this._viewDate.year());
                    if (action === 'selectMonth') value.month(this._viewDate.month());
                    this.date(value);
                    if (!this._options.inline) this.hide();
                }
                else this.currentViewMode--;
                this._emit('update', { change: action === 'selectMonth' ? 'M' : 'YYYY', viewDate: this._viewDate.clone() });
            }
            else if (action === 'togglePicker') { this._showTime = !this._showTime; this._timeView = 'picker'; }
            else if (action === 'togglePeriod') this.date(value.add(value.hour() >= 12 ? -12 : 12, 'h'));
            else if (/^(increment|decrement)(Hours|Minutes|Seconds)$/.test(action)) {
                const unit = action.endsWith('Hours') ? 'h' : action.endsWith('Minutes') ? 'm' : 's';
                value.add((action.startsWith('increment') ? 1 : -1) * (unit === 'm' ? this._options.stepping : 1), unit);
                if (this._isValid(value, unit)) this.date(value);
            }
            else if (/^show(Hours|Minutes|Seconds)$/.test(action)) this._timeView = action.slice(4).toLowerCase();
            else if (/^select(Hour|Minute|Second)$/.test(action)) {
                this.date(value.set(action.slice(6).toLowerCase(), Number(target.dataset.value)));
                this._timeView = 'picker';
                const lastComponent = action === 'selectSecond' || (action === 'selectMinute' && !this._isEnabled('s')) || (action === 'selectHour' && !this._isEnabled('m'));
                if (lastComponent && !this._options.keepOpen && !this._options.inline) this.hide();
            }
            else if (action === 'today') {
                const today = this.getMoment();
                if (this._isValid(today, 'd')) this.date(today);
            }
            else if (action === 'clear') this.clear();
            else if (action === 'close') this.hide();
            this._update();
            this._animateChange(snapshot, direction, action === 'previous' || action === 'next');
        }

        _keydown(event) {
            if (event.key === 'Tab') { this.hide(); return; }
            if (event.key === 'Escape' || event.key === 'Enter') {
                if (this.widget || event.key === 'Enter') { event.preventDefault(); this.hide(); }
                return;
            }
            if (event.key.toLowerCase() === 't') { event.preventDefault(); this.date(this.getMoment()); return; }
            if (!this.widget) {
                if (event.key === 'ArrowDown') { event.preventDefault(); this.show(); }
                return;
            }
            const date = this._lastDate();
            const time = !this._hasDate() || this._showTime;
            const adjustments = time
                ? { ArrowUp: [this._options.stepping, 'm'], ArrowDown: [-this._options.stepping, 'm'] }
                : { ArrowUp: [-7, 'd'], ArrowDown: [7, 'd'], ArrowLeft: [-1, 'd'], ArrowRight: [1, 'd'], PageUp: [-1, 'M'], PageDown: [1, 'M'] };
            let adjustment = adjustments[event.key];
            if (event.ctrlKey && ['ArrowUp', 'ArrowDown'].includes(event.key)) {
                adjustment = [(event.key === 'ArrowUp' ? 1 : -1) * (time ? 1 : -1), time ? 'h' : 'y'];
            }
            if (adjustment) { event.preventDefault(); this.date(date.add(...adjustment)); }
            else if (event.key === 'Delete') { event.preventDefault(); this.clear(); }
            else if (event.ctrlKey && event.key === ' ' && time && !this.use24Hours) {
                event.preventDefault();
                this.date(date.add(date.hour() >= 12 ? -12 : 12, 'h'));
            }
        }

        _place() {
            if (!this.widget || this._options.inline) return;
            const anchor = this.component || this.element;
            const position = anchor.getBoundingClientRect();
            const width = this.widget.offsetWidth;
            const height = this.widget.offsetHeight;
            const preferred = this._options.widgetPositioning;
            const top = preferred.vertical === 'top' || (preferred.vertical === 'auto' && position.bottom + height > window.innerHeight && position.top - height > -2);
            const left = preferred.horizontal === 'left' || (preferred.horizontal !== 'left' && position.right - width < 0);
            this.widget.classList.toggle('bs-popover-top', top);
            this.widget.classList.toggle('bs-popover-bottom', !top);
            const rtl = getComputedStyle(anchor).direction === 'rtl';
            this.widget.dir = rtl ? 'rtl' : 'ltr';
            this.widget.classList.toggle('dtp-align-end', left === rtl);
            const parent = this.widget.offsetParent || document.documentElement;
            const offset = parent === document.body || parent === document.documentElement ? { top: -window.scrollY, left: -window.scrollX } : parent.getBoundingClientRect();
            Object.assign(this.widget.style, {
                top: `${Math.max(0, (top ? position.top - height - 8 : position.bottom) - offset.top)}px`,
                left: `${Math.max(0, (left ? position.left : position.right - width) - offset.left)}px`,
                right: 'auto', bottom: 'auto'
            });
        }

        show() {
            if (this.destroyed || this.widget || this.input.disabled || (this.input.readOnly && !this._options.ignoreReadonly)) return this;
            if (this.input.value.trim()) this._readInput();
            else if (!this._dates.length && this._options.useCurrent) {
                const now = this.getMoment();
                if (typeof this._options.useCurrent === 'string') now.startOf(this._options.useCurrent);
                this.date(now);
            }
            this._timeView = 'picker';
            this._showTime = this._options.viewMode === 'time';
            this.widget = node('div', `dtpicker${this._options.inline ? '' : ' popover dropdown-menu'}`);
            this.widget.style.display = 'block';
            this.widget.setAttribute('role', 'dialog');
            this.widget.setAttribute('aria-label', this._hasDate() ? this._options.tooltips.selectDate : this._options.tooltips.selectTime);
            this.widget.addEventListener('mousedown', event => event.preventDefault());
            this.widget.addEventListener('click', event => this._doAction(event));
            const parent = this._options.inline ? this.element : typeof this._options.widgetParent === 'string' ? document.querySelector(this._options.widgetParent) : this._options.widgetParent || document.body;
            if (!parent) throw new Error('The date/time picker widget parent was not found.');
            parent.append(this.widget);
            this._update();
            this._animateOpen();
            this.component?.classList.add('active');
            this.input.setAttribute('aria-expanded', 'true');
            if (this._options.focusOnShow && !this._options.inline) this.input.focus({ preventScroll: true });
            this._emit('show');
            return this;
        }

        hide() {
            if (!this.widget) return this;
            this._cancelMotion();
            this.widget.remove();
            this.widget = null;
            this.component?.classList.remove('active');
            this.input.setAttribute('aria-expanded', 'false');
            this._viewDate = this._lastDate();
            this._emit('hide', { date: this._lastDate() });
            this.input.blur();
            return this;
        }

        toggle() { return this.widget ? this.hide() : this.show(); }
        enable() { this.input.disabled = false; return this; }
        disable() { this.hide(); this.input.disabled = true; return this; }

        destroy() {
            if (this.destroyed) return;
            this._cancelMotion();
            this.events.abort();
            this.widget?.remove();
            this.widget = null;
            this.component?.classList.remove('active');
            this.input.removeAttribute('aria-expanded');
            instances.delete(this.element);
            this.destroyed = true;
        }
    }

    namespace.DateTimePicker = DateTimePicker;
})();
