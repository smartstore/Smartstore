const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const context = { Smartstore: { Admin: {} }, jQuery() {} };
vm.createContext(context);
vm.runInContext(fs.readFileSync(path.resolve(__dirname, '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.js'), 'utf8'), context);
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

// Run the editor's actual keyboard handlers with a minimal DOM and the bundled Underscore.
function keyboardEditor({ direction = 'ltr', blockedStorage = false, allowedSizes = [], layer = 'User', belowPosition } = {}) {
    const handlers = new Map();
    const controls = new Map();
    const extension = { inert: false };
    const element = {
        dataset: { instanceId: 'a', minWidth: '2', maxWidth: '6' },
        style: {}, isConnected: true, offsetHeight: 300,
        classList: { contains: () => false, add() {}, remove() {} },
        getBoundingClientRect: () => ({ top: 0, bottom: 300, height: 300 }),
        querySelector: selector => selector.includes('resize') || selector === '.dashboard-widget-overlay' ? {} : null,
        querySelectorAll: selector => selector.includes(':not(.dashboard-extend)') ? [] : [extension]
    };
    const belowElement = { ...element, dataset: { ...element.dataset, instanceId: 'b' }, style: {},
        querySelector: () => ({}), getBoundingClientRect: () => ({ top: 300, bottom: 500, height: 200 }) };
    const container = {
        appendChild() {},
        querySelectorAll: selector => selector === '[data-instance-id]' ? (belowPosition ? [element, belowElement] : [element]) : [],
        addEventListener: (name, callback) => handlers.set(name, [callback]),
        getBoundingClientRect: () => ({ left: 0, right: 1200, top: 0 })
    };
    const scrollContainer = { scrollTop: 0, getBoundingClientRect: () => ({ top: 0, bottom: 900 }) };
    const config = {
        layout: { id: 'dashboard', columnCount: 12, rules: ['xs', 'sm', 'md', 'lg', 'xl', 'xxl'].map(breakpoint => ({ breakpoint, columnCount: 12, fullWidthWidgets: ['xs', 'sm'].includes(breakpoint), hasNewsRail: false })), widgets: [
            { id: 'a', widgetSystemName: 'Test', allowMove: true,
                positions: [{ breakpoint: 'xs', column: 2, row: 1, size: { columnSpan: 4, rowSpan: 1 } }] }
        ] },
        maxHeight: 310, widgets: [{ systemName: 'Test', allowedSizes }], layer, dirty: false, reset: false
    };
    if (belowPosition) {
        config.layout.widgets.push({ id: 'b', widgetSystemName: 'Test', allowMove: true,
            positions: [{ breakpoint: 'xs', column: 2, row: 2, size: { columnSpan: 4, rowSpan: 1 }, minHeight: 200, ...belowPosition }] });
    }
    let confirmation;
    let submissions = 0;
    const form = {
        action: 'https://example.test/admin/Home/DashboardSave',
        elements: Object.fromEntries(['changesJson', 'reset', 'command', 'widgetSystemName'].map(name => [name, { value: '' }]))
    };
    const document = {
        createElement: () => ({ style: {}, setAttribute() {}, remove() {} }),
        getElementById: id => ({
            'dashboard-editor': { dataset: { editing: 'true', editorConfig: 'config' } },
            config: { textContent: JSON.stringify(config) },
            'dashboard-grid': container,
            content: scrollContainer,
            'dashboard-draft-form': form
        })[id],
        querySelectorAll: () => [],
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
                toggleClass() { return this; },
                on(name, callback) { this[name] = callback; return this; }
            };
            controls.set(selector, control);
        }
        return controls.get(selector);
    }
    const sandbox = {
        Smartstore: { Admin: {} }, ResponsiveBootstrapToolkit: { current: () => 'xl' }, jQuery: jquery, document, structuredClone, URL,
        HTMLFormElement: { prototype: { submit() { submissions++; } } },
        confirm2(options) { confirmation = options; return { find: () => ({ text() {} }) }; },
        window: { innerWidth: 1200, innerHeight: 900, scrollY: 0, location: { href: 'https://example.test/admin?dashboardHistory=test' },
            history: { replaceState() {} }, addEventListener() {} },
        sessionStorage: {
            getItem() { if (blockedStorage) throw new Error('Storage blocked'); return null; },
            removeItem() {}
        },
        getComputedStyle: () => ({ direction, gridTemplateRows: '150px 150px 150px 150px', gridTemplateColumns: Array(12).fill(100).join(' ') }),
        requestAnimationFrame: callback => { sandbox.frame = callback; return 1; },
        cancelAnimationFrame() {}
    };
    vm.createContext(sandbox);
    vm.runInContext(fs.readFileSync(path.resolve(__dirname, '../../../../src/Smartstore.Web/wwwroot/lib/underscore/underscore.js'), 'utf8'), sandbox);
    vm.runInContext(fs.readFileSync(path.resolve(__dirname, '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.js'), 'utf8'), sandbox);
    return {
        extension,
        style: element.style,
        belowStyle: belowElement.style,
        scrollContainer,
        save: () => controls.get(form).submit({ preventDefault() {} }),
        confirm: accepted => confirmation.callback(accepted),
        hasConfirmation: () => !!confirmation,
        submissions: () => submissions,
        submitted: () => JSON.parse(form.elements.changesJson.value),
        undo: () => controls.get('#dashboard-undo').click(),
        redo: () => controls.get('#dashboard-redo').click(),
        canUndo: () => !controls.get('#dashboard-undo').disabled,
        drag(kind, ...points) {
            const handle = { dataset: { dashboardResize: kind }, closest: () => element };
            handlers.get('pointerdown')[0]({ clientX: 300, clientY: 300,
                target: { closest: () => handle }, preventDefault() {} });
            for (const point of points) {
                if (point.scrollTop !== undefined) scrollContainer.scrollTop = point.scrollTop;
                handlers.get('pointermove')[0]({ clientX: 300, clientY: 300, ...point });
                sandbox.frame();
            }
            handlers.get('pointerup')[0]();
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
        },
        press(kind, key) {
            const handle = { dataset: { dashboardResize: kind }, closest: () => element };
            const event = { key, target: { closest: () => handle }, preventDefault() {} };
            for (const handler of handlers.get('keydown')) handler(event);
        }
    };
}

test('each keyboard handle ignores unrelated keys without creating undo entries', () => {
    for (const [kind, keys] of [[undefined, ['Home']], ['width', ['Home', 'ArrowUp', 'ArrowDown']],
        ['width-start', ['Home', 'ArrowUp', 'ArrowDown']], ['height', ['ArrowLeft', 'ArrowRight']]]) {
        const editor = keyboardEditor();
        const before = { ...editor.style };
        for (const key of keys) editor.press(kind, key);
        assert.deepEqual(editor.style, before);
        assert.equal(editor.canUndo(), false);
    }
});

test('width limit does not add a redundant undo step and redo restores the change', () => {
    const editor = keyboardEditor();
    editor.press('width', 'ArrowRight');
    editor.press('width', 'ArrowRight');
    editor.press('width', 'ArrowRight');
    assert.equal(editor.style.gridColumn, '3 / span 6');
    editor.undo();
    assert.equal(editor.style.gridColumn, '3 / span 5');
    editor.redo();
    assert.equal(editor.style.gridColumn, '3 / span 6');
});

test('RTL start resizing keeps the opposite edge fixed even with blocked history storage', () => {
    const editor = keyboardEditor({ direction: 'rtl', blockedStorage: true });
    editor.press('width-start', 'ArrowRight');
    assert.equal(editor.style.gridColumn, '2 / span 5');
    editor.undo();
    assert.equal(editor.style.gridColumn, '3 / span 4');
});

// A gap is only converted into grid rows when the drop is committed.
test('pointer and keyboard resizing use only allowed widths and keep the opposite edge fixed', () => {
    const allowedSizes = [{ columnSpan: 4, rowSpan: 1 }, { columnSpan: 6, rowSpan: 1 }];
    for (const direction of ['ltr', 'rtl']) {
        const editor = keyboardEditor({ allowedSizes, direction });
        editor.press('width', direction === 'ltr' ? 'ArrowRight' : 'ArrowLeft');
        assert.equal(editor.style.gridColumn, '3 / span 6');
        editor.undo();
        editor.press('width-start', direction === 'ltr' ? 'ArrowLeft' : 'ArrowRight');
        assert.equal(editor.style.gridColumn, '1 / span 6');
        editor.undo();
        editor.drag('width', { clientX: direction === 'ltr' ? 510 : 90 });
        assert.equal(editor.style.gridColumn, '3 / span 6');
        editor.undo();
        editor.drag('width-start', { clientX: direction === 'ltr' ? 90 : 510 });
        assert.equal(editor.style.gridColumn, '1 / span 6');
    }
});

test('personal dashboard saves immediately without a confirmation', () => {
    const editor = keyboardEditor();
    editor.save();
    assert.equal(editor.hasConfirmation(), false);
    assert.equal(editor.submissions(), 1);
});

test('global dashboard only saves after confirmation and can be cancelled', () => {
    const editor = keyboardEditor({ layer: 'Global' });
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

test('keyboard row resizing and history never produce pixel heights', () => {
    const editor = keyboardEditor({ belowPosition: { column: 6 } });
    editor.press('height', 'ArrowDown');
    assert.equal(editor.style.gridRow, '2 / span 2');
    assert.equal(editor.style.minHeight, undefined);
    editor.undo();
    assert.equal(editor.style.gridRow, '2 / span 1');
    editor.redo();
    assert.equal(editor.style.gridRow, '2 / span 2');
    editor.press('height', 'Home');
    assert.equal(editor.style.gridRow, '2 / span 1');
});

test('pointer resizing snaps to rows and undo restores the row span', () => {
    const editor = keyboardEditor({ belowPosition: { column: 6 } });
    editor.dragHeight(450);
    assert.equal(editor.style.gridRow, '2 / span 2');
    editor.undo();
    assert.equal(editor.style.gridRow, '2 / span 1');
    editor.redo();
    editor.dragHeight(300);
    assert.equal(editor.style.gridRow, '2 / span 1');
});

test('saving compacts every responsive layout without removing occupied spans', () => {
    const editor = keyboardEditor();
    assert.equal(editor.style.gridRow, '2 / span 1');
    editor.save();
    for (const p of editor.submitted().widgets[0].positions) assert.equal(p.row, 0);
});

test('the extension button remains interactive while widget contents are inert', () => {
    assert.equal(keyboardEditor().extension.inert, false);
});
