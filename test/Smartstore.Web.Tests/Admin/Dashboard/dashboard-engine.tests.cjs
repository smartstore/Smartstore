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
test('neighbors retain a shared row regardless of their requested heights', () => {
    const items = [item('a', 0, 0, 6, { minHeight: 700 }), item('b', 6, 0, 6, { minHeight: 300 }), item('c', 0, 1, 12)];
    Engine.arrange(items, 12, 'a');
    assert.deepEqual(items.map(x => x.row), [0, 0, 1]);
    assert.equal(items[0].minHeight, 700);
});
test('moving into an occupied cell pushes the other card to the next row', () => {
    const items = [item('a', 0, 1, 6), item('b', 0, 1, 6), item('c', 6, 1, 6)];
    Engine.arrange(items, 12, 'a');
    assert.deepEqual(items.map(x => x.row), [0, 1, 0]);
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
test('empty rows close together without splitting neighboring cards', () => {
    const items = [item('a', 0, 4, 6), item('b', 6, 4, 6), item('c', 0, 9, 12)];
    Engine.arrange(items, 12);
    assert.deepEqual(items.map(x => x.row), [0, 0, 1]);
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
    const element = {
        dataset: { instanceId: 'a', minWidth: '2', maxWidth: '6' },
        style: {}, isConnected: true, offsetHeight: 300,
        classList: { contains: () => false, add() {}, remove() {} },
        getBoundingClientRect: () => ({ top: 0, bottom: 300, height: 300 }),
        querySelector: () => null, querySelectorAll: () => []
    };
    const belowElement = { ...element, dataset: { ...element.dataset, instanceId: 'b' }, style: {},
        querySelector: () => ({}), getBoundingClientRect: () => ({ top: 300, bottom: 500, height: 200 }) };
    const container = {
        querySelectorAll: () => belowPosition ? [element, belowElement] : [element],
        addEventListener: (name, callback) => handlers.set(name, [callback]),
        getBoundingClientRect: () => ({ left: 0, right: 1200, top: 0 })
    };
    const scrollContainer = { scrollTop: 0, getBoundingClientRect: () => ({ top: 0, bottom: 900 }) };
    const config = {
        layout: { id: 'dashboard', columnCount: 12, widgets: [
            { id: 'a', widgetSystemName: 'Test', allowMove: true,
                positions: [{ minViewportWidth: 0, column: 2, row: 1, size: { columnSpan: 4, rowSpan: 1 } }] }
        ] },
        maxHeight: 310, widgets: [{ systemName: 'Test', allowedSizes }], layer, dirty: false, reset: false
    };
    if (belowPosition) {
        config.layout.widgets.push({ id: 'b', widgetSystemName: 'Test', allowMove: true,
            positions: [{ minViewportWidth: 0, column: 2, row: 2, size: { columnSpan: 4, rowSpan: 1 }, minHeight: 200, ...belowPosition }] });
    }
    let confirmation;
    let submissions = 0;
    const form = {
        action: 'https://example.test/admin/Home/DashboardSave',
        elements: Object.fromEntries(['changesJson', 'reset', 'command', 'widgetSystemName'].map(name => [name, { value: '' }]))
    };
    const document = {
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
        Smartstore: { Admin: {} }, jQuery: jquery, document, structuredClone, URL,
        HTMLFormElement: { prototype: { submit() { submissions++; } } },
        confirm2(options) { confirmation = options; return { find: () => ({ text() {} }) }; },
        window: { innerWidth: 1200, innerHeight: 900, scrollY: 0, location: { href: 'https://example.test/admin?dashboardHistory=test' },
            history: { replaceState() {} }, addEventListener() {} },
        sessionStorage: {
            getItem() { if (blockedStorage) throw new Error('Storage blocked'); return null; },
            removeItem() {}
        },
        getComputedStyle: () => ({ direction, gridTemplateColumns: Array(12).fill(100).join(' ') }),
        requestAnimationFrame: callback => { sandbox.frame = callback; return 1; },
        cancelAnimationFrame() {}
    };
    vm.createContext(sandbox);
    vm.runInContext(fs.readFileSync(path.resolve(__dirname, '../../../../src/Smartstore.Web/wwwroot/lib/underscore/underscore.js'), 'utf8'), sandbox);
    vm.runInContext(fs.readFileSync(path.resolve(__dirname, '../../../../src/Smartstore.Web/wwwroot/admin/js/smartstore.dashboard.js'), 'utf8'), sandbox);
    return {
        style: element.style,
        belowStyle: belowElement.style,
        scrollContainer,
        save: () => controls.get(form).submit({ preventDefault() {} }),
        confirm: accepted => confirmation.callback(accepted),
        hasConfirmation: () => !!confirmation,
        submissions: () => submissions,
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

test('height uses the server limit and Home restores automatic height', () => {
    const editor = keyboardEditor();
    editor.press('height', 'ArrowDown');
    editor.press('height', 'ArrowDown');
    assert.equal(editor.style.minHeight, '310px');
    editor.undo();
    assert.equal(editor.style.minHeight, '0px');
    editor.redo();
    editor.press('height', 'Home');
    assert.equal(editor.style.minHeight, '0px');
});

test('RTL start resizing keeps the opposite edge fixed even with blocked history storage', () => {
    const editor = keyboardEditor({ direction: 'rtl', blockedStorage: true });
    editor.press('width-start', 'ArrowRight');
    assert.equal(editor.style.gridColumn, '2 / span 5');
    editor.undo();
    assert.equal(editor.style.gridColumn, '3 / span 4');
});

// A gap is only converted into grid rows when the drop is committed.
test('dropping below a shortened card spans its neighbor and retains the original during preview', () => {
    const items = [item('a', 0, 0, 6, { resizable: true }),
        item('b', 6, 0, 6, { stretch: false, minHeight: 200 }), item('c', 0, 1, 6), item('d', 0, 2, 12)];
    const before = JSON.stringify(items);
    const proposed = Engine.insertBelow(items, 'b', 'c', 6);
    assert.equal(JSON.stringify(items), before);
    Engine.arrange(proposed, 12, 'c');
    assert.equal(proposed[0].size.rowSpan, 2);
    assert.equal(proposed[0].stretch, true);
    assert.equal(proposed[1].size.rowSpan, 1);
    assert.equal(proposed[2].row, 1);
    assert.equal(proposed[2].column, 6);
    assert.equal(proposed[3].row, 2);
});

test('gap insertion rejects locked neighbors, locked following rows and unsupported spans atomically', () => {
    for (const restrictions of [{ resizable: false }, { resizable: true, maxSize: { rowSpan: 1 } },
        { resizable: true, allowedSizes: [{ columnSpan: 6, rowSpan: 1 }] }]) {
        const items = [item('a', 0, 0, 6, restrictions), item('b', 6, 0, 6, { stretch: false }), item('c', 0, 1, 6)];
        const before = JSON.stringify(items);
        assert.equal(Engine.insertBelow(items, 'b', 'c', 6), null);
        assert.equal(JSON.stringify(items), before);
    }
    const items = [item('a', 0, 0, 6, { resizable: true }), item('b', 6, 0, 6, { stretch: false }),
        item('c', 0, 2, 6), item('locked', 0, 1, 12, { locked: true })];
    assert.equal(Engine.insertBelow(items, 'b', 'c', 6), null);
    assert.equal(Engine.insertBelow(items, 'b', 'c', 5), null);
});

test('dropping a two-row widget extends crossing neighbors by two rows', () => {
    const items = [item('a', 0, 0, 6, { resizable: true }), item('b', 6, 0, 6, { stretch: false }),
        item('c', 0, 2, 6, { size: { columnSpan: 6, rowSpan: 2 } })];
    const proposed = Engine.insertBelow(items, 'b', 'c', 6);
    Engine.arrange(proposed, 12, 'c');
    assert.equal(proposed[0].size.rowSpan, 3);
    assert.equal(proposed[2].row, 1);
    assert.equal(proposed[2].size.rowSpan, 2);
});

test('shortening leaves the row unchanged and undo, redo and Home restore stretching', () => {
    const editor = keyboardEditor();
    const row = editor.style.gridRow;
    editor.press('height', 'ArrowUp');
    assert.equal(editor.style.alignSelf, 'start');
    assert.equal(editor.style.gridRow, row);
    editor.undo();
    assert.equal(editor.style.alignSelf, 'stretch');
    editor.redo();
    assert.equal(editor.style.alignSelf, 'start');
    editor.press('height', 'Home');
    assert.equal(editor.style.alignSelf, 'stretch');
});


test('returning the lower edge to its original height restores stretching and history', () => {
    const editor = keyboardEditor();
    editor.dragHeight(250, 300);
    assert.equal(editor.style.alignSelf, 'stretch');
    assert.equal(editor.style.minHeight, '300px');
    editor.undo();
    assert.equal(editor.style.minHeight, '0px');
    editor.redo();
    assert.equal(editor.style.alignSelf, 'stretch');
    editor.press('height', 'ArrowUp');
    editor.dragHeight(250, 300);
    assert.equal(editor.style.alignSelf, 'start');
});

test('a dropped resizable widget fills the gap using its existing width and minimum height fields', () => {
    const items = [item('a', 0, 0, 6, { resizable: true }),
        item('b', 6, 0, 6, { stretch: false }), item('c', 0, 1, 4, { resizable: true })];
    const proposed = Engine.insertBelow(items, 'b', 'c', 8, 480);
    assert.equal(proposed[2].column, 6);
    assert.equal(proposed[2].size.columnSpan, 6);
    assert.equal(proposed[2].minHeight, 480);
    assert.equal(proposed[2].stretch, true);
    items[2].maxSize = { columnSpan: 4 };
    assert.equal(Engine.insertBelow(items, 'b', 'c', 8, 480)[2].size.columnSpan, 4);
    items[2].resizable = false;
    const fixed = Engine.insertBelow(items, 'b', 'c', 8, 480)[2];
    assert.equal(fixed.size.columnSpan, 4);
    assert.equal(fixed.minHeight, undefined);
});


test('a larger resizable widget can fill a narrower gap without bypassing size restrictions', () => {
    const items = [item('a', 0, 0, 6, { resizable: true }),
        item('b', 6, 0, 4, { stretch: false }),
        item('c', 0, 1, 6, { resizable: true, minSize: { columnSpan: 3, rowSpan: 1 }, maxSize: { columnSpan: 12 } })];
    const proposed = Engine.insertBelow(items, 'b', 'c', 6, 300);
    assert.equal(proposed[2].column, 6);
    assert.equal(proposed[2].size.columnSpan, 4);
    items[2].allowedSizes = [{ columnSpan: 6, rowSpan: 1 }];
    assert.equal(Engine.insertBelow(items, 'b', 'c', 6, 300), null);
    items[2].allowedSizes = [];
    items[2].resizable = false;
    assert.equal(Engine.insertBelow(items, 'b', 'c', 6, 300), null);
});

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

test('height dragging accounts for content scrolling and scrolls the content at its edge', () => {
    const editor = keyboardEditor();
    editor.drag('height', { clientY: 250, scrollTop: 40 });
    assert.equal(editor.style.minHeight, '290px');
    editor.undo();
    editor.drag('height', { clientY: 880 });
    assert.equal(editor.scrollContainer.scrollTop, 55);
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

test('height growth releases space below and undo/redo restores both cards', () => {
    const editor = keyboardEditor({ belowPosition: {} });
    editor.dragHeight(310);
    assert.equal(editor.style.minHeight, '310px');
    assert.equal(editor.belowStyle.minHeight, '190px');
    editor.undo();
    assert.equal(editor.style.minHeight, '0px');
    assert.equal(editor.belowStyle.minHeight, '200px');
    editor.redo();
    assert.equal(editor.style.minHeight, '310px');
    assert.equal(editor.belowStyle.minHeight, '190px');
});

test('reversing a height drag restores the lower minimum without accumulating changes', () => {
    const editor = keyboardEditor({ belowPosition: {} });
    editor.dragHeight(310, 305, 310, 300);
    assert.equal(editor.belowStyle.minHeight, '200px');
});

test('height growth leaves lower cards with different columns or nonadjacent rows alone', () => {
    for (const belowPosition of [{ column: 3 }, { row: 3 }, { size: { columnSpan: 3, rowSpan: 1 } }]) {
        const editor = keyboardEditor({ belowPosition });
        editor.dragHeight(310);
        assert.equal(editor.belowStyle.minHeight, '200px');
    }
});


test('moving cities upward reuses the unused realtime row without extending neighbors', () => {
    const items = [
        item('devices', 0, 5, 3, { resizable: true, size: { columnSpan: 3, rowSpan: 7 } }),
        item('realtime', 3, 6, 3, { resizable: true, stretch: false, minHeight: 273, size: { columnSpan: 3, rowSpan: 2 } }),
        item('cities', 3, 8, 3, { resizable: true }),
        item('events', 6, 7, 4, { resizable: true, size: { columnSpan: 4, rowSpan: 5 } }),
        item('following', 0, 12, 4)
    ];
    const original = JSON.stringify(items);
    const proposed = Engine.insertBelow(items, 'realtime', 'cities', 3, 129, 7);
    assert.ok(proposed);
    assert.equal(proposed[1].size.rowSpan, 1);
    assert.equal(proposed[1].stretch, true);
    assert.equal(proposed[1].minHeight, 273);
    assert.equal(proposed[2].row, 7);
    assert.equal(proposed[0].size.rowSpan, 7);
    assert.equal(proposed[3].size.rowSpan, 5);
    assert.equal(proposed[4].row, 12);
    assert.equal(JSON.stringify(items), original);
    assert.equal(Engine.insertBelow(proposed, 'realtime', 'cities', 3, 129, 7), null);
});

test('reusing rows respects anchor size limits and occupied cells without modifying the layout', () => {
    for (const restrictions of [
        { resizable: false },
        { minSize: { rowSpan: 2 } },
        { allowedSizes: [{ columnSpan: 3, rowSpan: 2 }] }
    ]) {
        const items = [item('anchor', 0, 0, 3, { resizable: true, stretch: false,
            size: { columnSpan: 3, rowSpan: 2 }, ...restrictions }), item('moving', 0, 2, 3, { resizable: true })];
        const original = JSON.stringify(items);
        assert.equal(Engine.insertBelow(items, 'anchor', 'moving', 0, 100, 1), null);
        assert.equal(JSON.stringify(items), original);
    }
    const items = [item('anchor', 0, 0, 3, { resizable: true, stretch: false, size: { columnSpan: 3, rowSpan: 2 } }),
        item('moving', 0, 2, 3, { resizable: true }), item('occupied', 0, 1, 3, { locked: true })];
    assert.equal(Engine.insertBelow(items, 'anchor', 'moving', 0, 100, 1), null);
});
