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
    static CANDIDATE_SELECTOR = 'a, button, [role], [tabindex]';
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
    _listening = false;

    static get instance() {
        if (!this.#instance) throw new Error('AccessKit instance not created yet. Call AccessKit.create() first.');
        return this.#instance;
    }

    static get isReady() {
        return this.#instance !== null;
    }

    static register(strategy) {
        if (strategy) Array.isArray(strategy) ? this.#strategies.push(...strategy) : this.#strategies.push(strategy);
    }

    static create(options) {
        if (this.#instance) return this.#instance;
        this.#instance = new this(options);

        const k = AccessKit.KEY;
        this.#navKeys = new Set([k.TAB,k.UP, k.DOWN, k.LEFT, k.RIGHT, k.HOME, k.END, k.ENTER, k.SPACE, k.ESC]);

        // Add more strategies as needed...
        return this.#instance;
    }

    static #isNavKey(key) {
        return this.#navKeys.has(e.key);
    }

    constructor(options) {
        this.options = options;
        this.plugins = new Map();

        document.addEventListener('keydown', this._onKeyDown, true);
        document.addEventListener('keyup', this._onKeyUp, true);

        // Generic event handler for collapsibles
        ['expand.ak', 'collapse.ak'].forEach(eventName => {
            document.addEventListener(eventName, (e) => {
                if (!this._listening) return;
                e.target.click();
            });
        });

        // Handle .nav-collapsible aria-expanded attribute on page resize
        this._initCollapsibles();

        // Handle .btn-skip-content button click
        this._initContentSkipper();

        // Start listening to key events
        this.startListen();
    }

    startListen() {
        this._listening = true;
    }

    stopListen() {
        this._listening = false;
    }

    get isListening() {
        return this._listening;
    }

    _isCandidateElement(el) {
        if (el.tagName == 'TEXTAREA' || el.isContentEditable) {
            return false;
        }
        if (el.tagName == 'INPUT') {
            return !AccessKit.TEXT_INPUT_TYPES.has(el.type);
        }

        return el.matches(AccessKit.CANDIDATE_SELECTOR);
    };

    _onKeyDown(e) {
        if (!this._listening) return;

        // Skip irrelevant targets immediately.
        const t = e.target;
        if (!t || !(t instanceof Element)) return;
        if (e.key != KEY.ESC) {
            if (!this._isCandidateElement(t)) return;
        }

        // Exit if no navigational key is pressed.
        if (!AccessKit.#isNavKey(e.key)) return;

        // Init plugin instance if needed.
        this._tryCreateInstance(t);

        // Dispatch event to all already active plugins.
        this._dispatchKey(e);
    }

    _onKeyUp(e) {
        if (!this._listening) return;

        // ...
    }

    _matchStrategy(strategy, target) {
        if (strategy.itemSelector && !target.matches(strategy.itemSelector)) {
            return null;
        }

        return target.closest(strategy.rootSelector);
    }

    _tryCreateInstance(target) {
        for (const strategy of AccessKit.#strategies) {
            let root = this._matchStrategy(strategy, target);

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

    _dispatchKey(e) {
        const hook = e.type === 'keydown' ? 'handleKey' : 'handleKeyUp';

        for (const plugin of this.plugins.values()) {
            const handler = plugin[hook];
            // Check whether plugin implements the handler method & if it returns true (handled).
            if (typeof handler === 'function' && handler.call(plugin, e)) {
                // Preserve natural Tab behaviour, but prevent default for all other keys.
                if (e.type === 'keydown' && e.key !== AccessKit.KEY.TAB) {
                    e.preventDefault();
                }

                e.stopPropagation();
                return;
            }
        }
    }

    _initCollapsibles() {
        // Handle .nav-collapsible aria-expanded attribute on page resize
        const setCollapsibleState = (viewport) => {
            const toggles = document.querySelectorAll('.nav-collapsible > [data-toggle="collapse"]');
            const isLargeScreen = viewport.is('>=md');
            toggles.forEach(el => {
                if (isLargeScreen) {
                    el.removeAttribute('role');
                    el.removeAttribute('aria-expanded');
                    el.setAttribute('data-aria-controls', el.getAttribute('aria-controls'));
                    el.removeAttribute('aria-controls');
                    el.removeAttribute('tabindex');
                }
                else {
                    el.setAttribute('aria-expanded', !el.matches('.collapsed'));
                    el.setAttribute('aria-controls', el.getAttribute('data-aria-controls'));
                    el.removeAttribute('data-aria-controls');
                    el.setAttribute('tabindex', '0');
                }
            });
        };

        EventBroker.subscribe("page.resized", function (_, viewport) {
            setCollapsibleState(viewport);
        });

        setCollapsibleState(ResponsiveBootstrapToolkit);
    }

    _initContentSkipper() {
        document.addEventListener('click', (e) => {
            const trigger = e.target.closest('.btn-skip-content');
            if (!trigger) return;

            e.preventDefault();

            const href = trigger.getAttribute('href') ?? '';
            let target = null;

            if (href.startsWith('#')) {
                // Classic Skip‑Link: #id
                target = document.getElementById(href.slice(1));
            }
            else {
                // Find next element sibling of container
                let pointer = trigger.parentElement;
                while (pointer && !pointer.nextElementSibling) {
                    pointer = pointer.parentElement;
                }
                target = pointer?.nextElementSibling ?? null;
            }

            if (target) {
                // Remove visual focus
                trigger.blur();
                // Make target element focusable
                //target.setAttribute('tabindex', '-1');
                // For screenreader
                target.focus({ preventScroll: true });
                // Scroll smoothly
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    }
}

class AccessKitEvents {
    EVENT_NAMESPACE = '.ak';

    constructor() {
        this._handles = [];
    }

    /**
     * Add event listener to element and store the handle for later removal.
     * @param {Element} element
     * @param {Event} evt
     * @param {Function} fn
     * @param {EventListenerOptions} opts
     */
    on(element, evt, fn, opts) {
        element.addEventListener(evt, fn, opts);
        this._handles.push(() => element.removeEventListener(evt, fn, opts));
    }

    /**
     * Event publisher that is compatible with both vanilla JS and jQuery event handling mechanism.
     * @param {String} name
     * @param {Element} element
     * @param {Object} args
     * @returns {CustomEvent}
     */
    trigger(name, element, args = {}) {
        // TODO: (wcag) (mc) Move trigger method to a common place/script.
        if (!name.endsWith(EVENT_NAMESPACE)) {
            name += EVENT_NAMESPACE;
        }

        const jEvent = $.Event(name, { detail: args });
        $(element).trigger(jEvent);

        const bubbles = !jEvent.isPropagationStopped();
        const nativeDispatch = !jEvent.isImmediatePropagationStopped();
        const defaultPrevented = jEvent.isDefaultPrevented();

        const event = new CustomEvent(name, { bubbles, detail: args, cancelable: true });

        if (defaultPrevented) {
            event.preventDefault();
        }

        if (nativeDispatch) {
            element.dispatchEvent(event);
        }

        if (event.defaultPrevented) {
            jEvent.preventDefault();
        }

        return event;
    }

    /**
     * Remove all event listeners.
    */
    destroy() {
        this._handles.forEach(off => off());
    }
}


/*
* Plugin base class for AccessKit plugins.
*/
class AccessKitPluginBase {
    constructor(strategy) {
        this.strategy = strategy;
        this._widgets = new WeakMap();
        this._events = new AccessKitEvents();
    }

    get widgets() {
        return this._widgets;
    }

    get events() {
        return this._events;
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
                items: this.getRovingItems(root),
                rtl: AccessKit.RTL
            };

            this.initWidget(widget);
            this._widgets.set(root, widget);

            return true;
        }

        return false;
    }

    refreshWidget() {
        // TODO: (wcag) (mc) Implement refreshWidget() to update the widget items.
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

    /**
     * Apply roving tabindex to all elements matching the selector within the root element.
     * 
     * @param {Object} widget
     * 
     * @returns {Array<Element>} items - The roving focusable items.
     */
    applyRoving(widget) {
        /* Build a safe scope for roving-focus:
           1. If the container has a role ⇒ use [role="…"].
           2. Else if it has an id        ⇒ use #id.
           3. Otherwise no selector, fall back to root.contains().
           Keep only items whose closest() match equals the container. */

        //// TODO: (wcag) Remove this legacy code? Or apply to relevant class(es) only.
        //const root = widget.root;
        //const role = root.getAttribute('role');
        //const scopeSelector = role ? `[role="${CSS.escape(role)}"]` : root.id ? `#${CSS.escape(root.id)}` : null;
        //const items = [...root.querySelectorAll(selector)].filter(el => {
        //    return scopeSelector ? el.closest(scopeSelector) === root : root.contains(el);
        //});

        const items = widget.items;
        items.forEach((el, i) => el.tabIndex = i === start ? 0 : -1);

        return items;
    }

    /**
    * Key handler for plugins using a roving tabindex list 
    * @param {KeyboardEvent}  e        – Original event
    * @param {Object}  widget          – Currently active widget
    * @param {Object} [options]        – Optional configuration
    *        onActivate    function(el, idx, widget)    Handler for ENTER/SPACE
    *        onKey         function(e, idx, widget)     Extra key handler for plugins
    *
    * @returns {boolean}   true → Event processed, false → delegate to browser
    */
    handleRovingKeys(e, widget, { onActivate = null, onKey = null } = {}) {
        // TODO: (wcag) (mh) Research why items are of type NodeList after AJAX-Updates (e.g. in product detail variant update)
        var items = widget.items;
        if (items instanceof NodeList) {
            items = [...items];
        }

        const idx = items.indexOf(e.target);
        if (idx === -1) return false;

        // Determine navigation keys depending on orientation & rtl
        const k = AccessKit.KEY;
        const [PREV_KEY, NEXT_KEY] = this._getNavKeys(widget);

        switch (e.key) {
            /* Navigate ------------------------------------------------------- */
            case PREV_KEY:
                return this.move(items[this._nextIdx(idx, -1, items.length)], widget);
            case NEXT_KEY:
                return this.move(items[this._nextIdx(idx, +1, items.length)], widget);
            case k.HOME:
                return this.move(items[0], widget);
            case k.END:
                return this.move(items[items.length - 1], widget);

            /* Activate (ENTER / SPACE) -------------------------------------- */
            case k.ENTER:
            case k.SPACE:
                if (typeof onActivate === 'function') {
                    onActivate(e.target, idx, widget);
                    return true;
                }
                break;
        }

        // Plugin specific extra keys
        if (typeof onKey === 'function') {
            return onKey(e, idx, widget) === true;
        }

        return false;
    }

    handleKey(e) {
        const item = e.target;
        const root = this.getRootElement(item);
        if (!root) return false;

        const widget = this.getWidget(root, true); // add if not present
        if (!widget || !widget.items.length) return false;

        this.handleKeyCore(e, widget);
    }

    /**
     * Handle 'keydown' or 'keyup' event.
     * Must return `true` if the event has been processed (AccessKit then calls preventDefault / stopPropagation).
     * 
     * @param {KeyboardEvent} e
     * @param {Object} widget
     * 
     * @returns {Boolean} handled?
     */
    handleKeyCore(e, widget) {
        // Overwrite to do something special
    }

    /**
     * Overridable move base implementation.
     * @param {Element} target
     * @param {Object} widget
     */
    move(target, widget) {
        return this.moveFocus(target, widget);
    }

    /**
     * Move focus to the target element and set roving tabindex.
     * @param {Element} target
     * @param {Object} widget
     */
    moveFocus(target, widget) {
        if (!target) return false;
        widget.items.forEach(i => i.tabIndex = -1);
        target.tabIndex = 0;
        target.focus();
        return true;
    }

    removeFocus(el) { }

    /**
     * Gets directional keys based on menubar or menu  aria-orientation attribute & rtl
     * @param {Boolean} rtl
     */
    _getNavKeys(widget) {
        const k = AccessKit.KEY;
        return widget.orientation === 'horizontal'
            ? (widget.rtl ? [k.RIGHT, k.LEFT] : [k.LEFT, k.RIGHT])
            : [k.UP, k.DOWN];
    }

    // Returns the next index in a circular manner.
    _nextIdx(cur, delta, len) {
        return (cur + delta + len) % len;
    }

    /**
     * Remove all event listeners that were registered by this plugin.
    */ 
    destroy() {
        this.events.destroy();
    }
}


/**
 * Base plugin for accessible expandable elements (tree, menubar, combobox, disclosure, accordion).
 */
class AccessKitExpandablePluginBase extends AccessKitPluginBase {
    /**
    * Opens, closes or toggles an expand/collapse trigger.
    * @param {HTMLElement} trigger   Element with aria-expanded
    * @param {boolean|null} expand   true = open, false = close, null = Toggle state (default)
    * @param {Object} [opt]
    *        focusTarget      'first' | 'trigger' | HTMLElement | null
    *        collapseSiblings boolean – Close all siblings on opening */
    toggleExpanded(trigger, expand = null, opt = {}) {
        if (!trigger) return;

        // Determine targets
        let target;
        if (trigger.hasAttribute('aria-controls')) {
            target = document.getElementById(trigger.getAttribute('aria-controls'));
        }
        else {
            // Fallback: <button> … <div class="panel">
            target = trigger.nextElementSibling;
        }
        if (!target) return;

        var isOpen = trigger.getAttribute('aria-expanded') === 'true' || trigger.open === true;
        var shouldOpen = expand === null ? !isOpen : Boolean(expand);

        // Dispatch event so consumers can execute their special open/close mechanisms if they have to.
        this.events.trigger(shouldOpen ? 'expand' : 'collapse', trigger, { trigger, target });

        // Wait for transition end to set focus
        async function waitForTransitions(el) {
            if (!el) return;
            const trans = el.getAnimations().filter(a => a instanceof CSSTransition);
            if (trans.length === 0) return;

            await Promise.all(
                trans.map(a => a.finished.catch(() => { }))
            );
        }

        if (shouldOpen) {
            waitForTransitions(target).then(setAttrsAndFocus);
        }
        else {
            setAttrsAndFocus();
        }

        function setAttrsAndFocus() {
            // Set attributes & visibilty
            if (trigger.hasAttribute('aria-expanded')) {
                trigger.setAttribute('aria-expanded', shouldOpen ? 'true' : 'false');
            } else if ('open' in trigger) {
                trigger.open = shouldOpen;
            }

            // TODO: (wcag) (mh) Don't do this! This should be handled by the event consumer. Remove after testing in all expandable plugins.
            //if (target) target.hidden = !shouldOpen;

            // Accordeon mode > toggle siblings
            if (shouldOpen && opt.collapseSiblings && trigger.parentElement) {
                const peers = trigger.parentElement.querySelectorAll('[aria-expanded="true"],[open]');
                peers.forEach((p) => {
                    if (p !== trigger) this.toggleExpanded(p, false);
                });
            }

            // Focus
            if (target) {
                let focusEl = null;
                if (opt.focusTarget === 'first') {
                    // TODO: (wcag) (mh) This smells :-)
                    focusEl = target.querySelector(':is([tabindex], button, a, input, select, textarea):not([tabindex="-1"])');
                } else if (opt.focusTarget instanceof HTMLElement) {
                    focusEl = opt.focusTarget;
                } else if (opt.focusTarget === 'trigger') {
                    focusEl = trigger;
                }

                focusEl?.focus();
            }

            // Set attributes
            trigger.setAttribute('aria-expanded', shouldOpen ? 'true' : 'false');

            if (target) {
                target.setAttribute('aria-hidden', shouldOpen ? 'false' : 'true');
            }
        }
    }
}


/* --------------------------------------------------
*  TablistPlugin – Roving‑Tabindex & Panel Switching
*  Handles all items of [role="tablist"] + [role="tab"] + [role="tabpanel"]
* -------------------------------------------------- */
class TablistPlugin extends AccessKitPluginBase {
    initWidgetCore(widget) {
        const selectedTab = widget.items.find(t => t.getAttribute('aria-selected') === 'true') || widget.items[0];
        this.select(selectedTab, widget, false);
    }

    handleKeyCore(e, widget) {
        return this.handleRovingKeys(e, widget, {
            onActivate: (el) => this.select(el),
        });
    }

    // Shift roving focus to the next tab.
    move(tab, widget) {
        if (!tab) return;
        super.move(tab, widget);
        this.select(tab, widget, false); // Do not display tabpanel on move.
    }

    // Update aria attributes & optionally select/display tab panel.
    select(tab, widget, displayPanel = true) {
        if (!tab) return;

        // Update tab attributes.
        widget.items.forEach(t => {
            const selected = t === tab;
            t.setAttribute('aria-selected', selected ? 'true' : 'false');
            t.setAttribute('tabindex', selected ? 0 : -1);
        });

        // Shift focus and display tabpanel by dispatching the click event on tab.
        if (displayPanel) {
            tab.dispatchEvent(new Event('click', { bubbles: true, cancelable: true }));
            document.getElementById(tab.getAttribute('aria-controls'))?.focus();
        }
    }
}


/* --------------------------------------------------
 *  RadiogroupPlugin – Roving‑Tabindex & Selection.
 *  Handles widgets using [role="radiogroup"] and input[type="radio"] children.
 *  Supports only single‑‑select.
 * -------------------------------------------------- */
class RadioGroupPlugin extends AccessKitPluginBase {
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

    handleKeyCore(e, widget) {
        if (!widget.items?.length) return false;

        return this.handleRovingKeys(e, widget, {
            onActivate: (el) => el.click(),
            onKey: (ev) => {
                // INFO: This prevents default browser navigation
                // which is possible for radiogroups via ↑ & ← for backward and ↓ & → for forward navigation with no regard of orientation
                return !this._getNavKeys(widget).includes(ev.key);
            }
        });
    }

    move(target, widget) {
        super.move(target, widget);

        // TODO: (wcag) (mh) Evaluate if this is needed
        // In radiogroups, moving also selects (if manualselect is not true)
        if (!widget?.manualselect) target.click();
    }
}

// Boot
(function () {
    // Register default strategies
    AccessKit.register([
        {
            ctor: RadioGroupPlugin.ctor,
            name: 'radiogroup',
            rootSelector: '[role="radiogroup"]',
            itemSelector: 'input[type="radio"]:not([disabled])'
        },
        {
            ctor: TablistPlugin.ctor,
            name: 'tablist',
            rootSelector: '[role="tablist"]',
            itemSelector: '[role="tab"]'
        },
    ]);
    
    document.addEventListener('DOMContentLoaded', () => {
        window.AK = AccessKit.create(window.AccessKitOptions || {});
    });
})();