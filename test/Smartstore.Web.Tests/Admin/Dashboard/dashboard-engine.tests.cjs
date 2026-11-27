const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const context = { Smartstore: { Admin: {} }, jQuery() {} };
vm.createContext(context);
vm.runInContext(
    fs.readFileSync(
    path.resolve(__dirname, '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.editor.js'),
    'utf8'),
    context);
const Engine = context.Smartstore.Admin.DashboardEngine;
const item = (id, column, row, width, extra = {}) => ({ id, column, row, size: { columnSpan: width, rowSpan: 1 }, ...extra });
test('moving into an occupied cell pushes the other card to the next row', () => {
    const items = [item('a', 0, 1, 6), item('b', 0, 1, 6), item('c', 6, 1, 6)];
    Engine.arrange(items, 12, 'a');
    assert.deepEqual(items.map(x => x.row), [1, 2, 1]);
});
test('reserved sidebar columns cannot be occupied', () => {
    const items = [item('a', 9, 0, 6)];
    Engine.arrange(items, 10, 'a');
    assert.equal(items[0].column, 4);
});
test('locked cards are preserved when a movable card collides', () => {
    const items = [item('fixed', 0, 0, 6, { locked: true }), item('a', 0, 0, 6)];
    Engine.arrange(items, 12, 'a');
    assert.deepEqual(items.map(x => x.row), [0, 1]);
});
test('row spans prevent collisions with following rows', () => {
    const items = [item('a', 0, 0, 6, { size: { columnSpan: 6, rowSpan: 2 } }), item('b', 0, 1, 6)];
    Engine.arrange(items, 12);
    assert.equal(items[1].row, 2);
});
test('repeated arrangement remains stable', () => {
    const items = [item('a', 0, 0, 8), item('b', 4, 0, 6), item('c', 0, 2, 12)];
    Engine.arrange(items, 12, 'a');
    const before = JSON.stringify(items);
    Engine.arrange(items, 12);
    assert.equal(JSON.stringify(items), before);
});

test('resizing from the start keeps the opposite edge fixed', () => {
    const card = item('a', 4, 1, 4);
    Engine.resizeStart(card, 2, 3, 10);
    assert.equal(card.column, 2);
    assert.equal(card.size.columnSpan, 6);
    assert.equal(card.column + card.size.columnSpan, 8);
    Engine.resizeStart(card, 5, 3, 10);
    assert.equal(card.column, 5);
    assert.equal(card.size.columnSpan, 3);
    assert.equal(card.row, 1);
});
test('start resize respects both size limits and the grid boundary', () => {
    for (const [column, expectedStart, expectedWidth] of [[-10, 2, 6], [20, 5, 3]]) {
        const card = item('a', 4, 0, 4);
        Engine.resizeStart(card, column, 3, 6);
        assert.equal(card.column, expectedStart);
        assert.equal(card.size.columnSpan, expectedWidth);
    }

    const card = item('a', 2, 0, 4);
    Engine.resizeStart(card, -10, 3, 12);
    assert.equal(card.column, 0);
    assert.equal(card.size.columnSpan, 6);
});
test('expanding left displaces a neighbor without moving the opposite edge', () => {
    const neighbor = item('neighbor', 0, 0, 4);
    const card = item('a', 4, 0, 4);
    Engine.resizeStart(card, 2, 3, 10);
    Engine.arrange([neighbor, card], 10, card.id);
    assert.equal(card.column + card.size.columnSpan, 8);
    assert.equal(card.row, 0);
    assert.equal(neighbor.row, 1);
});

// Run the editor with a minimal DOM and the bundled Underscore.
function createEditor({ deferFade = false, reducedMotion = false, required = false, direction = 'ltr', blockedStorage = false, allowedSizes = [], layer = 'User', belowPosition, news = false, breakpoint = 'xl', dirty = false, storage = new Map() } = {}) {
    let finishFade, cancelFade;
    const animations = [];
    const handlers = new Map();
    const controls = new Map();
    const element = {
        dataset: { instanceId: 'a', minWidth: '2', maxWidth: '6' },
        style: {}, isConnected: true, offsetHeight: 300,
        remove() { this.isConnected = false; },
        getAnimations: () => [],
        animate(frames, options) {
            animations.push({ id: this.dataset.instanceId, frames, options });
            return {
                finished: deferFade && options.duration === 140
                    ? new Promise((resolve, reject) => { finishFade = resolve; cancelFade = reject; })
                    : Promise.resolve()
            };
        },
        classList: { contains: () => false, add() {}, remove() {} },
        getBoundingClientRect: () => ({ left: 0, top: 0, bottom: 300, height: 300 }),
        querySelector: selector => selector.includes('resize') || selector === '.dashboard-widget-overlay' ? {} : null,
        querySelectorAll: () => []
    };
    const belowElement = { ...element, dataset: { ...element.dataset, instanceId: 'b' }, style: {},
        querySelector: element.querySelector, getBoundingClientRect: () => ({ top: 300, bottom: 500, height: 200 }) };
    const newsElement = { ...element, dataset: { ...element.dataset, instanceId: 'news' }, style: {}, querySelector: () => null };
    const previewWidths = { xs: 375, sm: 576, md: 768, lg: 992, xl: 1200, xxl: 1400 };
    const viewport = {
        style: {}, classList: { remove() {} }, parentElement: { clientWidth: previewWidths[breakpoint] },
        getBoundingClientRect: () => ({ width: Math.min(parseFloat(viewport.style.maxWidth) || Infinity, viewport.parentElement.clientWidth) })
    };
    const breakpointButtons = ['', ...Object.keys(previewWidths)].map(tier => ({
        dataset: { dashboardBreakpoint: tier }, children: [{ cloneNode: () => ({ textContent: tier.toUpperCase() || 'Automatic' }) }], classList: { toggle() {} },
        setAttribute(name, value) { this[name] = value; },
        addEventListener(name, callback) { this[name] = callback; }
    }));
    const breakpointToggle = {
        replaceChildren(label) { this.label = label; },
        querySelector() { return this.label; }
    };
    let resize;
    const container = {
        parentElement: viewport,
        style: {},
        appendChild(el) { el.isConnected = true; },
        querySelectorAll: selector => selector === '[data-instance-id]' ? [element, ...(belowPosition ? [belowElement] : []), ...(news ? [newsElement] : [])] : [],
        addEventListener: (name, callback) => handlers.set(name, [callback]),
        getBoundingClientRect: () => ({ left: 0, right: 1200, top: 0 })
    };
    const scrollContainer = { scrollTop: 0, getBoundingClientRect: () => ({ top: 0, bottom: 900 }) };
    const config = {
        layout: { id: 'dashboard', columnCount: 12, rules: ['xs', 'sm', 'md', 'lg', 'xl', 'xxl'].map(breakpoint => ({ breakpoint, columnCount: 12, fullWidthWidgets: ['xs', 'sm'].includes(breakpoint), hasNewsRail: false })), widgets: [
            { id: 'a', widgetSystemName: 'Test', allowMove: true, isRequired: required,
                positions: [{ breakpoint: 'xs', column: 2, row: 1, size: { columnSpan: 4, rowSpan: 1 } }] }
        ] },
        maxHeight: 310, widgets: [{ systemName: 'Test', allowedSizes }], layer, dirty, editUrl: '/admin?edit=true', cancelUrl: '/admin'
    };

    if (belowPosition) {
        config.layout.widgets.push({ id: 'b', widgetSystemName: 'Test', allowMove: true,
            positions: [{ breakpoint: 'xs', column: 2, row: 2, size: { columnSpan: 4, rowSpan: 1 }, minHeight: 200, ...belowPosition }] });
    }

    if (news) {
        config.layout.widgets.push({ id: 'news', widgetSystemName: 'Smartstore.Admin.Dashboard.NewsFeed', allowMove: false,
            positions: [
                { breakpoint: 'xs', column: 0, row: 7, size: { columnSpan: 12, rowSpan: 1 } },
                { breakpoint: 'lg', column: 10, row: 0, size: { columnSpan: 2, rowSpan: 6 } }
            ] });

        for (const rule of config.layout.rules) {
            rule.hasNewsRail = ['lg', 'xl', 'xxl'].includes(rule.breakpoint);
            rule.columnCount = rule.hasNewsRail ? 10 : 12;
        }
    }

    let confirmation;
    let submissions = 0;
    const navigations = [];
    const form = {
        action: 'https://example.test/admin/Home/DashboardSave',
        elements: Object.fromEntries(['changesJson', 'reset', 'command', 'widgetSystemName', 'save'].map(name => [name, { value: '' }]))
    };
    const document = {
        body: { style: {}, classList: { add() {}, remove() {} }, appendChild() {} },
        createElement: () => ({ style: {}, setAttribute() {}, remove() {}, appendChild() {}, replaceChildren() {} }),
        getElementById: id => ({
            'dashboard-editor-data': { textContent: JSON.stringify(config) },
            'dashboard-grid': container,
            content: scrollContainer,
            'dashboard-draft-form': form,
            'dashboard-breakpoint-toggle': breakpointToggle
        })[id],
        querySelectorAll: selector => selector === '[data-dashboard-breakpoint]' ? breakpointButtons : [],
        addEventListener: (name, callback) => {
            const list = handlers.get(name) || [];
            list.push(callback);
            handlers.set(name, list);
        }
    };
    function jquery(selector) {
        if (typeof selector === 'function') { selector(); return; }

        if (!controls.has(selector)) {
            const control = {
                disabled: false,
                prop(name, value) { this[name] = value; return this; },
                text(value) { this.label = value; return this; },
                attr(name, value) { this[name] = value; return this; },
                toggleClass() { return this; },
                on(name, selector, callback) { this[callback ? selector : name] = callback || selector; return this; }
            };
            controls.set(selector, control);
        }

        return controls.get(selector);
    }
    const sandbox = {
        Smartstore: { Admin: {} }, jQuery: jquery, document, structuredClone, URL,
        HTMLFormElement: { prototype: { submit() { submissions++; } } },
        confirm2(options) { confirmation = options; return { find: () => ({ text() {} }) }; },
        window: { matchMedia: () => ({ matches: reducedMotion }), innerWidth: 1200, innerHeight: 900, scrollY: 0, location: { href: 'https://example.test/admin?dashboardHistory=test', assign(url) { navigations.push(url); } },
            Res: { Dashboard: { cancel: 'Cancel' } }, history: { replaceState() {} }, addEventListener() {} },
        sessionStorage: {
            getItem(key) {
                if (blockedStorage) throw new Error('Storage blocked');
                return storage.get(key) ?? null;
            },
            setItem(key, value) {
                if (blockedStorage) throw new Error('Storage blocked');
                storage.set(key, value);
            },
            removeItem(key) {
                if (blockedStorage) throw new Error('Storage blocked');
                storage.delete(key);
            }
        },
        ResizeObserver: class { constructor(callback) { resize = callback; } observe() {} },
        getComputedStyle: el => ({
            minHeight: '64px', direction, paddingLeft: '0px', paddingRight: '0px',
            gridTemplateRows: '150px 150px 150px 150px', gridTemplateColumns: Array(12).fill(100).join(' '),
            getPropertyValue(name) {
                if (name === '--dashboard-preview-width') return previewWidths[el.dataset?.dashboardBreakpoint] ? previewWidths[el.dataset.dashboardBreakpoint] + 'px' : '';
                if (name === '--dashboard-breakpoint') return viewport.style.maxWidth
                    ? Object.keys(previewWidths).find(tier => previewWidths[tier] === parseFloat(viewport.style.maxWidth))
                    : Object.keys(previewWidths).filter(tier => previewWidths[tier] <= viewport.parentElement.clientWidth).pop() || 'xs';
                return '';
            }
        }),
        requestAnimationFrame: callback => { sandbox.frame = callback; return 1; },
        cancelAnimationFrame() {}
    };
    vm.createContext(sandbox);
    vm.runInContext(
        fs.readFileSync(path.resolve(__dirname, '../../../../src/Smartstore.Web/wwwroot/lib/underscore/underscore.js'), 'utf8'),
        sandbox);
    vm.runInContext(
        fs.readFileSync(
        path.resolve(
        __dirname,
        '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.editor.js'),
        'utf8'),
        sandbox);
    return {
        animations,
        finishFade: () => finishFade(),
        cancelFade: () => cancelFade(new Error('Animation cancelled')),
        remove() { controls.get(container)['.dashboard-remove'].call({ closest: () => element }); },
        hide() { return controls.get(container)['.dashboard-hide'].call({ closest: () => element }); },
        show() { controls.get('[data-show-widget]').click.call({ dataset: { showWidget: 'a' } }); },
        selectBreakpoint: tier => breakpointButtons.find(button => button.dataset.dashboardBreakpoint === tier).click(),
        previewWidth: () => viewport.style.maxWidth,
        previewLabel: () => breakpointToggle.label.textContent,
        resize(width) { viewport.parentElement.clientWidth = width; resize(); },
        breakpointDisabled: tier => breakpointButtons.find(button => button.dataset.dashboardBreakpoint === tier).disabled,
        style: element.style,
        belowStyle: belowElement.style,
        newsStyle: newsElement.style,
        scrollContainer,
        save: () => controls.get(form).submit({ preventDefault() {} }),
        confirm: accepted => confirmation.callback(accepted),
        hasConfirmation: () => !!confirmation,
        submissions: () => submissions,
        submitted: () => JSON.parse(form.elements.changesJson.value),
        undo: () => controls.get('#dashboard-undo').click(),
        redo: () => controls.get('#dashboard-redo').click(),
        cancel: () => controls.get('#dashboard-cancel').click(),
        canCancel: () => !controls.get('#dashboard-cancel').disabled,
        navigations,
        changeLayer: layer => controls.get('[data-dashboard-edit]').click.call({ dataset: { dashboardEdit: layer } }),
        canSave: () => !controls.get(form.elements.save).disabled,
        reset: () => controls.get('#dashboard-reset').click(),
        resetSubmitted: () => form.elements.reset.value,
        action: () => form.action,
        canUndo: () => !controls.get('#dashboard-undo').disabled,
        drag(kind, ...points) {
            const handle = { dataset: { dashboardResize: kind }, closest: () => element };
            handlers.get('pointerdown')[0]({ clientX: 300, clientY: 300,
                target: { closest: () => handle }, preventDefault() {} });

            for (const point of points) {
                if (point.scrollTop !== undefined) scrollContainer.scrollTop = point.scrollTop;

                handlers.get('pointermove')[0]({ clientX: 300, clientY: 300, ...point });
                sandbox.frame();
                point.inspect?.();
            }

            handlers.get(points.at(-1)?.cancel ? 'pointercancel' : 'pointerup')[0]();
        },
        dragHeight(...heights) {
            const handle = { dataset: { dashboardResize: 'height' }, closest: () => element };
            handlers.get('pointerdown')[0]({ clientX: 300, clientY: 300,
                target: { closest: () => handle }, preventDefault() {} });

            for (const clientY of heights) {
                handlers.get('pointermove')[0]({ clientX: 300, clientY });
                sandbox.frame();
            }

            handlers.get('pointerup')[0]();
        }
    };
}

// A gap is only converted into grid rows when the drop is committed.
test('pointer resizing uses only allowed widths and keeps the opposite edge fixed', () => {
    const allowedSizes = [{ columnSpan: 4, rowSpan: 1 }, { columnSpan: 6, rowSpan: 1 }];

    for (const direction of ['ltr', 'rtl']) {
        const editor = createEditor({ allowedSizes, direction });
        editor.drag('width', { clientX: direction === 'ltr' ? 510 : 90 });
        assert.equal(editor.style.gridColumn, '3 / span 6');
        editor.undo();
        editor.drag('width-start', { clientX: direction === 'ltr' ? 90 : 510 });
        assert.equal(editor.style.gridColumn, '1 / span 6');
    }
});

test('personal dashboard saves changes without a confirmation', () => {
    const editor = createEditor();
    assert.equal(editor.canSave(), false);
    assert.equal(editor.canCancel(), true);
    editor.save();
    assert.equal(editor.submissions(), 0);
    editor.drag('width', { clientX: 410 });
    assert.equal(editor.canSave(), true);
    assert.equal(editor.canCancel(), true);
    editor.undo();
    assert.equal(editor.canSave(), false);
    assert.equal(editor.canCancel(), true);
    editor.redo();
    assert.equal(editor.canSave(), true);
    assert.equal(editor.canCancel(), true);
    editor.save();
    assert.equal(editor.hasConfirmation(), false);
    assert.equal(editor.submissions(), 1);
});

test('global dashboard only saves after confirmation and can be cancelled', () => {
    const editor = createEditor({ layer: 'Global' });
    editor.drag('width', { clientX: 410 });
    editor.save();
    assert.equal(editor.hasConfirmation(), true);
    assert.equal(editor.submissions(), 0);
    editor.confirm(false);
    assert.equal(editor.submissions(), 0);
    editor.save();
    editor.confirm(true);
    assert.equal(editor.submissions(), 1);
});

test('extend reuses only the immediate free cells without moving following widgets', () => {
    const items = [item('a', 0, 0, 6, { resizable: true }), item('b', 6, 0, 6), item('c', 6, 1, 6), item('d', 0, 2, 12)];
    const before = JSON.stringify(items);
    const proposed = Engine.extend(items, 'a', 3);
    assert.equal(proposed[0].size.rowSpan, 2);
    assert.deepEqual(Array.from(proposed, x => x.row), [0, 0, 1, 2]);
    assert.equal(JSON.stringify(items), before);
});

test('partially occupied cells insert a row and extend only neighbors crossing its boundary', () => {
    const items = [item('a', 0, 0, 6, { resizable: true }),
        item('crossing', 6, 0, 6, { resizable: true, size: { columnSpan: 6, rowSpan: 3 } }),
        item('occupied', 4, 1, 2), item('below', 0, 3, 12)];
    const proposed = Engine.extend(items, 'a', 4);
    assert.deepEqual(Array.from(proposed, x => x.row), [0, 0, 2, 4]);
    assert.equal(proposed[0].size.rowSpan, 2);
    assert.equal(proposed[1].size.rowSpan, 4);
});

test('a neighbor ending at the insertion boundary keeps its span', () => {
    const proposed = Engine.extend([item('a', 0, 0, 6, { resizable: true }), item('b', 6, 0, 6)], 'a', 1);
    assert.equal(proposed[1].size.rowSpan, 1);
    assert.equal(proposed[0].size.rowSpan, 2);
});

test('insertion respects locked placements and row-span limits atomically', () => {
    for (const restriction of [{ locked: true }, { row: 0, resizable: false, size: { columnSpan: 6, rowSpan: 3 } },
        { row: 0, resizable: true, maxSize: { rowSpan: 3 }, size: { columnSpan: 6, rowSpan: 3 } }]) {
        const items = [item('a', 0, 0, 6, { resizable: true }), item('blocker', 0, 1, 6, restriction)];
        const before = JSON.stringify(items);
        assert.equal(Engine.extend(items, 'a', 3), null);
        assert.equal(JSON.stringify(items), before);
    }

    assert.equal(Engine.extend([item('a', 0, 0, 6, { resizable: true, allowedSizes: [{ columnSpan: 6, rowSpan: 1 }] })], 'a', 1), null);
});

test('resize releases occupied rows without compacting and cannot cover another widget', () => {
    const items = [item('a', 0, 0, 6, { resizable: true, size: { columnSpan: 6, rowSpan: 2 } }), item('b', 0, 2, 6)];
    const proposed = Engine.resizeRows(items, 'a', 1);
    assert.equal(proposed[0].size.rowSpan, 1);
    assert.equal(proposed[1].row, 2);
    assert.equal(Engine.resizeRows(items, 'a', 3), null);
    Engine.compact(proposed);
    assert.equal(proposed[1].row, 1);
});

test('save compaction retains rows occupied by spanning widgets and respects locked anchors', () => {
    const items = [item('a', 0, 0, 6, { size: { columnSpan: 6, rowSpan: 3 } }), item('b', 6, 2, 6), item('c', 0, 5, 12)];
    Engine.compact(items);
    assert.deepEqual(items.map(x => x.row), [0, 2, 3]);
    const locked = [item('a', 0, 3, 6, { locked: true }), item('b', 6, 6, 6)];
    Engine.compact(locked);
    assert.deepEqual(locked.map(x => x.row), [3, 4]);
});

test('pointer resizing snaps to rows and undo restores the row span', () => {
    const editor = createEditor({ belowPosition: { column: 6 } });
    editor.dragHeight(450);
    assert.equal(editor.style.gridRow, '2 / span 2');
    editor.undo();
    assert.equal(editor.style.gridRow, '2 / span 1');
    editor.redo();
    editor.dragHeight(300);
    assert.equal(editor.style.gridRow, '2 / span 1');
});

test('saving compacts every responsive layout without removing occupied spans', () => {
    const editor = createEditor({ dirty: true });
    assert.equal(editor.style.gridRow, '2 / span 1');
    editor.save();

    for (const p of editor.submitted().widgets[0].positions) assert.equal(p.row, 0);
});


test('adjacent widgets exchange order in both directions without changing their sizes', () => {
    for (const reverse of [false, true]) {
        const original = item('a', reverse ? 6 : 0, 0, 4);
        const moved = item('a', reverse ? 4 : 2, 0, 4);
        const neighbor = item('b', reverse ? 0 : 4, 0, 6);
        const below = item('c', 0, 1, 10);
        Engine.move([moved, neighbor, below], 10, 'a', original);
        assert.equal(moved.column, reverse ? 0 : 6);
        assert.equal(neighbor.column, reverse ? 4 : 0);
        assert.equal(moved.row, 0);
        assert.equal(neighbor.row, 0);
        assert.equal(below.row, 1);
        assert.equal(moved.size.columnSpan, 4);
        assert.equal(neighbor.size.columnSpan, 6);
    }
});

test('diagonal swap falls back to displacement when the replacement would hit other widgets', () => {
    const original = item('top', 0, 2, 4, { size: { columnSpan: 4, rowSpan: 2 } });
    const moved = { ...original, column: 4, row: 0 };
    const target = item('incomplete', 4, 0, 6, { size: { columnSpan: 6, rowSpan: 2 } });
    const registered = item('registered', 4, 2, 6);
    const orders = item('orders', 4, 3, 6);
    Engine.move([moved, target, registered, orders], 10, 'top', original);
    assert.deepEqual([moved.row, target.row, registered.row, orders.row], [0, 2, 4, 5]);
    assert.equal(target.column, 4);
});

test('diagonal swap preserves spanning widgets when both destinations fit', () => {
    const original = item('a', 0, 2, 4, { size: { columnSpan: 4, rowSpan: 2 } });
    const moved = { ...original, column: 4, row: 0 };
    const target = item('b', 4, 0, 4, { size: { columnSpan: 4, rowSpan: 2 } });
    Engine.move([moved, target], 10, 'a', original);
    assert.deepEqual([moved.column, moved.row, target.column, target.row], [4, 0, 0, 2]);
});

test('moving over a locked widget preserves its placement', () => {
    const moved = item('a', 0, 0, 4);
    const fixed = item('fixed', 0, 0, 6, { locked: true });
    Engine.move([moved, fixed], 10, 'a', item('a', 6, 0, 4));
    assert.deepEqual([fixed.column, fixed.row, moved.row], [0, 0, 1]);
});


test('pointer swap keeps the grid stable until drop and cancellation restores the draft', () => {
    const editor = createEditor({ belowPosition: { row: 0 } });
    const before = { ...editor.style };
    const neighbor = { ...editor.belowStyle };
    const inspect = () => {
        assert.deepEqual(editor.style, before);
        assert.deepEqual(editor.belowStyle, neighbor);
    };
    editor.drag(undefined, { clientY: 100, inspect, cancel: true });
    inspect();
    assert.equal(editor.canUndo(), false);
    editor.drag(undefined, { clientY: 100, inspect });
    assert.equal(editor.style.gridRow, '1 / span 1');
    assert.equal(editor.belowStyle.gridRow, '2 / span 1');
    editor.undo();
    inspect();
});

test('moving onto multiple widgets displaces them without overlaps', () => {
    const original = item('a', 0, 2, 8);
    const moved = { ...original, row: 0 };
    const items = [moved, item('b', 0, 0, 4), item('c', 4, 0, 4), item('d', 0, 1, 8)];
    Engine.move(items, 8, 'a', original);
    assert.deepEqual(items.map(x => x.row), [0, 1, 1, 2]);

    for (const widget of items) assert.equal(Engine.fits(items, widget, 8), true);
});

test('free destinations stay free placements and full-width widgets can exchange rows', () => {
    const moved = item('a', 0, 2, 4);
    const neighbor = item('b', 0, 1, 4);
    Engine.move([moved, neighbor], 4, 'a', item('a', 0, 0, 4));
    assert.deepEqual([moved.row, neighbor.row], [2, 1]);
    moved.row = 1;
    Engine.move([moved, neighbor], 4, 'a', item('a', 0, 2, 4));
    assert.deepEqual([moved.row, neighbor.row], [1, 2]);
});

for (const outcome of ['configuration', 'validation', 'configuration-error', 'load', 'load-render-error', 'load-notification-error']) {
    test(`widget request handles ${outcome} without a second request`, () => {
        const loading = outcome.startsWith('load');
        function deferred() {
            const done = [], fail = [], always = [];
            let state = 'pending';
            return {
                done(fn) { done.push(fn); return this; },
                fail(fn) { fail.push(fn); return this; },
                always(fn) { always.push(fn); return this; },
                promise() { return this; },
                state() { return state; },
                resolve(value) { state = 'resolved'; done.forEach(fn => fn(value)); always.forEach(fn => fn(value)); },
                reject(value) { state = 'rejected'; fail.forEach(fn => fn(value)); always.forEach(fn => fn(value)); }
            };
        }
        const requests = [], notifications = [], timers = [];
        let content = 'original', destroyed = 0, result, rejected = false, busy = false, hidden = false;
        const renderError = new Error('Widget rendering failed');
        const notificationError = new Error('Notification failed');
        const widget = { isConnected: true, dataset: { instanceId: 'payments' } };
        const settings = { count: 7 };
        const entry = { id: 'payments', settings };
        let remembered = 0, flipped = false, formContent;
        const formSteps = [];
        const invalidField = { closest() { return { id: 'fees' }; }, focus() { formSteps.push('focus'); } };
        const formElement = {
            dataset: { configurationUrl: '/configure' },
            closest() { return widget; }, querySelector() { return outcome === 'validation' ? invalidField : null; }
        };
        const $form = {
            valid() { return true; }, serialize() { return 'Count=3'; },
            data() { return { destroy() { formSteps.push('destroy'); } }; },
            off() { formSteps.push('off'); },
            children() { return { html(value) { formContent = value; formSteps.push('html'); } }; },
            find() {
                return { filter(callback) {
                    assert.equal(callback.call({ getAttribute() { return '#fees'; } }), true);
                    return { tab() { formSteps.push('tab'); } };
                } };
            }
        };
        const controls = {
            filter() { return this; }, prop(name, value) { this[name] = value; return this; }, first() { return this; },
            attr() {}, index() { return -1; }
        };
        const $content = {
            0: { contains() { return true; } },
            first() { return this; }, find() { return controls; },
            html(value) {
                if (outcome === 'load-render-error') throw renderError;

                content = value; return this;
            }
        };
        const $widget = {
            0: widget,
            closest() { return { attr() { return '/load'; } }; },
            find(selector) {
                if (selector === '.dashboard-widget-content') return $content;

                if (selector === '.dashboard-widget-config') return { attr() { return '/configure'; } };

                return controls;
            },
            data(key, value) { return value === undefined ? { period: '30' } : this; },
            attr(name) {
                if (name === 'aria-busy') busy = true;

                return name === 'data-instance-id' ? 'payments' : this;
            },
            removeAttr() { busy = false; return this; },
            children() { return { throbber() { return { data() { return { show() {}, hide() { hidden = true; } }; } }; } }; }
        };
        const document = {};
        const $ = target => {
            if (typeof target === 'function') return target();

            if (target === document) return { on() {} };

            if (target === widget) return $widget;

            if (target === formElement) return $form;

            throw new Error('Unexpected jQuery target');
        };
        // The initial closest lookup needs the DOM element rather than the grid wrapper.
        const originalClosest = $widget.closest;
        $widget.closest = selector => selector === '.dashboard-widget' ? $widget : originalClosest();
        $.Deferred = deferred;
        $.param = data => new URLSearchParams({ ...data, parameters: JSON.stringify(data.parameters) }).toString();
        $.ajax = options => {
            const request = deferred();
            requests.push({ options, request });
            return request;
        };
        const Chart = { instances: { old: { canvas: {}, destroy() { destroyed++; } } } };
        const sandbox = {
            jQuery: $, Smartstore: {}, document, Chart,
            window: { Chart, Res: { Dashboard: { invalid: 'invalid', loadFailed: 'failed' } }, setTimeout(fn) { timers.push(fn); } },
            performance: { now() { return 0; } },
            updateControls() {}, $, previewing: false, config: { layer: 'User' },
            widgetsById: new Map([['payments', entry]]), layout: { widgets: [entry] },
            capture() { return settings; }, remember() { remembered++; },
            flipConfiguration() { flipped = true; },
            initializeEditControls() { formSteps.push('controls'); }, reinitFormValidator() { formSteps.push('validator'); },
            displayNotification(message) {
                if (outcome === 'load-notification-error') throw notificationError;

                notifications.push(message);
            }
        };
        vm.runInNewContext(fs.readFileSync(path.resolve(
            __dirname,
            '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.js'), 'utf8'), sandbox);

        if (loading) {
            sandbox.Smartstore.Admin.Dashboard.loadWidget(widget).done(value => { result = value; }).fail(() => { rejected = true; });
        }
        else {
            const editorSource = fs.readFileSync(path.resolve(
                __dirname,
                '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.editor.js'), 'utf8');
            vm.runInNewContext(editorSource.slice(
                editorSource.indexOf('        function serializeLayout('),
                editorSource.indexOf('        function submit(')) + editorSource.slice(
                    editorSource.indexOf('        function setConfigurationContent('),
                    editorSource.indexOf('        function flipConfiguration(')), sandbox);
            const start = editorSource.indexOf(
                "function (event) {",
                editorSource.indexOf("$(container).on('submit', '.dashboard-widget-config'"));
            const end = editorSource.indexOf("        $(form).on('submit'", start);
            const handler = vm.runInNewContext('(' + editorSource.slice(start, end).trim().slice(0, -2) + ')', sandbox);
            handler.call(formElement, { preventDefault() {} });
        }

        assert.equal(requests.length, 1);
        assert.equal(requests[0].options.url, loading ? '/load' : '/configure');
        assert.equal(requests[0].options.type, 'POST');

        if (!loading) {
            const fields = new URLSearchParams(requests[0].options.data);
            assert.equal(fields.get('Count'), '3');
            assert.equal(fields.get('apply'), 'true');
            assert.equal(fields.get('instanceId'), 'payments');
            assert.equal(JSON.parse(fields.get('parameters')).period, '30');
        }

        if (outcome === 'load-render-error' || outcome === 'load-notification-error') {
            assert.equal(controls.disabled, true);
            assert.equal(busy, true);
            assert.throws(
                () => outcome === 'load-render-error' ? requests[0].request.resolve('loaded') : requests[0].request.reject(),
                error => error === (outcome === 'load-render-error' ? renderError : notificationError));
            assert.equal(controls.disabled, false);
            assert.equal(busy, false);
            assert.equal(hidden, true);
            assert.equal(rejected, true);
            assert.equal(result, undefined);
            sandbox.Smartstore.Admin.Dashboard.loadWidget(widget);
            assert.equal(requests.length, 2);
            return;
        }

        if (outcome === 'configuration-error') {
            requests[0].request.reject();
            assert.deepEqual(notifications, ['invalid']);
            assert.equal(entry.settings, settings);
            assert.equal(remembered, 0);
            assert.equal(flipped, false);
            assert.equal(sandbox.previewing, false);
            return;
        }

        const response = outcome === 'configuration' ? { settings: { count: 3 }, html: 'updated' }
            : outcome === 'validation' ? '<span>Invalid count</span>' : 'loaded';
        requests[0].request.resolve(response);
        timers.forEach(fn => fn());

        if (outcome === 'load') assert.equal(result, response);
        else {
            assert.equal(remembered, outcome === 'configuration' ? 1 : 0);
            assert.equal(flipped, outcome === 'configuration');
            assert.equal(entry.settings, outcome === 'configuration' ? response.settings : settings);
            assert.equal(formContent, outcome === 'validation' ? response : undefined);
            assert.deepEqual(
                formSteps,
                outcome === 'validation' ? ['destroy', 'html', 'controls', 'validator', 'off', 'tab', 'focus'] : []);
            assert.equal(sandbox.previewing, false);
        }

        assert.equal(requests.length, 1);
        assert.equal(content, outcome === 'validation' ? 'original' : outcome === 'configuration' ? 'updated' : 'loaded');
        assert.equal(destroyed, outcome === 'validation' ? 0 : 1);
        assert.deepEqual(notifications, []);
    });
}

for (const fail of [false, true]) {
    test(`configuration undo/redo loads the history draft and preserves history on failure (${fail})`, async () => {
        const editorSource = fs.readFileSync(path.resolve(
            __dirname,
            '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.editor.js'), 'utf8');
        const start = editorSource.indexOf('        function historyStep(');
        const end = editorSource.indexOf("        $('#dashboard-undo')", start);
        const oldLayout = { widgets: [{ id: 'payments', settings: { count: 3 } }] };
        const newLayout = { widgets: [{ id: 'payments', settings: { count: 7 } }] };
        const undo = [{ layout: oldLayout, dirty: false }], redo = [];
        const requests = [], updates = [], notifications = [];
        const element = { isConnected: true };
        const $ = () => ({ data() { return { period: '30' }; } });
        $.ajax = options => {
            let resolve, reject;
            const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
            promise.done = callback => { promise.then(callback, () => {}); return promise; };
            promise.fail = callback => { promise.then(() => {}, callback); return promise; };
            requests.push({ options, resolve, reject });
            return promise;
        };
        const sandbox = {
            updateControls() {}, $, structuredClone, previewing: false, config: { layer: 'User', previewUrl: '/preview' },
            container: { dataset: { dashboardLoadUrl: '/load' } },
            layout: newLayout, widgetsById: new Map([['payments', newLayout.widgets[0]]]),
            elements: new Map([['payments', element]]),
            _: { isEqual: (a, b) => JSON.stringify(a) === JSON.stringify(b) },
            finish() {}, render() {}, position: () => ({ hidden: false }),
            capture() { return { layout: sandbox.layout, dirty: true }; },
            setLayout(layout) { sandbox.layout = layout; sandbox.widgetsById = new Map(layout.widgets.map(w => [w.id, w])); },
            Smartstore: { Admin: { Dashboard: { updateWidget(el, html) { updates.push({ el, html }); } } } },
            window: { Res: { Dashboard: { invalid: 'invalid' } } },
            displayNotification(message) { notifications.push(message); }
        };
        vm.runInNewContext(editorSource.slice(
            editorSource.indexOf('        function serializeLayout('),
            editorSource.indexOf('        function submit(')) + editorSource.slice(start, end), sandbox);
        sandbox.historyStep(undo, redo);
        assert.equal(requests.length, 1);
        assert.equal(requests[0].options.url, '/load');
        assert.equal(requests[0].options.data.instanceId, 'payments');
        assert.equal(requests[0].options.data.parameters.period, '30');
        assert.equal(JSON.parse(requests[0].options.data.changesJson).widgets[0].settings.count, 3);
        assert.equal(undo.length, 1);
        assert.equal(sandbox.previewing, true);

        if (fail) requests[0].reject(new Error('Load failed'));
        else requests[0].resolve('old widget');

        await new Promise(resolve => setImmediate(resolve));
        assert.equal(sandbox.previewing, false);

        if (fail) {
            assert.equal(undo.length, 1);
            assert.equal(redo.length, 0);
            assert.equal(updates.length, 0);
            assert.equal(sandbox.layout, newLayout);
            assert.deepEqual(notifications, ['invalid']);
            return;
        }

        assert.equal(undo.length, 0);
        assert.equal(redo.length, 1);
        assert.equal(updates[0].el, element);
        assert.equal(updates[0].html, 'old widget');
        assert.equal(sandbox.layout.widgets[0].settings.count, 3);
        sandbox.historyStep(redo, undo);
        assert.equal(requests.length, 2);
        assert.equal(requests[1].options.url, '/load');
        assert.equal(JSON.parse(requests[1].options.data.changesJson).widgets[0].settings.count, 7);
        requests[1].resolve('new widget');
        await new Promise(resolve => setImmediate(resolve));
        assert.equal(sandbox.layout.widgets[0].settings.count, 7);
        assert.equal(undo.length, 1);
        assert.equal(redo.length, 0);
        assert.equal(updates[1].html, 'new widget');
        assert.deepEqual(notifications, []);
    });
}

for (const hasNews of [false, true]) {
    test(`available news positions follow the draft across breakpoints and saving (news: ${hasNews})`, () => {
        for (const breakpoint of ['xs', 'sm', 'md', 'lg', 'xl', 'xxl']) {
            const editor = createEditor({ news: hasNews, breakpoint, dirty: true });
            const desktop = ['lg', 'xl', 'xxl'].includes(breakpoint);

            if (hasNews) assert.equal(editor.newsStyle.gridRow, desktop ? '1 / span 2' : '3 / span 1');

            editor.save();
            const draft = editor.submitted();
            const news = draft.widgets.find(widget => widget.id === 'news');
            assert.equal(draft.widgets.length, hasNews ? 2 : 1);

            if (!hasNews) {
                assert.equal(news, undefined);
                continue;
            }

            assert.equal(news.positions.length, 6);
            assert.ok(news.positions.slice(0, 3).every(p => p.row === 1 && p.size.rowSpan === 1 && p.column === 0 && p.size.columnSpan === 12));
            assert.ok(news.positions.slice(3).every(p => p.row === 0 && p.size.rowSpan === 1 && p.column === 10 && p.size.columnSpan === 2));
        }
    });
}

for (const layer of ['User', 'Global']) {
    test(`reset submits directly to save only after confirmation (${layer})`, () => {
        const editor = createEditor({ layer, blockedStorage: true });
        editor.reset();
        assert.equal(editor.submissions(), 0);
        editor.confirm(false);
        assert.equal(editor.submissions(), 0);
        editor.reset();
        editor.confirm(true);
        assert.equal(editor.submissions(), 1);
        assert.equal(editor.resetSubmitted(), 'true');
        assert.equal(editor.action(), 'https://example.test/admin/Home/DashboardSave');
        assert.equal(editor.canUndo(), false);
    });
}

test('cancel leaves directly without changes and only discards changes after confirmation', () => {
    const clean = createEditor();
    clean.cancel();
    assert.equal(clean.hasConfirmation(), false);
    assert.deepEqual(clean.navigations, ['/admin']);
    assert.equal(clean.submissions(), 0);

    const editor = createEditor();
    editor.drag('width', { clientX: 410 });
    editor.cancel();
    assert.equal(editor.hasConfirmation(), true);
    editor.confirm(false);
    assert.deepEqual(editor.navigations, []);
    assert.equal(editor.canCancel(), true);
    editor.cancel();
    editor.confirm(true);
    assert.deepEqual(editor.navigations, ['/admin']);
    assert.equal(editor.submissions(), 0);
});

test('changing dashboard view only asks to discard when there are changes', () => {
    const clean = createEditor();
    clean.changeLayer('Global');
    assert.equal(clean.hasConfirmation(), false);
    assert.deepEqual(clean.navigations, ['/admin?edit=true&layer=Global']);
    const dirty = createEditor({ dirty: true });
    dirty.changeLayer('Global');
    dirty.confirm(false);
    assert.deepEqual(dirty.navigations, []);
    dirty.changeLayer('Global');
    dirty.confirm(true);
    assert.deepEqual(dirty.navigations, ['/admin?edit=true&layer=Global']);
    assert.equal(dirty.submissions(), 0);
});

test('breakpoint preview changes neither dirty state nor undo history', () => {
    const editor = createEditor({ news: true });
    editor.selectBreakpoint('md');
    assert.equal(editor.previewWidth(), '768px');
    assert.equal(editor.newsStyle.gridColumn, '1 / span 12');
    assert.equal(editor.canSave(), false);
    assert.equal(editor.canUndo(), false);
    editor.selectBreakpoint('');
    assert.equal(editor.previewWidth(), '');
    assert.equal(editor.newsStyle.gridColumn, '11 / span 2');
});

test('editing a preview only changes that breakpoint and survives switching back', () => {
    const editor = createEditor();
    editor.selectBreakpoint('md');
    editor.drag('width', { clientX: 510 });
    assert.equal(editor.style.gridColumn, '3 / span 6');
    editor.selectBreakpoint('');
    assert.equal(editor.style.gridColumn, '3 / span 4');
    editor.save();
    const positions = editor.submitted().widgets[0].positions;
    assert.equal(positions.find(p => p.breakpoint === 'md').size.columnSpan, 6);
    assert.ok(positions.filter(p => p.breakpoint !== 'md').every(p => p.size.columnSpan === 4));
});

test('an unavailable preview returns to automatic without creating changes', () => {
    const editor = createEditor();
    editor.selectBreakpoint('lg');
    editor.resize(800);
    assert.equal(editor.previewWidth(), '');
    assert.equal(editor.breakpointDisabled('lg'), true);
    assert.equal(editor.breakpointDisabled('md'), false);
    assert.equal(editor.canSave(), false);
    assert.equal(editor.canUndo(), false);
});

test('saving restores the selected preview once without restoring dirty state or undo', () => {
    for (const tier of ['', 'xs', 'sm', 'md', 'lg', 'xl']) {
        const storage = new Map();
        const editor = createEditor({ storage, dirty: true });
        editor.selectBreakpoint(tier);
        const width = editor.previewWidth();
        editor.save();
        assert.equal(editor.submissions(), 1);

        const reopened = createEditor({ storage });
        assert.equal(reopened.previewWidth(), width);
        assert.equal(reopened.canSave(), false);
        assert.equal(reopened.canUndo(), false);
        assert.equal(storage.size, 0);
        assert.equal(createEditor({ storage }).previewWidth(), '');
    }
});

test('unavailable tab storage does not prevent saving a selected preview', () => {
    const editor = createEditor({ blockedStorage: true, dirty: true });
    editor.selectBreakpoint('md');
    editor.save();
    assert.equal(editor.submissions(), 1);
});

test('XXL uses all available width and automatic shows the active breakpoint on resize', () => {
    const editor = createEditor({ breakpoint: 'xxl' });
    editor.resize(1800);
    editor.selectBreakpoint('xxl');
    assert.equal(editor.previewWidth(), '');
    assert.equal(editor.previewLabel(), 'XXL');
    editor.resize(1300);
    assert.equal(editor.breakpointDisabled('xxl'), true);
    assert.equal(editor.previewLabel(), 'Automatic · XL');
    editor.resize(800);
    assert.equal(editor.previewLabel(), 'Automatic · MD');
    assert.equal(editor.canSave(), false);
    assert.equal(editor.canUndo(), false);
});

test('hiding is local to one tier, survives save, and supports undo, redo and showing again', async () => {
    const editor = createEditor({ news: true });
    editor.selectBreakpoint('sm');
    await editor.hide();
    assert.equal(editor.hasConfirmation(), false);
    assert.equal(editor.style.display, 'none');
    assert.equal(editor.newsStyle.gridRow, '1 / span 1');
    editor.selectBreakpoint('md');
    assert.equal(editor.style.display, 'grid');
    editor.selectBreakpoint('xs');
    assert.equal(editor.style.display, 'grid');
    editor.selectBreakpoint('sm');
    await editor.undo();
    assert.equal(editor.style.display, 'grid');
    await editor.redo();
    assert.equal(editor.style.display, 'none');
    editor.save();
    const widget = editor.submitted().widgets.find(w => w.id === 'a');
    assert.deepEqual(widget.positions.filter(p => p.hidden).map(p => p.breakpoint), ['sm']);
    editor.show();
    assert.equal(editor.style.display, 'grid');
});

test('required widgets cannot be hidden through the editor', async () => {
    const editor = createEditor({ required: true });
    await editor.hide();
    assert.equal(editor.hasConfirmation(), false);
    assert.equal(editor.style.display, 'grid');
    assert.equal(editor.canSave(), false);
});

test('showing resolves collisions in the active tier', async () => {
    const editor = createEditor({ belowPosition: { row: 1 } });
    editor.selectBreakpoint('md');
    await editor.hide();
    assert.equal(editor.hasConfirmation(), false);
    editor.show();
    assert.notEqual(editor.style.gridRow, editor.belowStyle.gridRow);
    editor.selectBreakpoint('lg');
    assert.equal(editor.style.gridRow, '2 / span 1');
    assert.equal(editor.belowStyle.gridRow, '2 / span 1');
});

test('showing animates the existing widget without changing saved placement rules', async () => {
    const editor = createEditor();
    await editor.hide();
    assert.equal(editor.hasConfirmation(), false);
    editor.show();
    const animation = editor.animations.find(a => a.id === 'a' && a.frames[0].opacity === 0);
    assert.equal(animation.options.duration, 240);
    assert.equal(animation.frames[0].opacity, 0);
    assert.equal(animation.frames[1].opacity, 1);
});

test('reduced motion suppresses visibility animations', async () => {
    const editor = createEditor({ reducedMotion: true });
    await editor.hide();
    assert.equal(editor.hasConfirmation(), false);
    editor.show();
    assert.equal(editor.animations.length, 0);
    assert.equal(editor.style.display, 'grid');
});

test('removing requires confirmation and removes the widget from every tier', async () => {
    const editor = createEditor();
    editor.remove();
    assert.equal(editor.hasConfirmation(), true);
    editor.confirm(false);
    assert.equal(editor.canSave(), false);
    editor.remove();
    await editor.confirm(true);
    await editor.undo();
    assert.equal(editor.canSave(), false);
    await editor.redo();
    assert.equal(editor.canSave(), true);
    editor.save();
    assert.equal(editor.submitted().widgets.some(w => w.id === 'a'), false);
});

test('required widgets cannot be removed through the editor', async () => {
    const editor = createEditor({ required: true });
    editor.remove();
    assert.equal(editor.hasConfirmation(), false);
    assert.equal(editor.canSave(), false);
});

for (const action of ['hide', 'remove']) {
    test(`undo restores a ${action} widget with animation, redo animates removal`, async () => {
        const editor = createEditor();
        if (action === 'hide') await editor.hide();
        else { editor.remove(); await editor.confirm(true); }
        editor.animations.length = 0;
        await editor.undo();
        assert.equal(editor.animations.some(a => a.id === 'a' && a.frames[0].opacity === 0), true);
        editor.animations.length = 0;
        await editor.redo();
        assert.equal(editor.animations.some(a => a.id === 'a' && a.frames[1].opacity === 0), true);
    });
}

test('redo animates showing a widget again', async () => {
    const editor = createEditor();
    await editor.hide();
    editor.show();
    await editor.undo();
    editor.animations.length = 0;
    await editor.redo();
    assert.equal(editor.animations.some(a => a.id === 'a' && a.frames[0].opacity === 0), true);
});

test('undo does not animate visibility changes from another breakpoint', async () => {
    const editor = createEditor();
    editor.selectBreakpoint('sm');
    await editor.hide();
    editor.selectBreakpoint('lg');
    editor.animations.length = 0;
    await editor.undo();
    assert.equal(editor.animations.length, 0);
});

for (const action of ['hide', 'remove']) {
    test(`saving waits for the ${action} animation and includes the completed change`, async () => {
        const editor = createEditor({ dirty: true, deferFade: true });
        assert.equal(editor.canSave(), true);
        let pending;
        if (action === 'hide') pending = editor.hide();
        else { editor.remove(); pending = editor.confirm(true); }
        assert.equal(editor.canSave(), false);
        editor.save();
        assert.equal(editor.submissions(), 0);
        editor.finishFade();
        await pending;
        assert.equal(editor.canSave(), true);
        editor.save();
        assert.equal(editor.submissions(), 1);
        const widget = editor.submitted().widgets.find(w => w.id === 'a');
        if (action === 'hide') assert.equal(widget.positions.find(p => p.breakpoint === 'xl').hidden, true);
        else assert.equal(widget, undefined);
    });

    test(`cancelled ${action} animation releases the save lock without changing visibility`, async () => {
        const editor = createEditor({ dirty: true, deferFade: true });
        let pending;
        if (action === 'hide') pending = editor.hide();
        else { editor.remove(); pending = editor.confirm(true); }
        editor.cancelFade();
        await pending;
        assert.equal(editor.canSave(), true);
        editor.save();
        const widget = editor.submitted().widgets.find(w => w.id === 'a');
        assert.ok(widget);
        assert.equal(widget.positions.some(p => p.hidden), false);
    });
}
