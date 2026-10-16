// Shared Smartstore color picker; instantiated by callers or the editor initializer.
(() => {
    'use strict';

    const namespace = window.Smartstore = window.Smartstore || {};
    const instances = new WeakMap();
    const clamp = (value, max = 1) => Math.min(max, Math.max(0, value));
    const round = value => Math.round(value * 1000) / 1000;
    const themeColors = [
        'blue', 'indigo', 'purple', 'pink', 'red', 'orange', 'yellow', 'green', 'teal', 'cyan',
        'primary-bg-subtle', 'warning-bg-subtle', 'success-bg-subtle', 'danger-bg-subtle', 'info-bg-subtle',
        'white', 'light', 'gray', 'gray-dark', 'black'
    ];

    // The controls operate in HSV/sRGB. Keep the source expression separately so
    // choosing a theme variable does not replace it with its current RGB value.
    class Color {
        constructor(r, g, b, a = 1) {
            this.r = clamp(r, 255);
            this.g = clamp(g, 255);
            this.b = clamp(b, 255);
            this.a = clamp(a);
        }

        static fromHsv(h, s, v, a = 1) {
            const channel = n => {
                const k = (n + h / 60) % 6;
                return 255 * v * (1 - s * Math.max(0, Math.min(k, 4 - k, 1)));
            };
            return new Color(channel(5), channel(3), channel(1), a);
        }

        toHsv() {
            const r = this.r / 255, g = this.g / 255, b = this.b / 255;
            const max = Math.max(r, g, b), min = Math.min(r, g, b), delta = max - min;
            let h = 0;
            if (delta) {
                h = max === r ? (g - b) / delta : max === g ? (b - r) / delta + 2 : (r - g) / delta + 4;
                h = (h * 60 + 360) % 360;
            }
            return { h, s: max ? delta / max : 0, v: max, a: this.a };
        }

        toHexString(alpha = false) {
            const bytes = [this.r, this.g, this.b];
            if (alpha) bytes.push(this.a * 255);
            return '#' + bytes.map(x => Math.round(x).toString(16).padStart(2, '0')).join('');
        }

        toRgbString() {
            const rgb = [this.r, this.g, this.b].map(Math.round).join(', ');
            return this.a === 1 ? `rgb(${rgb})` : `rgba(${rgb}, ${round(this.a)})`;
        }

        toHslString() {
            const { h, s, v } = this.toHsv();
            const l = v * (1 - s / 2);
            const saturation = l === 0 || l === 1 ? 0 : (v - l) / Math.min(l, 1 - l);
            const hsl = `${round(h)}, ${round(saturation * 100)}%, ${round(l * 100)}%`;
            return this.a === 1 ? `hsl(${hsl})` : `hsla(${hsl}, ${round(this.a)})`;
        }

        toString(format) {
            format = format?.replace(/a$/, '');
            if (format === 'hsl') return this.toHslString();
            if (format === 'rgb') return this.toRgbString();
            if (format === 'hex') return this.toHexString(this.a < 1);
            return this.a < 1 ? this.toRgbString() : this.toHexString();
        }
    }

    // Ask the browser to resolve CSS syntax and variables in the owner's context.
    // This is a preview conversion, not a serializer or a wide-gamut color model.
    class ColorResolver {
        constructor(context) {
            this.context = context;
        }

        parse(value) {
            if (!value || !CSS.supports('color', value) || /^(inherit|initial|unset|revert|revert-layer)$/i.test(value)) return null;
            const host = document.createElement('span');
            const probe = document.createElement('span');
            host.style.cssText = 'position:absolute;visibility:hidden;pointer-events:none;width:0;height:0;overflow:hidden';
            probe.style.color = value;
            host.append(probe);
            const context = this.context.matches('input') ? this.context.parentElement : this.context;
            (context || document.body).append(host);
            try {
                // Invalid variable substitutions inherit. Two sentinel colors distinguish
                // those from valid colors without maintaining a second CSS parser.
                const inherited = getComputedStyle(context || document.body).color;
                host.style.color = 'rgb(1, 2, 3)';
                const first = getComputedStyle(probe).color;
                host.style.color = 'rgb(4, 5, 6)';
                const second = getComputedStyle(probe).color;
                if (first !== second && !/currentcolor/i.test(value)) return null;
                host.style.color = inherited;
                const resolved = getComputedStyle(probe).color;

                // Avoid canvas quantization for the common RGB/hex path.
                const rgb = resolved.match(/^rgba?\((\d+(?:\.\d+)?),\s*(\d+(?:\.\d+)?),\s*(\d+(?:\.\d+)?)(?:,\s*([\d.]+))?\)$/);
                if (rgb) return new Color(+rgb[1], +rgb[2], +rgb[3], rgb[4] === undefined ? 1 : +rgb[4]);

                this.canvas ||= document.createElement('canvas');
                this.canvas.width = this.canvas.height = 1;
                const canvas = this.canvas.getContext('2d', { willReadFrequently: true });
                if (!canvas) return null;
                canvas.clearRect(0, 0, 1, 1);
                canvas.fillStyle = resolved;
                canvas.fillRect(0, 0, 1, 1);
                const [r, g, b, a] = canvas.getImageData(0, 0, 1, 1).data;
                return new Color(r, g, b, a / 255);
            }
            finally { host.remove(); }
        }
    }

    class ColorPicker {
        static defaults = {
            color: false,
            fallbackColor: false,
            autoInputFallback: false,
            autoHexInputFallback: false,
            input: 'input',
            showInput: false,
            addon: '.colorpicker-input-addon',
            container: false,
            inline: false,
            showOnFocus: true,
            useAlpha: true,
            format: null,
            customClass: null,
            swatches: true,
            // A map of stored expressions to preview colors, or a function returning one.
            colors: null,
            preview: true,
            showText: false,
            popover: { animation: true, placement: 'top', fallbackPlacement: 'flip' },
            labels: { saturation: 'Saturation and brightness', hue: 'Hue', alpha: 'Opacity', color: 'Color' }
        };

        static getInstance(element) {
            return instances.get(element);
        }

        constructor(element, options = {}) {
            if (!(element instanceof HTMLElement)) throw new TypeError('A color picker element is required.');
            if (instances.has(element)) throw new Error('This element already has a Smartstore color picker.');
            this.element = element;
            this.options = { ...ColorPicker.defaults, ...options };
            this.options.labels = { ...ColorPicker.defaults.labels, ...options.labels };
            this.input = element.matches('input') ? element : this.options.input ? element.querySelector(this.options.input) : null;
            this.addon = this.options.addon ? element.querySelector(this.options.addon) : null;
            this.trigger = this.addon || (!this.input && !this.options.inline && this.options.popover !== false ? element : null);
            this.resolver = new ColorResolver(element);
            this.events = new AbortController();
            this.value = '';
            this.detectedFormat = null;
            this.color = null;
            this.hsv = { h: 0, s: 1, v: 1, a: 1 };
            this.disabled = Boolean(this.input?.disabled);
            this.visible = false;
            this.destroyed = false;
            this._createPicker();
            this._bind();
            instances.set(element, this);
            element.classList.add('colorpicker-element');
            const initial = this.options.color !== false ? this.options.color : this.input?.value || this.options.fallbackColor || '';
            this.setValue(initial, false);
            this._resolveAlpha();
            this._update();
            // Consumers can subscribe after construction. All content already exists
            // when Create is raised, so popover measurements include the palette.
            queueMicrotask(() => {
                if (this.destroyed) return;
                if (this.options.inline) this.show();
                this._emit('Create');
            });
        }

        _on(target, type, listener) {
            target?.addEventListener(type, listener, { signal: this.events.signal });
        }

        _emit(name, value = this.value) {
            const detail = { colorpicker: this, color: this.color, value };
            // Use one event path, avoiding duplicate delivery to jQuery consumers.
            if (window.jQuery) window.jQuery(this.element).trigger({ type: `colorpicker${name}`, ...detail });
            else this.element.dispatchEvent(new CustomEvent(`colorpicker${name}`, { bubbles: true, detail }));
        }

        _createPicker() {
            const picker = this.picker = document.createElement('div');
            picker.className = 'colorpicker colorpicker-horizontal colorpicker-hidden';
            if (this.options.customClass) picker.classList.add(...this.options.customClass.split(/\s+/).filter(Boolean));
            picker.innerHTML = '<div class="colorpicker-saturation"><i class="colorpicker-guide"></i></div>'
                + '<div class="colorpicker-hue"><i class="colorpicker-guide"></i></div>'
                + '<div class="colorpicker-alpha"><div class="colorpicker-alpha-color"></div><i class="colorpicker-guide"></i></div>';
            this.controls = {};
            for (const name of ['saturation', 'hue', 'alpha']) {
                const control = picker.querySelector(`.colorpicker-${name}`);
                control.tabIndex = 0;
                control.setAttribute('role', name === 'saturation' ? 'group' : 'slider');
                control.setAttribute('aria-label', this.options.labels[name]);
                control.style.touchAction = 'none';
                this.controls[name] = control;
            }
            if (this.options.preview) {
                const preview = document.createElement('div');
                preview.className = 'colorpicker-bar colorpicker-preview';
                this.preview = document.createElement('div');
                preview.append(this.preview);
                picker.append(preview);
            }
            if (this.options.showInput) this._createInput();
            if (this.options.swatches) this._createSwatches();
            const container = this.options.container;
            this.container = container === true ? this.element : typeof container === 'string' ? document.querySelector(container) : container;
            if (this.options.inline || this.options.popover === false) {
                const parent = this.container || (this.element.matches('input') ? this.element.parentElement : this.element);
                parent.append(picker);
            }
        }

        _createInput() {
            const group = document.createElement('div');
            group.className = 'colorpicker-input';
            const input = this.pickerInput = document.createElement('input');
            input.type = 'text';
            input.className = 'form-control form-control-sm';
            input.autocomplete = 'off';
            input.spellcheck = false;
            input.setAttribute('aria-label', this.options.labels.color);
            this._on(input, 'keydown', event => {
                if (event.key === 'Enter') event.preventDefault();
            });
            group.append(input);
            this.picker.append(group);
            this.picker.classList.add('colorpicker-with-input');
        }

        _createSwatches() {
            let colors = typeof this.options.colors === 'function' ? this.options.colors(this) : this.options.colors;
            if (!colors) {
                const style = getComputedStyle(this.element);
                colors = Object.fromEntries(themeColors.map(name => [`var(--${name})`, style.getPropertyValue(`--${name}`).trim()]).filter(([, value]) => value));
            }
            const bar = document.createElement('div');
            bar.className = 'colorpicker-bar colorpicker-swatches';
            const inner = document.createElement('div');
            inner.className = 'colorpicker-swatches--inner';
            for (const [value, preview] of Object.entries(colors)) {
                const swatch = document.createElement('span');
                swatch.className = 'colorpicker-swatch';
                const button = document.createElement('a');
                button.className = 'colorpicker-swatch--inner';
                button.href = '#';
                button.setAttribute('role', 'button');
                button.title = value;
                button.setAttribute('aria-label', value);
                button.style.backgroundColor = preview;
                this._on(button, 'click', event => {
                    event.preventDefault();
                    this.setValue(value);
                });
                this._on(button, 'keydown', event => {
                    if (event.key === ' ') { event.preventDefault(); button.click(); }
                });
                swatch.append(button);
                inner.append(swatch);
            }
            bar.append(inner);
            this.picker.append(bar);
        }

        _bind() {
            for (const input of [this.input, this.pickerInput].filter(Boolean)) {
                const read = () => {
                    this.editingInput = input;
                    try { this.setValue(input.value, true, false); }
                    finally { this.editingInput = null; }
                };
                this._on(input, 'input', read);
                this._on(input, 'change', read);
            }
            this._on(this.input, 'focus', () => {
                if (this.options.showOnFocus && !this.restoringFocus) this.show();
            });
            this._on(this.trigger, 'click', event => { event.preventDefault(); this.toggle(); });
            this._on(this.element, 'keydown', event => {
                if (event.key === 'Escape') this.hide();
            });
            this._on(document, 'pointerdown', event => {
                if (!this.options.inline && !this.element.contains(event.target) && !this.picker.contains(event.target)) this.hide();
            });
            this._on(this.input?.form, 'reset', () => queueMicrotask(() => {
                if (!this.destroyed) this.setValue(this.input.value);
            }));
            for (const [name, control] of Object.entries(this.controls)) {
                this._on(control, 'pointerdown', event => {
                    if (this.disabled || event.button !== 0 || this.drag) return;
                    event.preventDefault();
                    control.focus({ preventScroll: true });
                    control.setPointerCapture(event.pointerId);
                    this.drag = { id: event.pointerId, control, name };
                    this._move(event);
                });
                this._on(control, 'pointermove', event => this._move(event));
                for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) {
                    this._on(control, type, event => {
                        if (this.drag?.id !== event.pointerId) return;
                        this.drag = null;
                        if (control.hasPointerCapture(event.pointerId)) control.releasePointerCapture(event.pointerId);
                    });
                }
                this._on(control, 'keydown', event => this._key(event, name));
            }
            this._on(this.picker, 'keydown', event => {
                if (event.key === 'Escape') {
                    this.hide();
                    this.restoringFocus = true;
                    (this.input || this.trigger)?.focus({ preventScroll: true });
                    this.restoringFocus = false;
                }
            });
        }

        _move(event) {
            if (this.disabled || this.drag?.id !== event.pointerId) return;
            const { control, name } = this.drag;
            const rect = control.getBoundingClientRect();
            if (!rect.width || !rect.height) return;
            const x = clamp((event.clientX - rect.left) / rect.width);
            const y = clamp((event.clientY - rect.top) / rect.height);
            if (name === 'saturation') { this.hsv.s = x; this.hsv.v = 1 - y; }
            else if (name === 'hue') this.hsv.h = x * 360;
            else this.hsv.a = x;
            this._applyHsv();
        }

        _key(event, name) {
            if (this.disabled || !['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return;
            event.preventDefault();
            const step = event.shiftKey ? 10 : 1;
            const property = name === 'hue' ? 'h' : name === 'alpha' ? 'a' : ['ArrowUp', 'ArrowDown'].includes(event.key) ? 'v' : 's';
            const max = property === 'h' ? 360 : 1;
            let direction = ['ArrowRight', 'ArrowUp'].includes(event.key) ? 1 : -1;
            this.hsv[property] = event.key === 'Home' ? 0 : event.key === 'End' ? max
                : clamp(this.hsv[property] + direction * step * (max === 1 ? 0.01 : 1), max);
            this._applyHsv();
        }

        _applyHsv() {
            const { h, s, v, a } = this.hsv;
            this.color = Color.fromHsv(h, s, v, this.alphaEnabled ? a : 1);
            const value = this.color.toString(this._format());
            const changed = value !== this.value;
            this.value = value;
            this._update();
            if (changed) this._changed();
        }

        _changed() {
            // Notify form bindings for picker-driven changes, without duplicating typing events.
            if (this.input && this.editingInput !== this.input) {
                this.input.dispatchEvent(new Event('change', { bubbles: true }));
            }
            this._emit('Change');
        }

        _format() {
            const format = this.options.format === 'auto' ? this.detectedFormat : this.options.format || this.detectedFormat;
            // A dynamically chosen hex format must not discard a subsequently selected alpha value.
            return !this.options.format && format === 'hex' && this.color?.a < 1 ? 'rgb' : format;
        }

        _resolveAlpha() {
            if (this.alphaEnabled !== undefined) return;
            const format = this.options.format === 'auto' ? this.detectedFormat : this.options.format;
            this.alphaEnabled = this.options.useAlpha !== false && format !== 'hex'
                && (this.options.useAlpha === true || Boolean(this.color && this.color.a < 1));
        }

        /** Accepts a CSS color or an empty string. Input correction follows autoInputFallback. */
        setValue(value, notify = true, syncInput = true) {
            if (this.disabled && notify || this.destroyed) return false;
            value = value == null || value === false ? '' : String(value).trim();
            if (value === this.value) return true;
            let color = value ? this.resolver.parse(value) : null;
            if (value && !color) {
                this._emit('Invalid', value);
                if (!this.options.autoInputFallback) return false;
                const fallback = this.options.fallbackColor;
                color = fallback ? this.resolver.parse(String(fallback)) : null;
                value = color ? String(fallback) : this.value || '#000000';
                color ||= this.color ? new Color(this.color.r, this.color.g, this.color.b, this.color.a) : new Color(0, 0, 0);
                syncInput = true;
            }
            if (color && (this.options.format !== 'auto' || !this.detectedFormat)) {
                this.detectedFormat = /^#/.test(value) ? 'hex' : /^hsla?\(/i.test(value) ? 'hsl' : 'rgb';
            }
            this.color = color;
            if (color) this._resolveAlpha();
            const removeAlpha = color && !this.alphaEnabled && color.a < 1;
            if (removeAlpha) color.a = 1;
            this.value = color && (this.options.format || removeAlpha) ? color.toString(this._format()) : value;
            if (color && this.options.autoHexInputFallback && /^#[\da-f]{3}$/i.test(value)) this.value = color.toHexString();
            if (color) {
                const hsv = color.toHsv();
                if (!hsv.s) hsv.h = this.hsv.h;
                this.hsv = hsv;
            }
            this._update(syncInput || this.options.autoInputFallback || this.options.autoHexInputFallback);
            if (notify) this._changed();
            return true;
        }

        getValue(defaultValue = null) {
            return this.value || defaultValue;
        }

        /** Re-resolves theme variables after a theme change without changing their stored expression. */
        update() {
            if (this.destroyed) return;
            const color = this.value ? this.resolver.parse(this.value) : null;
            if (color) {
                if (!this.alphaEnabled) color.a = 1;
                this.color = color;
                this.hsv = color.toHsv();
            }
            this._update();
        }

        _update(syncInput = true) {
            const { h, s, v, a } = this.hsv;
            const { saturation, hue, alpha } = this.controls;
            const position = (control, x, y = 0) => {
                const guide = control.querySelector('.colorpicker-guide');
                guide.style.left = `${x * 100}%`;
                guide.style.right = 'auto';
                guide.style.top = `${y * 100}%`;
            };
            position(saturation, s, 1 - v);
            position(hue, h / 360, 0.5);
            position(alpha, a, 0.5);
            const hueColor = Color.fromHsv(h, 1, 1).toHexString();
            saturation.style.backgroundColor = hueColor;
            this.picker.style.setProperty('--cp-hue', hueColor);
            this.picker.style.setProperty('--cp-color', this.color?.toRgbString() || 'transparent');
            const opaque = Color.fromHsv(h, s, v);
            alpha.querySelector('.colorpicker-alpha-color').style.backgroundImage = `linear-gradient(to right, ${new Color(opaque.r, opaque.g, opaque.b, 0).toRgbString()}, ${opaque.toRgbString()})`;
            for (const [control, max, value] of [[hue, 360, h], [alpha, 100, a * 100]]) {
                control.setAttribute('aria-valuemin', '0');
                control.setAttribute('aria-valuemax', max);
                control.setAttribute('aria-valuenow', round(value));
            }
            this.picker.classList.toggle('colorpicker-with-alpha', Boolean(this.alphaEnabled));
            this.picker.classList.toggle('colorpicker-disabled', this.disabled);
            for (const control of Object.values(this.controls)) {
                control.tabIndex = this.disabled ? -1 : 0;
                control.setAttribute('aria-disabled', String(this.disabled));
            }
            for (const input of [this.input, this.pickerInput].filter(Boolean)) {
                if ((syncInput || input !== this.editingInput) && input.value !== this.value) input.value = this.value;
            }
            if (this.pickerInput) this.pickerInput.disabled = this.disabled;
            const cssColor = this.color?.toRgbString() || '';
            if (this.preview) {
                this.preview.style.backgroundColor = cssColor;
                this.preview.textContent = this.options.showText ? this.value : '';
            }
            const addonPreview = this.addon?.querySelector('i');
            if (addonPreview) addonPreview.style.backgroundColor = cssColor;
            this._emit('Update');
        }

        _createPopover() {
            if (this.popover) return;
            const target = this.addon || this.input || this.element;
            const options = {
                ...ColorPicker.defaults.popover, ...this.options.popover,
                trigger: 'manual', html: true, content: this.picker,
                container: this.container || document.body
            };
            this.decoratePopover = () => {
                const tip = this.popover.getTipElement?.() || this.popover.tip;
                tip?.classList.add('colorpicker-bs-popover', 'colorpicker-popover');
            };
            this.picker.classList.add('colorpicker-bs-popover-content');
            if (window.bootstrap?.Popover) {
                if (window.bootstrap.Popover.getInstance?.(target)) throw new Error('The color picker target already owns a popover.');
                this.popover = new window.bootstrap.Popover(target, options);
            }
            else if (window.jQuery?.fn.popover) {
                const el = window.jQuery(target);
                if (el.data('bs.popover')) throw new Error('The color picker target already owns a popover.');
                el.popover(options);
                this.popover = el.data('bs.popover');
            }
            else throw new Error('Popup mode requires Bootstrap Popover; inline mode has no Bootstrap dependency.');
            this.decoratePopover();
        }

        show() {
            if (this.destroyed || this.disabled || this.visible) return;
            if (!this.options.inline && this.options.popover !== false) this._createPopover();
            this.picker.classList.replace('colorpicker-hidden', 'colorpicker-visible');
            this.popover?.show();
            this.decoratePopover?.();
            this.visible = true;
            if (!this.options.inline) this.pickerInput?.focus({ preventScroll: true });
            this._emit('Show');
        }

        hide() {
            if (!this.visible || this.options.inline) return;
            this.popover?.hide();
            this.picker.classList.replace('colorpicker-visible', 'colorpicker-hidden');
            this.visible = false;
            this._emit('Hide');
        }

        toggle() { this.visible ? this.hide() : this.show(); }

        enable() {
            this.disabled = false;
            if (this.input) this.input.disabled = false;
            this._update();
            this._emit('Enable');
        }

        disable() {
            this.hide();
            this.disabled = true;
            this.drag = null;
            if (this.input) this.input.disabled = true;
            this._update();
            this._emit('Disable');
        }

        destroy() {
            if (this.destroyed) return;
            this.destroyed = true;
            this.events.abort();
            this.popover?.dispose();
            this.picker.remove();
            this.element.classList.remove('colorpicker-element');
            instances.delete(this.element);
            window.jQuery?.(this.element).removeData('colorpicker');
            this._emit('Destroy');
        }
    }

    namespace.ColorPicker = ColorPicker;

    // Optional jQuery adapter for editor initialization and existing jQuery consumers.
    if (window.jQuery) {
        const $ = window.jQuery;
        $.fn.colorpicker = function (option, ...args) {
            if (option === 'getValue') return instances.get(this[0])?.getValue(...args);
            return this.each(function () {
                let picker = instances.get(this);
                if (typeof option === 'string') {
                    if (!['setValue', 'update', 'show', 'hide', 'toggle', 'enable', 'disable', 'destroy'].includes(option)) {
                        throw new Error(`Unknown color picker method: ${option}`);
                    }
                    picker?.[option](...args);
                }
                else if (!picker) {
                    picker = new ColorPicker(this, { ...$(this).data(), ...option });
                    $(this).data('colorpicker', picker);
                }
            });
        };
    }
})();
