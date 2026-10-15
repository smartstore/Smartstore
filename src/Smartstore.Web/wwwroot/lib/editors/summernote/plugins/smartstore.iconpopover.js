// Smartstore icon editing; Summernote only supplies component selection and history.
(function ($) {
    'use strict';
    $.summernote.plugins.iconPopover = function (context) {
        const settings = document.getElementById('summernote-iconpicker-settings')?.dataset;
        if (!settings) return;
        let target, editor, popover;
        const tools = [];
        const buttons = new Map();
        const editable = context.layoutInfo.editable[0];
        const owns = node => !!node && (popover?.[0].contains(node) || tools.some(tool => tool.contains?.(node)));

        function hide() {
            tools.forEach(tool => tool.close?.());
            if (popover) editor.hidePopover(popover);
            target = null;
        }
        function createAnchor(node) {
            const rect = node.getBoundingClientRect();
            const doc = node.ownerDocument;
            const view = doc.defaultView;
            const parents = [];
            for (let parent = node.parentElement; parent; parent = parent.parentElement) {
                if (parent !== doc.scrollingElement) parents.push(parent);
            }
            const scroll = () => parents.reduce((position, parent) => ({
                x: position.x + parent.scrollLeft, y: position.y + parent.scrollTop
            }), { x: view.scrollX, y: view.scrollY });
            const initialScroll = scroll();
            // Keep the selection's original geometry; only scrolling moves the anchor.
            return {
                referenceNode: node, parentNode: node.parentNode, ownerDocument: doc,
                clientWidth: rect.width, clientHeight: rect.height,
                getBoundingClientRect() {
                    const current = scroll();
                    const left = rect.left + initialScroll.x - current.x;
                    const top = rect.top + initialScroll.y - current.y;
                    return { left, top, right: left + rect.width, bottom: top + rect.height,
                        width: rect.width, height: rect.height };
                }
            };
        }
        function value(option) {
            if (!target) return '';
            if (target.hasAttribute(option)) return target.getAttribute(option);
            const name = target.getAttribute('name') || '';
            const separator = name.indexOf('?');
            const query = new URLSearchParams(separator < 0 ? '' : name.slice(separator + 1));
            if (option.startsWith('flip-')) {
                const flip = query.get('flip');
                if (flip !== null) return String(flip.includes(option === 'flip-h' ? 'x' : 'y'));
                const axis = option === 'flip-h' ? 'x' : 'y';
                const style = getComputedStyle(target);
                return String((style.getPropertyValue('--icon-flip-' + axis).trim()
                    || style.getPropertyValue('--_icon-flip-' + axis).trim()) === '-1');
            }
            return query.get(option) || '';
        }
        function state(option) {
            const options = {};
            for (const key of ['flip-h', 'flip-v', 'rotate', 'stroke-scale']) {
                const setting = value(key);
                if (setting !== '') options[key] = setting;
            }
            return { name: target?.getAttribute('name'), area: target?.getAttribute('area') || 'public',
                options, value: value(option) };
        }
        function refresh() {
            for (const [option, button] of buttons) {
                const current = value(option);
                if (option === 'stroke-scale') button.classList.toggle('d-none', !target?.querySelector('svg[data-supports-stroke-scaling="true"]'));
                const active = option.startsWith('flip-') ? current.toLowerCase() === 'true' : !!current;
                button.classList.toggle('active', active);
                if (option.startsWith('flip-')) button.setAttribute('aria-pressed', String(active));
                if (option === 'color') button.firstElementChild.style.backgroundColor = current || '#fff';
            }
        }
        function removeIcon() {
            if (!target || !editable.contains(target) || context.isDisabled()) return hide();
            const icon = target;
            editor.history.recordUndo();
            editor.beforeCommand();
            const selection = editable.ownerDocument.createRange();
            selection.setStartBefore(icon);
            selection.collapse(true);
            icon.remove();
            editor.selection.setRange($.summernote.range.createFromNativeRange(selection));
            context.invoke('componentSelection.hide');
            hide();
            editor.afterCommand();
        }
        function change(option, setting) {
            if (!target || !editable.contains(target) || context.isDisabled()) return hide();
            const icon = target;
            const focused = owns(document.activeElement) ? document.activeElement : null;
            // Record authored markup on both sides of the change, just like editor commands.
            editor.history.recordUndo();
            const selection = editable.ownerDocument.createRange();
            selection.selectNode(icon);
            editor.selection.setRange($.summernote.range.createFromNativeRange(selection));
            editor.beforeCommand();
            if (setting === '') {
                icon.removeAttribute(option);
                const name = icon.getAttribute('name') || '';
                const separator = name.indexOf('?');
                if (separator >= 0) {
                    const query = name.slice(separator + 1).split('&').filter(pair => pair.split('=', 1)[0] !== option);
                    icon.setAttribute('name', name.slice(0, separator) + (query.length ? '?' + query.join('&') : ''));
                }
            }
            else icon.setAttribute(option, String(setting));
            editor.afterCommand();
            refresh();
            popover.data('popper')?.scheduleUpdate();
            context.invoke('componentSelection.position');
            if (focused?.isConnected) focused.focus({ preventScroll: true });
        }
        this.events = {
            'summernote.component.selected': (event, node) => {
                if (node.localName !== 'sm-icon') return hide();
                tools.forEach(tool => tool.close?.());
                target = node;
                refresh();
                editor.showPopover(popover, createAnchor(target));
            },
            'summernote.codeview.toggled summernote.disable summernote.dialog.shown': hide,
            'summernote.popover.shown': (event, other) => { if (other !== popover) hide(); },
            'summernote.change': () => { if (target && !editable.contains(target)) hide(); },
            'summernote.scroll': () => popover?.data('popper')?.scheduleUpdate(),
            'summernote.blur': (event, original) => {
                const next = original?.originalEvent?.relatedTarget || original?.relatedTarget;
                if (!owns(next)) hide();
            }
        };
        function outside(event) {
            if (!owns(event.target) && !target?.contains(event.target)) hide();
        }
        function iconLoaded(event) {
            if (event.target !== target) return;
            refresh();
            popover.data('popper')?.scheduleUpdate();
        }
        function hideOnKey(event) {
            if (['Escape', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Backspace', 'Delete'].includes(event.key)) hide();
        }
        this.initialize = function () {
            editor = context.modules.editor;
            popover = $.summernote.ui.popover({ className: 'note-icon-popover' }).render().appendTo(context.options.container);
            const content = popover.find('.popover-content,.note-popover-content');
            const group = $.summernote.ui.toolGroup({ className: 'note-icon-tools' }).render()
                .attr({ role: 'group', 'aria-label': settings.label }).appendTo(content);
            for (const [option, motif, label] of [
                ['color', null, settings.color], ['flip-h', 'flip-h', settings.flipHorizontal],
                ['flip-v', 'flip-v', settings.flipVertical], ['rotate', 'rotate', settings.rotation], ['size', 'scaling', settings.size], ['animation', 'animation', settings.animation], ['stroke-scale', 'hi:pencil', settings.strokeWidth]
            ]) {
                const button = $.summernote.ui.button({}).render().attr({ title: label, 'aria-label': label })[0];
                const symbol = document.createElement(motif ? 'sm-icon' : 'span');
                if (motif) {
                    symbol.setAttribute('name', motif);
                    symbol.setAttribute('area', 'admin');
                } else symbol.className = 'icon-tool-color-swatch';
                symbol.setAttribute('aria-hidden', 'true');
                button.append(symbol);
                group.append(button);
                buttons.set(option, button);
                const bind = option === 'color' ? 'bindColor' : option === 'animation' ? 'bindAnimation' : option === 'rotate' ? 'bindRotate' : option === 'size' ? 'bindSize' : option === 'stroke-scale' ? 'bindStrokeScale' : 'bindFlip';
                tools.push(Smartstore.IconTools[bind](button, {
                    getState: () => state(option), onChange: setting => change(option, setting),
                    noneLabel: settings.noAnimation,
                    getPlacement: () => (popover.attr('x-placement') || 'top').split('-')[0]
                }));
            }
            const removeGroup = $.summernote.ui.toolGroup({ className: 'note-icon-remove' }).render().appendTo(content);
            const removeButton = $.summernote.ui.button({
                click: removeIcon
            }).render().attr({ title: settings.delete, 'aria-label': settings.delete });
            const trash = document.createElement('sm-icon');
            trash.setAttribute('name', 'trash');
            trash.setAttribute('area', 'admin');
            trash.setAttribute('aria-hidden', 'true');
            removeButton.append(trash).appendTo(removeGroup);
            popover.on('mousedown', event => event.preventDefault());
            document.addEventListener('mousedown', outside, true);
            editable.addEventListener('keydown', hideOnKey, true);
            editable.addEventListener('icon-load', iconLoaded);
        };
        this.destroy = function () {
            hide();
            tools.forEach(tool => tool.dispose());
            document.removeEventListener('mousedown', outside, true);
            editable.removeEventListener('keydown', hideOnKey, true);
            editable.removeEventListener('icon-load', iconLoaded);
            popover?.remove();
        };
    };
})(jQuery);
