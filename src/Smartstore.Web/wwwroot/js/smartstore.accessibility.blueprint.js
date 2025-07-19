/**
* WCAG‑2.2 keyboard navigation framework
*/
'use strict';

/* --------------------------------------------------
*  AccessKit core – plugin host + key dispatcher
* -------------------------------------------------- */

class AccessKit {
    // Constants
    static RTL = document.documentElement.dir === 'rtl';
    static TEXT_INPUT_TYPES = new Set(['text', 'email', 'tel', 'url', 'search', 'password', 'date', 'datetime-local', 'datetime', 'month', 'number', 'time', 'week']);
    static ACTIVE_OPTION_SELECTOR = '[role="option"]:not(:is([disabled], .disabled, .hidden, [aria-disabled="true"]))';
    static ACTIVE_RADIO_SELECTOR = 'input[type="radio"]:not([disabled])';
    static KEY = {
        LEFT: 'ArrowLeft',
        UP: 'ArrowUp',
        RIGHT: 'ArrowRight',
        DOWN: 'ArrowDown',
        HOME: 'Home',
        END: 'End',
        ESC: 'Escape',
        SPACE: ' ',
        TAB: 'Tab',
        ENTER: 'Enter'
    };

    // Static fields
    static #instance = null;
    static #strategies = [];
    static #navKeys = null;

    // Instance fields
    #listening = false;

    static get instance() {
        if (!this.#instance) throw new Error('AccessKit instance not created yet. Call AccessKit.create() first.');
        return this.#instance;
    }

    static get isReady() {
        return this.#instance !== null;
    }

    static register(strategy) {
        if (strategy) this.#strategies.push(strategy);
    }

    static create(options) {
        if (this.#instance) return this.#instance;
        this.#instance = new this(options);

        const k = AccessKit.KEY;
        this.#navKeys = new Set([k.TAB,k.UP, k.DOWN, k.LEFT, k.RIGHT, k.HOME, k.END, k.ENTER, k.SPACE, k.ESC]);

        // Add more strategies as needed...
        return this.#instance;
    }

    constructor(options) {
        this.options = options;
        this.plugins = new Map();

        document.addEventListener('keydown', this.#onKeyDown, true);
        document.addEventListener('keyup', this.#onKeyUp, true);

        // Start listening to key events
        this.startListen();
    }

    startListen() {
        this.#listening = true;
    }

    stopListen() {
        this.#listening = false;
    }

    get isListening() {
        return this.#listening;
    }

    #isCandidateElement(el) {
        if (el.tagName == 'TEXTAREA' || el.isContentEditable) {
            return false;
        }
        if (el.tagName == 'INPUT') {
            return !AccessKit.TEXT_INPUT_TYPES.has(el.type);
        }

        return el.matches('a,button,[role],[tabindex]');
    };

    static #isNavKey(key) {
        // Exit if no navigational key is pressed.
        return this.#navKeys.has(e.key);
    }

    #onKeyDown(e) {
        if (!this.#listening) return;

        // Skip irrelevant targets immediately.
        const t = e.target;
        if (!t || !(t instanceof Element)) return;
        if (e.key != KEY.ESC) {
            if (!this.#isCandidateElement(t)) return;
        }

        if (!AccessKit.#isNavKey(e.key)) return;

        // Init plugin instance if needed.
        this.#tryCreateInstance(t);

        // Dispatch event to all already active plugins.
        this.#dispatchKey(e);
    }

    #onKeyUp(e) {
        if (!this.#listening) return;

        // ...
    }

    #matchStrategy(strategy, target) {
        if (strategy.itemSelector && !target.matches(strategy.itemSelector)) {
            return null;
        }

        return target.closest(strategy.rootSelector);
    }

    #tryCreateInstance(target) {
        for (const strategy of this.#strategies) {
            let root = this.#matchStrategy(strategy, target);

            if (!root)
                continue;

            if (!this.plugins.has(strategy.name)) {
                const instance = new strategy.ctor(strategy);

                // Add the first widget to the instance here already
                instance.addWidget(root);

                this.plugins.set(strategy.name, instance);
            }

            return;
        }
    }

    #dispatchKey(e) {
        const hook = e.type === 'keydown' ? 'handleKey' : 'handleKeyUp';

        for (const plugin of this.plugins.values()) {
            const handler = plugin[hook];
            // Check whether plugin implements the handler method & if it returns true (handled).
            if (typeof handler === 'function' && handler.call(plugin, e)) {
                // Preserve natural Tab behaviour, but prevent default for all other keys.
                if (e.type === 'keydown' && e.key !== this.#key.TAB) {
                    e.preventDefault();
                }

                e.stopPropagation();
                return;
            }
        }
    }
}

class TestPluginBase {
    constructor(strategy) {
        this.strategy = strategy;
        this._widgets = new WeakMap();
        this._handles = [];
    }

    get widgets() {
        return this._widgets;
    }

    hasWidget(root) {
        return this._widgets.has(root);
    }

    getWidget(root, addIfNotPresent = false) {
        let widget = this._widgets.get(root);
        if (!widget && addIfNotPresent) {
            this.addWidget(root);
            widget = this._widgets.get(root);
        }

        return widget;
    }

    addWidget(root, replaceExisting = false) {
        if (replaceExisting && this._widgets.has(root)) {
            this._widgets.delete(root);
        }

        if (!this._widgets.has(root)) {
            const widget = {
                strategy: this.strategy,
                root: root,
                orientation: root.getAttribute('aria-orientation') || 'vertical',
                multiselect: root.getAttribute('aria-multiselectable') === 'true',
                manualselect: root.dataset.manualselect === 'true',
                items: this.getRovingItems(root)
            };

            this.initWidget(widget);
            this._widgets.set(root, widget);

            return true;
        }

        return false;
    }

    getRootElement(element) {
        return element.closest(this.strategy.rootSelector);
    }

    getRovingItems(root) {
        return Array.from(root.querySelectorAll(this.strategy.itemSelector));
    }

    initWidget(widget) {
        this.applyRoving(widget);
        this.initWidgetCore(widget);
    }

    initWidgetCore(widget) {
        // Overwrite this to do something with widget.root
    }

    applyRoving(widget) {
        const items = widget.items;
        // ...
    }

    handleKey(e) {
        const item = e.target;
        const root = this.getRootElement(item);
        if (!root) return false;

        const widget = this.getWidget(root, true); // add if not present
        if (!widget || !widget.items.length) return false;

        this.handleKeyCore(e, widget);
    }

    handleKeyCore(e, widget) {
        // Overwrite to do something special
    }
}

class TestRadioGroupPlugin extends TestPluginBase {
    constructor(strategy) {
        // { name: 'radiogroup', rootSelector: '[role="radiogroup"]', itemSelector: 'input[type="radio"]:not([disabled])' }
        super(strategy);
    }

    initWidgetCore(widget) {
        // Make radiogroup focusable itself (fallback if options are removed dynamically)
        if (!widget.root.hasAttribute('tabindex')) {
            widget.root.tabIndex = -1;
        }
    }

    handleKeyCore(e) {
        // Do something special for radio group
    }
}

// Boot
(function () {
    // Register default strategies
    AccessKit.register({
        ctor: TestRadioGroupPlugin.ctor,
        name: 'radiogroup',
        rootSelector: '[role="radiogroup"]',
        itemSelector: 'input[type="radio"]:not([disabled])',
        // More stuff, e.g. match() ??
    });
    
    document.addEventListener('DOMContentLoaded', () => {
        window.AK = AccessKit.create(window.AccessKitOptions || {});
    });
})();