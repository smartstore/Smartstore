// Shared icon option editors. Hosts own their toolbar, selection and persistence.
(() => {
    'use strict';
    const namespace = window.Smartstore = window.Smartstore || {};
    if (namespace.IconTools) return;
    let closeCurrent;
    let nextToolId = 0;

    function addArrow(menu) {
        const arrow = document.createElement('span');
        arrow.className = 'icon-tool-arrow';
        arrow.setAttribute('x-arrow', '');
        arrow.setAttribute('aria-hidden', 'true');
        menu.append(arrow);
    }

    namespace.IconTools = {
        bindSize(toggle, callbacks) {
            const values = ['xs', 'sm', '', 'lg', 'xl', ...Array.from({ length: 9 }, (_, i) => `${i + 2}x`)];
            return this.bindRange(toggle, callbacks, {
                values, labels: values.map(value => value || '1x'),
                normalize: value => value === '1x' ? '' : value === '2xl' ? '2x' : value || ''
            });
        },
        bindOpacity(toggle, callbacks) {
            const percentages = Array.from({ length: 101 }, (_, i) => i);
            return this.bindRange(toggle, {
                ...callbacks, onChange: value => callbacks.onChange(value === '1' ? '' : value)
            }, {
                values: percentages.map(value => String(value / 100)),
                labels: percentages.map(value => `${value} %`), ticks: [],
                menuClass: 'icon-tool-opacity-menu', showPreview: true, previewOption: 'opacity',
                normalize(value) {
                    const number = value == null || value === '' ? 1 : Number(value);
                    return String(Math.round(Math.max(0, Math.min(1, Number.isFinite(number) ? number : 1)) * 100) / 100);
                }
            });
        },
        bindStrokeScale(toggle, callbacks) {
            const percentages = Array.from({ length: 36 }, (_, i) => 25 + i * 5);
            return this.bindRange(toggle, callbacks, {
                values: percentages.map(value => value === 100 ? '' : String(value / 100)),
                labels: percentages.map(value => `${value} %`), ticks: [0, 5, 10, 15, 25, 35], showPreview: callbacks.showPreview === true,
                normalize(value) {
                    const percent = Math.max(25, Math.min(200, Math.round((Number(String(value).replace(',', '.')) || 1) * 20) * 5));
                    return percent === 100 ? '' : String(percent / 100);
                }
            });
        },
        bindRotate(toggle, callbacks) {
            const degrees = Array.from({ length: 361 }, (_, i) => i);
            return this.bindRange(toggle, {
                ...callbacks, onChange: value => callbacks.onChange(value === '360' ? '' : value)
            }, {
                values: degrees.map(value => value ? String(value) : ''),
                labels: degrees.map(value => `${value}°`),
                ticks: [0, 90, 180, 270, 360], snapTolerance: 5,
                tickLabels: { 0: '↑', 90: '→', 180: '↓', 270: '←', 360: '↑' },
                showPreview: callbacks.showPreview === true, previewOption: 'rotate',
                normalize(value) {
                    const degrees = Math.round(Number(String(value || 0).replace(',', '.')) || 0);
                    return String((degrees % 360 + 360) % 360 || '');
                }
            });
        },
        bindRange(toggle, { getState, onChange, getPlacement }, { values, labels, ticks: tickIndices, normalize, showPreview = false, previewOption = 'stroke-scale', tickLabels, snapTolerance, menuClass = '' }) {
            let menu, popper;
            toggle.setAttribute('aria-haspopup', 'dialog');
            toggle.setAttribute('aria-expanded', 'false');
            function close(restoreFocus = false) {
                if (!menu) return;
                popper?.destroy(); popper = null;
                jQuery(menu).remove(); menu = null;
                document.removeEventListener('mousedown', outside, true);
                toggle.setAttribute('aria-expanded', 'false');
                if (closeCurrent === close) closeCurrent = null;
                if (restoreFocus && toggle.isConnected) toggle.focus();
            }
            function outside(event) {
                if (!toggle.contains(event.target) && !menu?.contains(event.target)) close();
            }
            function open(event) {
                event.preventDefault(); event.stopPropagation();
                if (menu) { close(); return; }
                const state = getState();
                if (!state?.name || toggle.disabled) return;
                closeCurrent?.(); closeCurrent = close;
                const current = normalize(state.value);
                const index = Math.max(0, values.indexOf(current));
                menu = document.createElement('div');
                menu.className = 'dropdown-menu show icon-tool-size-menu px-3 py-2 ' + menuClass;
                menu.setAttribute('role', 'dialog');
                menu.setAttribute('aria-label', toggle.getAttribute('aria-label'));
                const slider = document.createElement('div');
                slider.className = 'range-slider';
                slider.dataset.format = JSON.stringify(labels) + '[{0}]';
                const hidden = document.createElement('input');
                hidden.type = 'hidden'; hidden.id = `icon-size-${++nextToolId}`;
                const input = document.createElement('input');
                input.type = 'range'; input.min = 0; input.max = values.length - 1; input.step = 1; input.value = index;
                input.className = 'form-control-range px-0 custom-range';
                input.dataset.target = '#' + hidden.id;
                input.setAttribute('aria-label', menu.getAttribute('aria-label'));
                input.setAttribute('aria-valuetext', labels[index]);
                const ticks = document.createElement('div');
                ticks.className = 'range-ticks range-ticks-positioned';
                if (tickLabels) ticks.style.fontSize = '14px';
                for (const [i, label] of labels.entries()) {
                    if (tickIndices && !tickIndices.includes(i)) continue;
                    const tick = document.createElement('div');
                    tick.className = 'range-tick'; tick.dataset.value = tickLabels?.[i] || label;
                    tick.style.setProperty('--tick-position', `${i / (values.length - 1) * 100}%`);
                    ticks.append(tick);
                }
                const bubble = document.createElement('div');
                bubble.className = 'range-value';
                bubble.dataset.placement = 'left';
                const arrow = document.createElement('div');
                arrow.className = 'arrow';
                const bubbleText = document.createElement('div');
                bubbleText.className = 'range-value-inner text-nowrap';
                bubble.append(arrow, bubbleText);
                slider.append(hidden, input, bubble, ticks);
                let opacityRow;
                if (menuClass === 'icon-tool-opacity-menu') {
                    // Keep the accessible range interaction, with the color picker's alpha-track appearance.
                    const output = document.createElement('output');
                    output.className = 'text-right small text-muted text-nowrap';
                    menu.style.setProperty('--icon-tool-opacity-color', state.color || 'currentColor');
                    output.textContent = labels[index];
                    input.addEventListener('input', () => output.textContent = labels[Number(input.value)]);
                    opacityRow = document.createElement('div');
                    opacityRow.className = 'd-flex align-items-center gap-2';
                    slider.classList.add('flex-grow-1');
                    slider.style.minWidth = '0';
                    output.classList.add('flex-shrink-0');
                    opacityRow.append(slider, output);
                    menu.append(opacityRow);
                }
                let preview;
                if (showPreview) {
                    const row = opacityRow || document.createElement('div');
                    if (!opacityRow) row.className = 'd-flex align-items-center gap-3';
                    slider.classList.add('flex-grow-1');
                    slider.style.minWidth = '0';
                    const holder = document.createElement('div');
                    holder.className = 'd-flex align-items-center justify-content-center flex-shrink-0';
                    holder.style.width = '48px';
                    preview = document.createElement('sm-icon');
                    const separator = state.name.indexOf('?');
                    const query = separator < 0 ? [] : state.name.slice(separator + 1).split('&')
                        .filter(pair => !['color', 'size', 'animation', previewOption, ...(previewOption === 'opacity' ? ['o'] : [])].includes(pair.split('=', 1)[0]));
                    preview.setAttribute('name', (separator < 0 ? state.name : state.name.slice(0, separator)) + (query.length ? '?' + query.join('&') : ''));
                    if (state.area) preview.setAttribute('area', state.area);
                    for (const key of ['flip-h', 'flip-v', 'rotate', 'stroke-scale']) {
                        if (key !== previewOption && state.options?.[key]) preview.setAttribute(key, String(state.options[key]).replace(',', '.'));
                    }
                    if (previewOption === 'opacity' && state.color) preview.setAttribute('color', state.color);
                    preview.setAttribute('size', '3x');
                    preview.setAttribute('aria-hidden', 'true');
                    if (previewOption === 'rotate') preview.style.setProperty('--icon-rotate', (state.value || 0) + 'deg');
                    else if (state.value) preview.setAttribute(previewOption, String(state.value).replace(',', '.'));
                    holder.append(preview);
                    if (opacityRow) row.append(holder);
                    else { row.append(slider, holder); menu.append(row); }
                } else if (!opacityRow) menu.append(slider);
                const owner = toggle.closest('[data-note-owner]')?.getAttribute('data-note-owner');
                if (owner) menu.setAttribute('data-note-owner', owner);
                addArrow(menu);
                document.body.append(menu);
                jQuery(slider).rangeSlider(snapTolerance == null ? undefined : { snapValues: tickIndices, snapTolerance }).on('rangeslider:change', (event, position) => {
                    const i = Number(position);
                    input.setAttribute('aria-valuetext', labels[i]);
                    if (preview) {
                        if (previewOption === 'rotate') preview.style.setProperty('--icon-rotate', (values[i] || 0) + 'deg');
                        else if (values[i]) preview.setAttribute(previewOption, values[i]);
                        else preview.removeAttribute(previewOption);
                    }
                    onChange(values[i]);
                });
                menu.addEventListener('keydown', event => {
                    if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(true); }
                });
                popper = new Popper(toggle, menu, {
                    placement: getPlacement?.() || 'bottom',
                    modifiers: {
                        offset: { offset: '0, 8' },
                        preventOverflow: { boundariesElement: 'viewport', padding: 8 },
                        flip: { enabled: !getPlacement, boundariesElement: 'viewport' },
                        computeStyle: { gpuAcceleration: false }
                    }
                });
                toggle.setAttribute('aria-expanded', 'true');
                input.focus({ preventScroll: true });
                document.addEventListener('mousedown', outside, true);
            }
            toggle.addEventListener('click', open);
            return { close, contains: node => !!menu?.contains(node), dispose() { close(); toggle.removeEventListener('click', open); } };
        },
        // The host supplies one axis; explicit false also overrides inherited mirroring.
        bindFlip(toggle, { getState, onChange }) {
            function flip(event) {
                event.preventDefault(); event.stopPropagation();
                const state = getState();
                if (toggle.disabled || !state?.name) return;
                onChange(String(state.value).toLowerCase() !== 'true');
            }
            toggle.addEventListener('click', flip);
            return { dispose() { toggle.removeEventListener('click', flip); } };
        },
        // Reuse the application's colorpicker, including its palette and alpha support.
        bindColor(toggle, { getState, onChange, getPlacement }) {
            let menu, picker, popper;
            toggle.setAttribute('aria-haspopup', 'dialog');
            toggle.setAttribute('aria-expanded', 'false');

            function close(restoreFocus = false) {
                if (!menu) return;
                picker.colorpicker('destroy'); picker = null;
                popper?.destroy(); popper = null;
                menu.remove(); menu = null;
                document.removeEventListener('mousedown', outside, true);
                toggle.setAttribute('aria-expanded', 'false');
                if (closeCurrent === close) closeCurrent = null;
                if (restoreFocus && toggle.isConnected) toggle.focus();
            }
            function outside(event) {
                if (!toggle.contains(event.target) && !menu?.contains(event.target)) close();
            }
            function open(event) {
                event.preventDefault(); event.stopPropagation();
                if (menu) { close(); return; }
                const state = getState();
                if (!state?.name || toggle.disabled) return;
                closeCurrent?.();
                closeCurrent = close;
                menu = document.createElement('div');
                menu.className = 'dropdown-menu show icon-tool-color-menu p-0';
                menu.style.visibility = 'hidden';
                menu.setAttribute('role', 'dialog');
                menu.setAttribute('aria-label', toggle.getAttribute('aria-label') || 'Color');
                const owner = toggle.closest('[data-note-owner]')?.getAttribute('data-note-owner');
                if (owner) menu.setAttribute('data-note-owner', owner);
                addArrow(menu);
                document.body.append(menu);
                picker = jQuery(menu);
                picker.colorpicker({
                    inline: true, container: true, popover: false, addon: false,
                    input: false, showInput: true, color: state.value || '',
                    labels: { color: menu.getAttribute('aria-label') }
                });
                picker.on('colorpickerChange.iconTools', event => {
                    const value = event.value || '';
                    if (!value || CSS.supports('color', value)) onChange(value);
                    popper?.scheduleUpdate();
                });
                // The inline picker is visible before measuring the menu.
                picker.one('colorpickerCreate.iconTools', () => {
                    if (!menu) return;
                    const bounds = toggle.getBoundingClientRect();
                    const availableHeight = Math.max(bounds.top, window.innerHeight - bounds.bottom) - 18;
                    // Scroll the picker contents, leaving the outer arrow unclipped.
                    const content = menu.querySelector('.colorpicker');
                    if (content) {
                        content.style.maxHeight = `${Math.max(0, availableHeight)}px`;
                        content.style.overflowY = 'auto';
                    }
                    popper = new Popper(toggle, menu, {
                        placement: getPlacement?.() || 'bottom',
                        onCreate() {
                            if (!menu) return;
                            menu.style.visibility = '';
                            menu.querySelector('.colorpicker-input input')?.focus({ preventScroll: true });
                        },
                        modifiers: {
                            offset: { offset: '0, 8' },
                            preventOverflow: { boundariesElement: 'viewport', padding: 8 },
                            flip: { enabled: !getPlacement, boundariesElement: 'viewport' },
                            computeStyle: { gpuAcceleration: false }
                        }
                    });
                });
                menu.addEventListener('keydown', event => {
                    if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(true); }
                });
                toggle.setAttribute('aria-expanded', 'true');
                document.addEventListener('mousedown', outside, true);
            }
            toggle.addEventListener('click', open);
            return { close, contains: node => !!menu?.contains(node), dispose() { close(); toggle.removeEventListener('click', open); } };
        },
        // getState returns { name, area, options, value }; onChange receives the selected value.
        // The returned close/dispose methods let hosts clean up when their editor closes.
        bindAnimation(toggle, { getState, onChange, getPlacement, noneLabel = 'None' }) {
            let menu, popper;
            toggle.setAttribute('aria-haspopup', 'menu');
            toggle.setAttribute('aria-expanded', 'false');

            function close(restoreFocus = false) {
                if (!menu) return;
                popper?.destroy(); popper = null;
                menu.remove(); menu = null;
                document.removeEventListener('mousedown', outside, true);
                document.removeEventListener('focusin', outside, true);
                toggle.setAttribute('aria-expanded', 'false');
                if (closeCurrent === close) closeCurrent = null;
                if (restoreFocus && toggle.isConnected) toggle.focus();
            }
            function outside(event) {
                if (!toggle.contains(event.target) && !menu?.contains(event.target)) close();
            }
            function open(event) {
                event.preventDefault();
                event.stopPropagation();
                if (menu) { close(); return; }
                const state = getState();
                if (!state?.name || toggle.disabled) return;
                closeCurrent?.();
                closeCurrent = close;
                // Keep animation previews neutral; size, animation and color are not inherited.
                const separator = state.name.indexOf('?');
                const query = separator < 0 ? [] : state.name.slice(separator + 1).split('&')
                    .filter(pair => !['size', 'animation', 'color'].includes(pair.split('=', 1)[0]));
                const name = (separator < 0 ? state.name : state.name.slice(0, separator))
                    + (query.length ? '?' + query.join('&') : '');
                menu = document.createElement('div');
                menu.className = 'dropdown-menu show icon-tool-animation-menu p-2';
                menu.setAttribute('role', 'menu');
                menu.setAttribute('aria-label', toggle.getAttribute('aria-label') || 'Animation');
                const grid = document.createElement('div');
                grid.className = 'icon-tool-animation-grid';
                menu.append(grid);
                const values = ['', ...namespace.Icons.animations];
                for (const value of values) {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.className = 'btn btn-plain rounded shadow-none d-flex flex-column align-items-center justify-content-center gap-1 p-1';
                    button.setAttribute('role', 'menuitemradio');
                    button.setAttribute('aria-checked', String(value === (state.value || '')));
                    button.classList.toggle('active', value === (state.value || ''));
                    button.tabIndex = value === (state.value || '') ? 0 : -1;
                    const icon = document.createElement('sm-icon');
                    icon.setAttribute('name', name);
                    if (state.area) icon.setAttribute('area', state.area);
                    for (const [key, setting] of Object.entries(state.options || {})) {
                        if (setting != null && !['name', 'size', 'animation', 'color', 'font-scale', 'scale'].includes(key)) icon.setAttribute(key, setting);
                    }
                    if (value) icon.setAttribute('animation', value);
                    icon.setAttribute('aria-hidden', 'true');
                    const label = document.createElement('span');
                    label.className = 'fs-xs text-truncate mw-100';
                    label.textContent = value || noneLabel;
                    button.append(icon, label);
                    button.addEventListener('click', () => {
                        close();
                        onChange(value);
                        if (toggle.isConnected) toggle.focus();
                    });
                    grid.append(button);
                }
                menu.addEventListener('keydown', event => {
                    if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(true); return; }
                    if (event.key === 'Tab') { close(true); return; }
                    const buttons = [...menu.querySelectorAll('[role="menuitemradio"]')];
                    const index = buttons.indexOf(document.activeElement);
                    const rtl = getComputedStyle(menu).direction === 'rtl';
                    const step = { ArrowRight: rtl ? -1 : 1, ArrowLeft: rtl ? 1 : -1, ArrowDown: 3, ArrowUp: -3 }[event.key];
                    if (step === undefined && event.key !== 'Home' && event.key !== 'End') return;
                    event.preventDefault(); event.stopPropagation();
                    const next = event.key === 'Home' ? 0 : event.key === 'End' ? buttons.length - 1 : (index + step + buttons.length) % buttons.length;
                    buttons.forEach((button, i) => button.tabIndex = i === next ? 0 : -1);
                    buttons[next].focus();
                });
                const owner = toggle.closest('[data-note-owner]')?.getAttribute('data-note-owner');
                if (owner) menu.setAttribute('data-note-owner', owner);
                addArrow(menu);
                document.body.append(menu);
                popper = new Popper(toggle, menu, {
                    placement: getPlacement?.() || 'bottom',
                    modifiers: {
                        offset: { offset: '0, 8' },
                        preventOverflow: { boundariesElement: 'viewport', padding: 8 },
                        flip: { enabled: !getPlacement, boundariesElement: 'viewport' },
                        computeStyle: { gpuAcceleration: false }
                    }
                });
                toggle.setAttribute('aria-expanded', 'true');
                menu.querySelector('[tabindex="0"]')?.focus();
                document.addEventListener('mousedown', outside, true);
                document.addEventListener('focusin', outside, true);
            }
            toggle.addEventListener('click', open);
            return {
                close,
                contains: node => !!menu?.contains(node),
                dispose() { close(); toggle.removeEventListener('click', open); }
            };
        }
    };
})();
