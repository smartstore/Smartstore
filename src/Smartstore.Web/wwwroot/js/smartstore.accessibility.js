﻿/**
* WCAG‑2.2 keyboard framework
* TODO: (wcag) (mh) Docs
*/
'use strict';

/* --------------------------------------------------
*  AccessKit core – plugin host + key dispatcher
* -------------------------------------------------- */
class AccessKit {
    static _registry = [];
    static _textInputTypes = new Set(['text', 'email', 'tel', 'url', 'search', 'password', 'date', 'datetime-local', 'datetime', 'month', 'number', 'time', 'week']);
    static _activeOptionSelector = '[role="option"]:not(:is([disabled], .disabled, .hidden, [aria-disabled="true"]))';
    static _activeRadioSelector = 'input[type="radio"]:not([disabled])';

    /**
    * Add a plugin descriptor to the global plugin registry of AccessKit
    *
    * @param {Object} param
    * @param {Function} param.ctor          Plugin constructor.
    * @param {Function=} param.match        Matcher — receives the focused element and must return the widget’s
    *                                       root element or null. Use when the widget is not in the ancestor chain (e.g. menu dropdowns).
    *
    * The descriptor is stored in `AccessKit._registry` and later queried by `_findMatchingPlugin` to lazily instantiate plugins.
    */
    static register({ ctor, match }) {
        this._registry.push({ ctor, match });
    }

    constructor(options = {}) {
        this.options = options;
        this.rtl = options.rtl ?? (document.documentElement.dir === 'rtl');
        this._plugins = new Map();

        /* one keydown/keyup listener for relevant elements – capture phase */
        document.addEventListener('keydown', e => this._onKeyDown(e), true);
        document.addEventListener('keyup', e => this._onKeyUp(e), true);

        // Handle .nav-collapsible aria-expanded attribute on page resize
        this._initCollapsibles();

        // Handle .btn-skip-content button click
        this._initContentSkipper();

        // Generic event handler for collapsibles
        ['expand.ak', 'collapse.ak'].forEach(eventName => {
            document.addEventListener(eventName, (e) => {
                e.target.click();
            });
        });
    }

    _isCandidateElement(el) {
        if (el.tagName == 'TEXTAREA' || el.isContentEditable) {
            return false;
        }
        if (el.tagName == 'INPUT') {
            return !AccessKit._textInputTypes.has(el.type);
        }

        return el.matches('a,button,[role],[tabindex]');
    };

    _onKeyDown(e) {
        // Skip irrelevant targets immediately.
        const t = e.target;
        if (!t || !(t instanceof Element)) return;
        if (e.key != AK.KEY.ESC) {
            if (!this._isCandidateElement(t)) return;
        }

        // Exit if no navigational key is pressed.
        // TODO: (wcag) (mh) Use a static Set for key codes instead of an array, or find another faster way to lookup.
        if (![AK.KEY.TAB, AK.KEY.UP, AK.KEY.DOWN, AK.KEY.LEFT, AK.KEY.RIGHT, AK.KEY.HOME, AK.KEY.END, AK.KEY.ENTER, AK.KEY.SPACE, AK.KEY.ESC].includes(e.key))
            return;

        // Init plugin if needed.
        this._findMatchingPlugin(t);
        
        // Dispatch event to all already active plugins.
        this._dispatchKey(e);
    }

    _onKeyUp(e) {
        // TODO: (wcag) (mh) (mc) ???
        // ...
    }

    /**
    * Lazily boot the first plugin whose root contains the currently-focused element.
    * 
    * If the focused element belongs to a widget that has no plugin yet,
    * try to match the element, create ONE instance of the corresponding ctor, store it in this._plugins, then exit.
    * 
    * @param {Element} target  element that holds keyboard focus (event.target from a key event)
    */
    _findMatchingPlugin(target) {
        for (const plugin of AccessKit._registry) {
            let root = typeof plugin.match === 'function' ? plugin.match(target) : null;

            if (!root)
                continue;

            if (!this._plugins.has(plugin.ctor)) {
                const instance = new plugin.ctor(this, root);
                this._plugins.set(plugin.ctor, instance);
            }

            return;         
        }
    }

    _dispatchKey(e) {
        const hook = e.type === 'keydown' ? 'handleKey' : 'handleKeyUp';

        for (const plugin of this._plugins.values()) {
            const handler = plugin[hook]; 
            // Check whether plugin implements the handler method & if it returns true (handled).
            if (typeof handler === 'function' && handler.call(plugin, e)) {
                // Preserve natural Tab behaviour, but prevent default for all other keys.
                if (e.type === 'keydown' && e.key !== KEY.TAB) {
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

    /**
    * Refreshes the content of a live region to ensure that a screen reader does not skip it.
    * Required if the live region is updated via AJAX or if its content should be initially read after a page load.
    * 
    * @param {string} id  ID of element to update (has role="status" or role="alert")
    */
    static refreshLiveRegion(id) {
        const el = document.getElementById(id);
        if (el) {
            const content = el.textContent;
            if (content.length) {
                //el.textContent += '\u2060';
                el.textContent = '';
                setTimeout(() => { el.textContent = content; }, 50);
            }
        }
    }

    /**
    * Informs screen readers of a status, such as "Please wait...".
    * 
    * @param {string} message  The message to set.
    * @param {string} busyId   Optional ID of an element to set "aria-busy=true". Removes "aria-busy" if the message is empty.
    */
    static notifyStatus(message, busyId) {
        const el = document.getElementById('sr-status');
        if (el) {
            el.textContent = message || '';
            if (busyId) {
                const elBusy = document.getElementById(busyId);
                if (elBusy) {
                    message?.length ? elBusy.setAttribute('aria-busy', 'true') : elBusy.removeAttribute('aria-busy');
                }
            }
        }
    }
}

// Global namespace
window.AccessKit = window.AccessKit || AccessKit;
const AK = window.AccessKit;

AK.KEY = {
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

// TODO: (wcag) (mh) Replace every KEY in this script with AK.KEY > Then remove the shortcut KEY
const KEY = AK.KEY;

// Plugin base class for AccessKit plugins.
// TODO: (wcag) (mh) Create a separate file for every base class and every plugin.
// TODO: (wcag) (mh) Use this closure for every modular scripts when you place them in separate files.
(function (AK) {
    const EVENT_NAMESPACE = '.ak';

    AK.AccessKitPluginBase = class AccessKitPluginBase {
        constructor(ak) {
            this.ak = ak;
            this._handles = [];
            this._cache = new WeakMap();
            this.init(document);
        }

        // Will be called at initialization of the plugin and can be called after AJAX updates to re-initialize the plugin.
        // @param {Element|Document} container => context
        init(container = document) { }

        // Handle 'keydown' events .
        // Must return `true` if the event has been processed (AccessKit then calls preventDefault / stopPropagation).
        // @param {KeyboardEvent} e
        // @returns {Boolean} handled?
        handleKey(e) { return false; }

        // Optional addition for 'keyup' cases (e.g. cancel gestures, space-to-click semantics). Only implement if needed.
        // @param {KeyboardEvent} e
        // @returns {Boolean} handled?
        handleKeyUp(e) { return false; }

        // Apply roving tabindex to all elements matching the selector within the root element.
        applyRoving(root, selector, start = 0) {
            /* Build a safe scope for roving-focus:
               1. If the container has a role ⇒ use [role="…"].
               2. Else if it has an id        ⇒ use #id.
               3. Otherwise no selector, fall back to root.contains().
               Keep only items whose closest() match equals the container. */
            const role = root.getAttribute('role');
            const scopeSelector = role ? `[role="${CSS.escape(role)}"]` : root.id ? `#${CSS.escape(root.id)}` : null;                               
            const items = [...root.querySelectorAll(selector)].filter(el => {
                return scopeSelector ? el.closest(scopeSelector) === root : root.contains(el);
            });

            items.forEach((el, i) => el.tabIndex = i === start ? 0 : -1);
            return items;
        }

        // Move focus to the target element and set roving tabindex.
        moveFocus(target, items) {
            if (!target) return;
            items.forEach(i => i.tabIndex = -1);
            target.tabIndex = 0;
            target.focus();
        }

        removeFocus(el) {}

        // Remove all event listeners that were registered by this plugin.
        destroy() { this._handles.forEach(off => off()); }

        // Set cache 
        _setCache(key, value) { this._cache.set(key, value); }

        // Get cache item if it exists else call callback as fallback. 
        _getCache(key, callback = null) {
            if (this._cache.has(key)) return this._cache.get(key);
            if (callback) {
                const value = callback();
                this._cache.set(key, value);
                return value;
            }
            return undefined;
        }

        // Gets directional keys based on menubar or menu  aria-orientation attribute & rtl
        _getNavKeys(orientation, rtl = false) {
            return orientation === 'horizontal'
                ? (rtl ? [KEY.RIGHT, KEY.LEFT] : [KEY.LEFT, KEY.RIGHT])
                : [KEY.UP, KEY.DOWN];
        }

        // Returns the next index in a circular manner.
        _nextIdx(cur, delta, len) {
            return (cur + delta + len) % len;
        }

        // Add event listener to element and store the handle for later removal.
        on(el, evt, fn, opts) {
            el.addEventListener(evt, fn, opts);
            this._handles.push(() => el.removeEventListener(evt, fn, opts));
        }

        trigger(name, element, args = {}) {
            // Event publisher that is compatible with both vanilla JS and jQuery event handling mechanism.
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
        * Key handler for plugins using a roving tabindex list 
        * @param {KeyboardEvent}  e        – Original event
        * @param {HTMLElement[]}  items    – Currently visible/focusable list elements
        * @param {Object} [cfg]            – Optional configuration
        *        orientation   'vertical' | 'horizontal'   (Default: 'vertical')
        *        activateFn    function(el, idx, items)    Callback for ENTER/SPACE
        *        extraKeysFn   function(e, idx, items)     Special keys for plugins
        *
        * @returns {boolean}   true → Event processed, false → delegate to browser
        */
        handleRovingKeys(e, items, { orientation = 'vertical', activateFn = null, extraKeysFn = null } = {}) {
            // TODO: (wcag) (mh) Research why items are of type NodeList after AJAX-Updates (e.g. in product detail variant update)
            if (items instanceof NodeList) {
                items = [...items];
            }

            const idx = items.indexOf(e.target);
            if (idx === -1) return false;

            // Determine navigation keys depending on orientation & rtl
            const rtl = this.ak?.rtl === true;
            const [PREV_KEY, NEXT_KEY] = this._getNavKeys(orientation, rtl);

            switch (e.key) {
                /* Navigate ------------------------------------------------------- */
                case PREV_KEY:
                    this._move(items[this._nextIdx(idx, -1, items.length)], null, items);
                    return true;
                case NEXT_KEY:
                    this._move(items[this._nextIdx(idx, +1, items.length)], null, items);
                    return true;
                case KEY.HOME:
                    this._move(items[0], null, items);
                    return true;
                case KEY.END:
                    this._move(items[items.length - 1], null, items);
                    return true;

                /* Activate (ENTER / SPACE) -------------------------------------- */
                case KEY.ENTER:
                case KEY.SPACE:
                    if (typeof activateFn === 'function') {
                        activateFn(e.target, idx, items);
                        return true;
                    }
                    break;
            }

            // Plugin specific extra keys
            if (typeof extraKeysFn === 'function') {
                return extraKeysFn(e, idx, items) === true;
            }

            return false;
        }

        // Overridable base implementation.
        _move(target, _root = null, items = []) {
            this.moveFocus(target, items);
        }
    }
})(window.AccessKit);

// Base plugin for accessible expandable elements (tree, menubar, combobox, disclosure, accordion).
AK.AccessKitExpandablePluginBase = class AccessKitExpandablePluginBase extends AK.AccessKitPluginBase {
    /**
    * Opens, closes or toggles an expand/collapse trigger.
    * @param {HTMLElement} trigger   Element mit aria-expanded oder open
    * @param {boolean|null} expand   true = open, false = close, null = Toggle state (default)
    * @param {Object} [opt]
    *        focusTarget      'first' | 'trigger' | HTMLElement | null
    *        collapseSiblings boolean – Close all siblings on opening */
    toggleExpanded(trigger, expand = null, opt = {}) {
        if (!trigger) return;

        // Determine targets
        var target = null;
        if (trigger.hasAttribute('aria-controls')) {
            target = document.getElementById(trigger.getAttribute('aria-controls'));
        } else if (trigger.nextElementSibling) {
            // Fallback: <button> … <div class="panel">
            target = trigger.nextElementSibling;
        }

        var isOpen = trigger.getAttribute('aria-expanded') === 'true' || trigger.open === true;
        var shouldOpen = expand === null ? !isOpen : Boolean(expand);

        // Dispatch event so consumers can execute their special open/close mechanisms if they have to.
        this.trigger(shouldOpen ? 'expand' : 'collapse', trigger, { trigger, target });

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
*  MenuPlugin – Roving‑Tabindex & Sub‑menu handling
*  Handles all items of role="menubar" based on subitems role="menu" & role="menuitem".
* -------------------------------------------------- */
AK.MenuPlugin = class MenuPlugin extends AK.AccessKitExpandablePluginBase {
    init(container = document) {
        // TODO: (wcag) (mh) Slow!
        container.querySelectorAll('[role="menubar"]').forEach(menubar => {
            const menuitems = this.applyRoving(menubar, '[role="menuitem"]');   
            this._setCache(menubar, menuitems);
        });
    }

    _items(container) {
        // TODO: (wcag) (mh) Slow!
        //return this._getCache(container, () =>
        //    [...container.querySelectorAll('[role="menuitem"]')]
        //        .filter(mi => mi.closest('[role="menubar"], [role="menu"]') === container)
        //    );

        // If cached already return immediately
        let items = this._getCache(container);
        if (items) return items;            

        // Get items and store them in cache.
        items = [...container.querySelectorAll('[role="menuitem"]')]
            .filter(mi => mi.closest('[role="menubar"],[role="menu"]') === container);
        
        this._setCache(container, items);
        
        return items;
    }

    handleKey(e) {
        const el = e.target;

        if (!el || el.getAttribute('role') !== 'menuitem')
            return false;

        // TODO: (wcag) (mh) Slow!
        const menubar = el.closest('[role="menubar"]');
        if (menubar) return this._menuKey(e, menubar, true);

        // TODO: (wcag) (mh) Slow!
        const submenu = el.closest('[role="menu"]');
        if (submenu) return this._menuKey(e, submenu);

        return false;
    }

    _menuKey(e, menu, isMenubar = false) {
        const items = this._items(menu);
        if (!items.length) return false;

        const orientation = menu.getAttribute('aria-orientation') ?? (isMenubar ? 'horizontal' : 'vertical');
            
        return this.handleRovingKeys(e, items, {
            orientation,

            /** ENTER/SPACE → Open submenu or execute command */
            activateFn: (item) => {
                if (item.getAttribute('aria-haspopup') === 'menu') {
                    this._open(item);
                } else {
                    item.click();          // Command
                }
            },

            /** Menu specific keys (Open, close, TAB) */
            extraKeysFn: (ev) => {
                const isVertical = orientation === 'vertical';
                const dirOpen = isMenubar ? null : isVertical ? this.ak.rtl ? KEY.LEFT : KEY.RIGHT : KEY.DOWN;
                const dirClose = isMenubar ? null : isVertical ? this.ak.rtl ? KEY.RIGHT : KEY.LEFT : KEY.UP;

                /* Open sub menu -------------------------------------- */
                if ((isMenubar && [KEY.DOWN, KEY.SPACE, KEY.ENTER].includes(ev.key)) || (!isMenubar && ev.key === dirOpen)) {
                    if (ev.target.getAttribute('aria-haspopup') === 'menu') {
                        this._open(ev.target);
                        return true;
                    }
                }

                /* Close ---------------------------------------------- */
                if (ev.key === KEY.ESC || ev.key === dirClose) {
                    if (isMenubar) {
                        this._closeAll();
                    } else {
                        const trigger = document.querySelector(`[aria-controls="${menu.id}"]`);
                        this._close(trigger, menu);
                        trigger?.focus();
                    }
                    return true;
                }

                /* TAB leaves the component --------------------------- */
                if (ev.key === KEY.TAB) {
                    if (isMenubar) {
                        // Reset focus & tabIndex
                        items.forEach((it, i) => (it.tabIndex = i === 0 ? 0 : -1));
                    } else {
                        this._closeAll();
                    }
                    return true;
                }

                return false;
            },
        });
    }

    _open(trigger) {
        const menu = document.getElementById(trigger.getAttribute('aria-controls'));
        if (!menu)
            return;

        let items = this._getCache(menu);
        if (!items) {
            items = this.applyRoving(menu, '[role="menuitem"]');
            this._setCache(menu, items);
        }

        this.toggleExpanded(trigger, true, { focusTarget: items[0] });
    }

    _close(trigger, menu) {
        if (!menu || !menu.classList.contains('show'))
            return;

        this.toggleExpanded(trigger, false, { focusTarget: "trigger" });
    }

    _closeAll() {
        document.querySelectorAll('[role="menu"].show').forEach(menu => {
            const trigger = document.querySelector(`[aria-controls="${menu.id}"][aria-expanded="true"]`);
            this.toggleExpanded(trigger, false, { focusTarget: "trigger" });
        });
    }
}

/* --------------------------------------------------
*  TablistPlugin – Roving‑Tabindex & Panel Switching
*  Handles all items of [role="tablist"] + [role="tab"] + [role="tabpanel"]
* -------------------------------------------------- */
AK.TablistPlugin = class TablistPlugin extends AK.AccessKitPluginBase {
    init(container = document) {
        container.querySelectorAll('[role="tablist"]').forEach(list => {
            const tabs = this.applyRoving(list, '[role="tab"]');
            this._setCache(list, tabs); 
            const selectedTab = tabs.find(t => t.getAttribute('aria-selected') === 'true') || tabs[0];
            this._select(selectedTab, false);
        });
    }

    handleKey(e) {
        const tab = e.target;
        if (!tab || tab.getAttribute('role') !== 'tab')
            return false;

        const list = tab.closest('[role="tablist"]');
        if (!list)
            return false;

        const tabs = this._getCache(list, () =>
            this.applyRoving(list, '[role="tab"]')
        );

        const orientation = list.getAttribute('aria-orientation') ?? 'horizontal';

        return this.handleRovingKeys(e, tabs, {
            orientation,
            activateFn: (el) => this._select(el),
        });
    }

    // Shift roving focus to the next tab.
    _move(tab) {
        if (!tab)
            return;

        super._move(tab);
        this._select(tab, false); // Do not display tabpanel on move.
    }

    // Update aria attributes & optionally select/display tab panel.
    _select(tab, displayPanel = true) {
        if (!tab)
            return;

        const tabs = this._getCache(tab.closest('[role="tablist"]'));

        // Update tab attributes.
        tabs.forEach(t => {
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
*  TreePlugin – 
*  Handles all items of [role="tree"] + [role="treeitem"]
* -------------------------------------------------- */
AK.TreePlugin = class TreePlugin extends AK.AccessKitExpandablePluginBase {
    _initRovingTabindex(tree) {
        const items = this._visibleItems(tree);
        items.forEach((item, i) => item.tabIndex = i === 0 ? 0 : -1);
    }

    init(container = document) {
        container.querySelectorAll('[role="tree"]').forEach(tree => {
            const items = this.applyRoving(tree, '[role="treeitem"]');
            this._setCache(tree, items);
        });
    }

    _allItems(tree) {
        return this._getCache(tree, () =>
        [...tree.querySelectorAll('[role="treeitem"]')]
            .filter(it => it.closest('[role="tree"]') === tree));
    }

    _visibleItems(tree) {
        // TODO: (wcag) (mh) Correct this and use cached items. 
        return Array.from(tree.querySelectorAll('[role="treeitem"]')).filter(node => {
            let anc = node.parentElement?.closest('[role="treeitem"]');
            while (anc) {
                if (anc.getAttribute('aria-expanded') === 'false') return false;
                anc = anc.parentElement?.closest('[role="treeitem"]');
            }
            return true; // No collapsed ancestor found
        });
    }

    handleKey(e) {
        const item = e.target;
        if (!item || item.getAttribute('role') !== 'treeitem')
            return false;

        const tree = item.closest('[role="tree"]') || item.closest('[role="group"]');
        if (!tree)
            return false;

        const items = this._visibleItems(tree);
        if (!items.length)
            return false;

        const orientation = tree.getAttribute('aria-orientation') ?? 'vertical';

        return this.handleRovingKeys(e, items, {
            orientation,
            activateFn: (el) => (el.trigger ? el.trigger('click') : el.click()),
            extraKeysFn: (ev, _idx, list) => {
                if (ev.key === KEY.RIGHT) {
                    this.toggleExpanded(item, true, { focusTarget: 'first' });
                    return true;
                }

                if (ev.key === KEY.LEFT || ev.key === KEY.ESC) {
                    this.toggleExpanded(item, false);
                    return true;
                }

                if (ev.key === KEY.TAB) {
                    this.applyRoving(tree, '[role="treeitem"]');
                    return false;
                }

                return false;
            },
        });
    }

    /* -------- Move roving focus -------- */
    _move(item, tree = null, siblings = null) {
        if (!item)
            return;

        tree = tree || item.closest('[role="tree"]');
        siblings = siblings || this._visibleItems(tree);

        super._move(item, siblings);
    }
}

/* --------------------------------------------------
 *  ListboxPlugin – Roving‑Tabindex, Selection & Type‑ahead
 *  Handles widgets using [role="listbox"] with [role="option"] children
 *  Supports single‑ & multi‑select (aria-multiselectable="true")
 * -------------------------------------------------- */
AK.ListboxPlugin = class ListboxPlugin extends AK.AccessKitPluginBase {
    init(container = document) {
        const lists = Array.from(container.querySelectorAll('[role="listbox"]'));
        lists.forEach(list => this._initListbox(list));
    }

    _initListbox(list) {
        // Determine selection mode
        list.dataset.akMultiselect = list.getAttribute('aria-multiselectable');

        // Initialise roving tabindex & ensure aria-selected is set
        const options = this.applyRoving(list, AK._activeOptionSelector);
        this._setCache(list, options);
            options.forEach(opt => {
                if (!opt.hasAttribute('aria-selected')) {
                    opt.setAttribute('aria-selected', 'false');
                }
        });

        // Pointer interaction mirrors keyboard behaviour
        list.addEventListener('click', e => {
            const opt = e.target.closest(AK._activeOptionSelector);
            if (opt && list.contains(opt)) {
                const opts = this._options(list);
                this._move(opt, list, opts);
                this._toggleSelect(opt, list, opts);
            }
        });

        // Make listbox focusable itself (fallback if options are removed dynamically)
        if (!list.hasAttribute('tabindex')) {
            list.tabIndex = -1;
        }
    }

    _options(list) {
        return this._getCache(list, () =>
            [...list.querySelectorAll(AK._activeOptionSelector)]
                .filter(opt => opt.closest('[role="listbox"]') === list)
        );
    }

    handleKey(e) {
        const opt = e.target;
        if (!opt || opt.getAttribute('role') !== 'option')
            return false;

        const list = opt.closest('[role="listbox"]');
        if (!list)
            return false;

        const options = this._options(list);
        if (!options.length)
            return false;

        const orientation = list.getAttribute('aria-orientation') ?? 'vertical';
            
        return this.handleRovingKeys(e, options, {
            orientation,
            activateFn: (el, _idx, opts) => this._toggleSelect(el, list, opts),
            extraKeysFn: (ev, idx, opts) => {
                if (ev.key.length === 1 && /\S/.test(ev.key)) {
                    this._typeahead(ev.key, idx, opts, list);
                    return true;
                }

                return false;
            }
        });
    }

    /* -------- Move roving focus -------- */
    _move(opt, list, options) {
        super._move(opt, null, options);

        // TODO: Evaluate if this is needed 
        // In single‑select listboxes, moving also selects (if manualselect is not true)
        if (list && list.length && list.dataset.akMultiselect !== 'true' && list.dataset.manualselect !== 'true') {
            this._toggleSelect(opt, list, options, /*replace*/ true);
        }
    }

    /* -------- Selection handling -------- */
    _toggleSelect(opt, list, options, replace = false) {
        const multiselect = list.dataset.akMultiselect === 'true';
        if (!multiselect || replace) {
            options.forEach(o => o.setAttribute('aria-selected', o === opt ? 'true' : 'false'));
            this.trigger('select.listbox', list, { list, opt });

            // Call click immediately for single select lists.
            opt.click();
        } else {
            const selected = opt.getAttribute('aria-selected') === 'true';
            opt.setAttribute('aria-selected', selected ? 'false' : 'true');
            this.trigger(selected ? 'deselect.listbox' : 'select.listbox', list, { list, opt });
        }
    }

    // TODO: (wcag) (mh) This seems to be to expensive. We don't listen for these keys right now.
    // Either find a way to reigister listing for these keys in a smarter way or throw away. See _onKey in base constructor.
    /* -------- First‑character type‑ahead -------- */
    _typeahead(char, startIdx, options, list) {
        char = char.toLowerCase();
        const len = options.length;
        for (let i = 1; i <= len; i++) {
            const opt = options[(startIdx + i) % len];
            if ((opt.textContent || '').trim().toLowerCase().startsWith(char)) {
                this._move(opt, list, options);
                break;
            }
        }
    }
}

/* --------------------------------------------------
 *  RadiogroupPlugin – Roving‑Tabindex & Selection.
 *  Handles widgets using [role="radiogroup"] and input[type="radio"] children.
 *  Supports only single‑‑select.
 * -------------------------------------------------- */
AK.RadioGroupPlugin = class RadiogroupPlugin extends AK.AccessKitPluginBase {
    init(container = document) {
        const groups = Array.from(container.querySelectorAll('[role="radiogroup"]'));
        groups.forEach(group => this._initRadioGroup(group));
    }

    _initRadioGroup(group) {
        // Initialise roving tabindex
        const options = this.applyRoving(group, AK._activeRadioSelector);
        this._setCache(group, options);

        // Make radiogroup focusable itself (fallback if options are removed dynamically)
        if (!group.hasAttribute('tabindex')) {
            group.tabIndex = -1;
        }
    }

    _options(group) {
        return this._getCache(group, () => group.querySelectorAll(AK._activeRadioSelector));
    }

    handleKey(e) {
        const opt = e.target;
        const group = opt.closest('[role="radiogroup"]');
        if (!group)
            return false;

        const options = this._options(group);
        if (!options.length)
            return false;

        const orientation = group.getAttribute('aria-orientation') ?? 'vertical';

        return this.handleRovingKeys(e, options, {
            orientation,
            activateFn: (el, _idx, _opts) => el.click(),
            extraKeysFn: (ev, idx, opts) => {
                // INFO: This prevents default browser navigation
                // which is possible for radiogroups via ↑ & ← for backward and ↓ & → for forward navigation with no regard of orientation
                const navKeys = this._getNavKeys(orientation);
                if (!navKeys.includes(ev.key)) {
                    return true;
                }
                return false;
            }
        });
    }

    /* -------- Move roving focus -------- */
    _move(opt, group, options) {
        super._move(opt, null, options);

        // TODO: Evaluate if this is needed 
        // In radiogroups, moving also selects (if manualselect is not true)
        if (group && group.length && group.dataset.manualselect !== 'true') {
            opt.click();
        }
    }
}

/**
* Combobox‑specific plugin that delegates option navigation to AK.ListboxPlugin.  
* Handles only:
*   – opening / closing the popup listbox
*   – synchronising the selected <option> value back to the trigger
*   – closing on ENTER / ESC / TAB / ALT+↑ and on pointer clicks
*
*  Mark‑up (single‑select):
*     <input type="text" role="combobox" aria-controls="city-list" aria-expanded="false" aria-autocomplete="list" />
*     <ul id="city-list" role="listbox" hidden>
*        <li role="option">Berlin</li>
*        …
*     </ul>
*/
AK.ComboboxPlugin = class ComboboxPlugin extends AK.AccessKitExpandablePluginBase {
    init(container = document) {
        container.querySelectorAll('[role="combobox"]').forEach(cb => this._initCombobox(cb));
    }

    _initCombobox(cb) {
        const listId = cb.getAttribute('aria-controls');
        const list = listId && document.getElementById(listId);
        if (!list || list.getAttribute('role') !== 'listbox') return;

        /* --- Pointer interactions --------------------------------------- */
        this.on(list, 'click', (e) => {
            const opt = e.target.closest('[role="option"]');
            if (!opt || !list.contains(opt)) return;
            this._commitSelection(opt, cb);
        });

        const outsideClick = (e) => {
            if (!cb.contains(e.target) && cb.getAttribute('aria-expanded') === 'true' && !list.contains(e.target)) {
                e.preventDefault();
                this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
            }
        };

        // Listen to self & execute default click behavior.
        this.on(cb, 'expand.ak', (e) => {
            e.stopPropagation();
            const open = cb.getAttribute('aria-expanded') === 'true';

            if (!open) {
                cb.click();
                document.addEventListener('mousedown', outsideClick, true);
            }
        });

        this.on(cb, 'collapse.ak', (e) => {
            document.removeEventListener('mousedown', outsideClick, true);
        });

        /* --- Keydown inside listbox (only CLOSE / COMMIT keys) ---------- */
        this.on(list, 'keydown', (e) => {
            switch (e.key) {
                case AK.KEY.ESC:
                case AK.KEY.TAB:
                case AK.KEY.UP && e.altKey:
                    if (e.key !== AK.KEY.TAB) e.preventDefault();
                    this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
                    break;
                case AK.KEY.ENTER:
                    const sel = list.querySelector('[role="option"][aria-selected="true"]') || e.target.closest('[role="option"]');
                    if (sel) this._commitSelection(sel, cb);
                    break;
            }
        });

        /* --- Sync trigger value when ListboxPlugin selects -------------- */
        this.on(list, 'select.listbox.ak', e => {
            const { opt } = e.detail || {};
            if (opt) this._syncToTrigger(cb, opt);
        });
    }

    handleKey(e) {
        const el = e.target;
        if (el.getAttribute('role') === 'combobox') {
            return this._comboKey(e, el);
        }
        return false;
    }

    _comboKey(e, cb) {
        const list = document.getElementById(cb.getAttribute('aria-controls'));
        if (!list) return false;

        const firstOpt = this._firstOption(list);
        const lastOpt = this._lastOption(list);
        const isOpen = cb.getAttribute('aria-expanded') === 'true';

        switch (e.key) {
            case AK.KEY.DOWN:
                this.toggleExpanded(cb, true, { focusTarget: firstOpt });
                return true;
            case AK.KEY.UP:
                this.toggleExpanded(cb, true, { focusTarget: lastOpt });
                return true;
            case AK.KEY.ENTER:
            case AK.KEY.SPACE:
                this.toggleExpanded(cb, !isOpen, { focusTarget: isOpen ? 'trigger' : firstOpt });
                return true;
            case AK.KEY.ESC:
            case AK.KEY.TAB:
                if (isOpen) {
                    this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
                }
                return e.key !== AK.KEY.TAB; // Prevent default on ESC, allow Tab
        }
        return false;
    }

    /* --------------------------- Selection helpers ---------------------- */
    _commitSelection(opt, cb) {
        const list = opt.closest('[role="listbox"]');

        if (list && list.getAttribute('data-ak-multiselect') == "true") {
            // Multi‑select → commit immediately (incl. close)
            this._syncToTrigger(cb, opt);
            this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
        }
    }

    // TODO: (wcag) (mh) Research this.
    _syncToTrigger(cb, opt) {
        cb.value = (opt.textContent || '').trim();
        cb.setAttribute('aria-activedescendant', opt.id || (opt.id = `ak-opt-${crypto.randomUUID()}`));
    }

    _firstOption(list) { return list.querySelector(AK._activeOptionSelector); }
    _lastOption(list) { const opts = list.querySelectorAll(AK._activeOptionSelector); return opts[opts.length - 1] || null; }
};

/**
* Disclosure/Accordion keyboard handler
*
* ▸ Stand‑alone pattern:
*    <button aria-expanded="false" aria-controls="panel">…</button>
*    <div id="panel" hidden>…</div>
*
* TODO: (wcag) (mh) Doc for data-collapse-siblings
* ▸ Accordion pattern (container gets data-ak-accordion):
*    <div data-ak-accordion>
*       <button aria-expanded="false" aria-controls="p1">…</button>
*       <div id="p1" hidden>…</div>
*       … (n×) …
*    </div>
*
* Keyboard‑Support
*   ↑ / ↓ / ← / →    Roving focus within accordion (orientation aware)
*   HOME / END       Jump first / last header in accordion
*   ENTER / SPACE    Toggle current disclosure / accordion item
*   ESC              Collapse current item (accordion only)
*/
AK.DisclosurePlugin = class DisclosurePlugin extends AK.AccessKitExpandablePluginBase {
    init(container = document) {
        /* --- Accordions -------------------------------- */
        container.querySelectorAll('[data-ak-accordion]').forEach(acc => {
            // TODO: (wcag) (mh) DRY > See registration selector below...
            const triggers = this.applyRoving(acc, '[aria-controls][aria-expanded]:not([role="combobox"])');
            this._setCache(acc, triggers);

            triggers.forEach(trig => {
                // Pointer interaction mirrors keyboard behaviour
                this.on(trig, 'click', e => {
                    this.toggleExpanded(e.currentTarget, null, {
                        collapseSiblings: acc.getAttribute('data-collapse-siblings') ?? false,
                        focusTarget: 'trigger'
                    });
                });
            });
        });

        /* --- Stand‑alone disclosures ------------------ */
        container.querySelectorAll('[aria-controls][aria-expanded]:not([data-ak-accordion] [aria-expanded])')
            .forEach(trig => {
                this.on(trig, 'click', e => {
                    this.toggleExpanded(e.currentTarget, null, { focusTarget: 'trigger' });
                });
            });
    }

    handleKey(e) {
        const trigger = e.target;
        const panelId = trigger.getAttribute("aria-controls");
        const panel = (panelId && document.getElementById(panelId)) || trigger.closest('[aria-hidden=false]');

        // ESC within an open panel 
        if (e.key === AK.KEY.ESC) {
            if (panel) {
                const opener = document.querySelector(`[aria-expanded="true"][aria-controls="${panel.id}"]`);
                if (opener) {
                    this.toggleExpanded(opener, false, { focusTarget: 'trigger' });
                    return true;                 
                }
            }
        }

        if (!trigger || !trigger.hasAttribute('aria-expanded'))
            return false;

        const accordion = trigger.closest('[data-ak-accordion]');

        // If we are in accordion mode apply roving tab index.
        if (accordion) {
            const triggers = this._getCache(accordion, () =>
                this.applyRoving(accordion, '[aria-controls][aria-expanded]'));

            const orientation = accordion.getAttribute('aria-orientation') ?? 'vertical';
            const collapseSiblings = accordion.getAttribute('data-collapse-siblings') ?? false; 

            return this.handleRovingKeys(e, triggers, {
                orientation,
                activateFn: (el) =>
                    this.toggleExpanded(el, true, { collapseSiblings: collapseSiblings, focusTarget: 'first' }),
                extraKeysFn: (ev) => {
                    if (ev.key === AK.KEY.ESC) {
                        // ESC collapses current panel
                        this.toggleExpanded(trigger, false, { collapseSiblings: collapseSiblings, focusTarget: 'trigger' });
                        return true;
                    }
                    return false;
                },
            });
        }

        // Stand‑alone disclosure (no accordion)
        if (e.key === AK.KEY.ENTER || e.key === AK.KEY.SPACE) {
            this.toggleExpanded(trigger, true, { focusTarget: 'first' });

            // Handle leaving via TAB to the next element outside the panel.
            if (panel) {
                // Close on TAB to the outside of the panel.
                const focusHandler = e => {
                    const el = e.target;
                    if (!panel.contains(el) && el !== trigger && trigger.hasAttribute("ak-close-on-leave")) {
                        this.toggleExpanded(trigger, false);
                        document.removeEventListener('focusin', focusHandler, true);
                    }
                };
                document.addEventListener('focusin', focusHandler, true);
            }

            return true;
        }

        return false;
    }

    _move(trigger, accordion = null, triggers = null) {
        if (!trigger) return;
        accordion = accordion || trigger.closest('[data-ak-accordion]');
        triggers = triggers || (accordion ? this._getCache(accordion) : [trigger]);
        super._move(trigger, null, triggers);
    }
};

(function () {
    const matchRole = (el, parentRole, role = null) => {
        if (role && el.getAttribute('role') !== role) {
            return null;
        }
        return el.closest(`[role="${parentRole}"]`);
    }

    AccessKit.register({
        ctor: AK.MenuPlugin,
        match: (el) => {
            let root = el.closest('[role="menubar"], [role="menu"]');
            if (!root && document.getElementById(el.getAttribute('aria-controls'))) {
                root = el.closest('[role="menuitem"][aria-controls]');
            }

            return root;
        }
    });

    AccessKit.register({
        ctor: AK.ComboboxPlugin,
        match: (el) => el.closest('[role="combobox"]')
    });

    AccessKit.register({
        ctor: AK.DisclosurePlugin,
        // TODO: (wcag) (mh) This can't be correct.
        // We claim any element that has aria-expanded + aria-controls
        match: (el) => el.closest('[aria-controls][aria-expanded]:not([role="combobox"])')
    });

    AccessKit.register({
        ctor: AK.ListboxPlugin,
        match: (el) => matchRole(el, 'listbox', 'option')
    });

    AccessKit.register({
        ctor: AK.RadioGroupPlugin,
        match: (el) => el.matches('input[type="radio"]') && el.closest('[role="radiogroup"]')
    });

    AccessKit.register({
        ctor: AK.TreePlugin,
        match: (el) => matchRole(el, 'tree', 'treeitem')
    });

    AccessKit.register({
        ctor: AK.TablistPlugin,
        match: (el) => matchRole(el, 'tablist', 'tab')
    });

    // Boot
    document.addEventListener('DOMContentLoaded', () => {
        window.AccessKitInstance = new AccessKit(window.AccessKitConfig || {});
    });
})();