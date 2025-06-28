class AccessKit {
    static #key = null;
    static #strategies = null;
    static #plugins = null;

    #strategies = [];
    #plugins = new Map();
    #rtl = document.documentElement.dir === 'rtl';
    #key = {
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

    static register(strategy) {
        this.#strategies.push(strategy);
    }

    constructor() {
        document.addEventListener('keydown', e => this.onKeyDown(e), true);
        document.addEventListener('keyup', e => this.onKeyUp(e), true);
    }

    isCandidateElement(el) {
        // ...
        return false;
    };

    isNavKey(key) {
        //// Exit if no navigational key is pressed.
        //// TODO: (wcag) (mh) Use a static Set for key codes instead of an array, or find another faster way to lookup.
        //if (![AK.KEY.TAB, AK.KEY.UP, AK.KEY.DOWN, AK.KEY.LEFT, AK.KEY.RIGHT, AK.KEY.HOME, AK.KEY.END, AK.KEY.ENTER, AK.KEY.SPACE, AK.KEY.ESC].includes(e.key))
        //    return;

        return false;
    }

    onKeyDown(e) {
        // Skip irrelevant targets immediately.
        const t = e.target;
        if (!t || !(t instanceof Element)) return;
        if (e.key != KEY.ESC) {
            if (!this.isCandidateElement(t)) return;
        }

        if (!this.isNavKey(e.key)) return;

        // Init plugin instance if needed.
        this.tryCreateInstance(t);

        // Dispatch event to all already active plugins.
        this.dispatchKey(e);
    }

    onKeyUp(e) {
    }

    matchStrategy(strategy, target) {
        if (strategy.itemSelector && !target.matches(strategy.itemSelector)) {
            return null;
        }

        return target.closest(strategy.rootSelector);
    }

    tryCreateInstance(target) {
        for (const strategy of this.#strategies) {
            let root = matchStrategy(strategy, target);

            if (!root)
                continue;

            if (!this.#plugins.has(strategy.name)) {
                const instance = new strategy.ctor(strategy);

                // Add the first widget to the instance here already
                instance.addWidget(root);

                this.#plugins.set(strategy.name, instance);
            }

            return;
        }
    }

    dispatchKey(e) {
        const hook = e.type === 'keydown' ? 'handleKey' : 'handleKeyUp';

        for (const plugin of this.#plugins.values()) {
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

AccessKit.register({
    ctor: TestRadioGroupPlugin.ctor,
    name: 'radiogroup',
    rootSelector: '[role="radiogroup"]',
    itemSelector: 'input[type="radio"]:not([disabled])',
    // More stuff, e.g. match() ??
});