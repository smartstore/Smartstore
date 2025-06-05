/**
 * WCAG‑2.2 keyboard framework
 * TODO: (wcag) (mh) Docs
 * TODO: (wcag) (mh) Apply an element's active class if applicable, e.g. in dropdowns. Don't programmatically focus in this case.
 */
(function (window) {
    'use strict';

    const KEY = {
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

    /* --------------------------------------------------
     *  AccessKit core – plugin host + key dispatcher
     * -------------------------------------------------- */
    class AccessKit {
        constructor(options = {}) {
            this.options = options;
            this.rtl = options.rtl ?? (document.documentElement.dir === 'rtl');

            /* instantiate plugins */
            this._plugins = AccessKit._registry.map(p => new p(this));

            /* one keydown/keyup listener for relevant elements – capture phase */
            const handleKey = e => {
                // Only execute further code if relevant.
                if (!e.target?.matches('a,[role],[tabindex]'))
                    return;

                this._dispatchKey(e);
            };

            window.addEventListener('keydown', handleKey, true);
            window.addEventListener('keyup', handleKey, true);

            this._initOffCanvasTrap();
        }

        _dispatchKey(e) {
            const hook = e.type === 'keydown' ? 'handleKey' : 'handleKeyUp';

            for (const plugin of this._plugins) {
                // Check whether plugin implements the handler method & if it returns true (handled).
                if (_.isFunction(plugin[hook]) && plugin[hook](e)) {

                    // Preserve natural Tab behaviour, but prevent default for all other keys.
                    if (e.type === 'keydown' && e.key !== KEY.TAB) {
                        e.preventDefault();
                    }

                    e.stopPropagation();
                    return;
                }
            }
        }

        static register(plugin) {
            AccessKit._registry.push(plugin);
        }

        _initOffCanvasTrap() {
            // TODO: (wcag) (mh) Implement focus trapping.
            // TODO: (wcag) (mh) We need a generic focusHandler that also can handle modal dialogs, popovers etc.

            // INFO: Jquery must be used here, because original event is namespaced & triggered via Jquery.
            $(document).on('shown.sm.offcanvas', function (e) {
                const offcanvas = $(e.target);
                offcanvas.attr("aria-hidden", false);

                // Get opener and set attribute aria-expanded.
                const opener = $(`[aria-controls="${offcanvas.attr('id')}"]`);
                opener.attr("aria-expanded", true);

                // TODO: (wcag) (mh) Find a better way to get the first activatable element in an offcanvas
                // Select first clickable element.
                //const firstActivatableElem = offcanvas.find('[data-toggle="tab"]').first();

                // INFO: Terrible selector, but ensures to find first link in offcanvas facette filter
                const firstActivatableElem = offcanvas.find('a[href]').first();
                firstActivatableElem.trigger("focus");

                // TODO: (wcag) (mh) Reinit AccessToolkit
            });

            $(document).on('hidden.sm.offcanvas', function (e) {
                const offcanvas = $(e.target);
                offcanvas.attr("aria-hidden", true);

                // Get opener and set attribute aria-expanded.
                const opener = $(`[aria-controls="${offcanvas.attr('id')}"]`);
                opener.attr("aria-expanded", false);
                opener.trigger("focus");
            });
        }
    }

    AccessKit._registry = [];

    /* --------------------------------------------------
     *  Shared helpers
     * -------------------------------------------------- */

    // TODO: (wcag) (mh) Move it to the plugin it belongs to or use it elsewhere.
    const setActive = (items, idx) => {
        items.forEach(el => el.tabIndex = -1);
        const el = items[idx];
        if (el) {
            // TODO: (wcag) (mh) Move this part to the base class, method setFocus(el). A derivative plugin could overwrite this, e.g. $(el).addClass('active'). Also need removeFocus(el) without impl in base.
            el.tabIndex = 0;
            el.focus();
        }
    };

    // Plugin base class for AccessKit plugins.
    // TODO: (wcag) (mh) Shift to own file.
    // TODO: (wcag) (mh) The goal must be to give this base class more power/implementation, so that plugins can be more generic and less specific.
    class AccessKitPluginBase {
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
            const items = [...root.querySelectorAll(selector)]
                .filter(i => i.closest(root.getAttribute('role') ? `[role="${root.getAttribute('role')}"]` : root) === root);
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

        // Add event listener to element and store the handle for later removal.
        on(el, evt, fn, opts) {
            el.addEventListener(evt, fn, opts);
            this._handles.push(() => el.removeEventListener(evt, fn, opts));
        }

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

        _dispatchEvent(name, el, detail = {}) {
            const event = new CustomEvent(name, {
                bubbles: true,
                detail: detail
            });
            el.dispatchEvent(event);
        }

        // TODO: (wcag) (mh) Use everywhere.
        // TODO: (wcag) (mh) Rtl should not be a param. We can obtain it directly in this function.
        /*
         * Key handler for plugins using a roving tabindex list 
         * @param {KeyboardEvent}  e        – Original event
         * @param {HTMLElement[]}  items    – Currently visible/focusable list elements
         * @param {Object} [cfg]            – Optional configuration
         *        orientation   'vertical' | 'horizontal'   (Default: 'vertical')
         *        rtl           boolean                     (Default: false)
         *        activateFn    function(el, idx, items)    Callback for ENTER/SPACE
         *        extraKeysFn   function(e, idx, items)     Special keys for plugins
         *
         * @returns {boolean}   true → Event processed, false → delegate to browser
         */
        handleRovingKeys(e, items, {orientation = 'vertical', rtl = false, activateFn = null, extraKeysFn = null} = {}) {
            const idx = items.indexOf(e.target);
            if (idx === -1) return false;

            // Determine navigation keys depending on orientation & rtl
            const PREV_KEY = orientation === 'vertical' ? KEY.UP : rtl ? KEY.RIGHT : KEY.LEFT;
            const NEXT_KEY = orientation === 'vertical' ? KEY.DOWN : rtl ? KEY.LEFT : KEY.RIGHT;

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

    // Base plugin for accessible expandable elements (tree, menubar, combobox, disclosure, accordion).
    class AccessKitExpandablePluginBase extends AccessKitPluginBase {
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
            let target = null;
            if (trigger.hasAttribute('aria-controls')) {
                target = document.getElementById(trigger.getAttribute('aria-controls'));
            } else if (trigger.nextElementSibling) {
                // Fallback: <button> … <div class="panel">
                target = trigger.nextElementSibling;
            }

            const isOpen = trigger.getAttribute('aria-expanded') === 'true' || trigger.open === true;
            const shouldOpen = expand === null ? !isOpen : Boolean(expand);

            // Set attributes & visibilty
            if (trigger.hasAttribute('aria-expanded')) {
                trigger.setAttribute('aria-expanded', shouldOpen ? 'true' : 'false');
            } else if ('open' in trigger) {
                trigger.open = shouldOpen;
            }

            if (target) target.hidden = !shouldOpen;

            // Accordeon mode > toggle siblings
            if (shouldOpen && opt.collapseSiblings && trigger.parentElement) {
                const peers = trigger.parentElement.querySelectorAll('[aria-expanded="true"],[open]');
                peers.forEach((p) => {
                    if (p !== trigger) this.toggleExpanded(p, false);
                });
            }

            // Focus
            if (shouldOpen && target) {
                let focusEl = null;
                if (opt.focusTarget === 'first') {
                    focusEl = target.querySelector('[tabindex="0"],[role],button,a,input,select,textarea');
                } else if (opt.focusTarget instanceof HTMLElement) {
                    focusEl = opt.focusTarget;
                } else if (opt.focusTarget === 'trigger') {
                    focusEl = trigger;
                }
                focusEl?.focus();
            }

            this._dispatchEvent(shouldOpen ? 'ak-toggle-open' : 'ak-toggle-close', trigger, { trigger, target });
        }
    }

    // TODO: (wcag) (mh) Is this necessary?
    // Export, so that specific plugins can use `extends AccessKitPlugin`.
    window.AccessKitPluginBase = AccessKitPluginBase;

    /* --------------------------------------------------
     *  MenuPlugin – Roving‑Tabindex & Sub‑menu handling
     *  Handles all items of role="menubar" based on subitems role="menu" & role="menuitem".
     * -------------------------------------------------- */
    class MenuPlugin extends AccessKitExpandablePluginBase {
        // TODO: (wcag) (mh) Very obfuscated code. Bad naming conventions, no comments. Don't trust ChatGPT unmoderated! TBD with MC.
        // TODO: (wcag) (mh) A special "key handler plugin" belongs to the plugin file if it excsts. In this case: smartstore.megamenu.js. But not if it is generic enough to handle more than one widget type.

        //init(container = document) {
        //    // TODO: (wcag) (mh) Slow!
        //    this.menubars = Array.from(container.querySelectorAll('[role="menubar"]'));
        //    this._initRovingTabindex();

        //    // TODO: (wcag) (mh) Evaluate with ChatGPT which is better to use.
        //    // 1. The current implmentation where we use the keydown event on the window object
        //    // 2. The implementation where we use the keydown event on the menuitem elements or other interceptable roles.
        //    //$(window).on('keydown', '[role=menuitem]', (e) => {
        //    //    if (isFirstOfMAinMenu) {
        //    //        // Init
        //    //    }
        //    //});
        //}

        init(container = document) {
            container.querySelectorAll('[role="menubar"]').forEach(menubar => {
                const menuitem = this.applyRoving(menubar, '[role="menuitem"]');   
                this._setCache(menubar, menuitem);
            });
        }

        //_initRovingTabindex() {
        //    this.menubars.forEach(bar => {
        //        this._items(bar).forEach((el, i) => el.tabIndex = i === 0 ? 0 : -1);
        //    });
        //}

        _items(container) {
            // TODO: (wcag) (mh) Slow!
            return this._getCache(container, () =>
                [...container.querySelectorAll('[role="menuitem"]')]
                    .filter(mi => mi.closest('[role="menubar"], [role="menu"]') === container)
                );
        }

        /* Entry point for dispatcher */
        handleKey(e) {
            const el = e.target;

            if (!el || el.getAttribute('role') !== 'menuitem') return false;

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
            const rtl = this.ak?.rtl === true;

            return this.handleRovingKeys(e, items, {
                orientation,
                rtl,

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
                    const dirOpen = isMenubar ? null : isVertical ? rtl ? KEY.LEFT : KEY.RIGHT : KEY.DOWN;
                    const dirClose = isMenubar ? null : isVertical ? rtl ? KEY.RIGHT : KEY.LEFT : KEY.UP;

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

        // TODO: (wcag) (mh): Remove when the new function is working as expected.
        _menuKey_OLD(e, menu, isMenubar = false) {
            const items = this._items(menu);
            const idx = items.indexOf(e.target);
            if (idx === -1) return false;

            const orientation = menu.getAttribute('aria-orientation') ?? (isMenubar ? 'horizontal' : 'vertical');
            const [KEY_PREV, KEY_NEXT] = this._getNavKeys(orientation, this.ak.rtl);
            const isVertical = orientation === 'vertical';
            const dirOpen = isMenubar ? null : (isVertical ? (this.ak.rtl ? KEY.LEFT : KEY.RIGHT) : KEY.DOWN);
            const dirClose = isMenubar ? null : (isVertical ? (this.ak.rtl ? KEY.RIGHT : KEY.LEFT) : KEY.UP);
            const openKeysRoot = [KEY.DOWN, KEY.SPACE, KEY.ENTER];

            switch (e.key) {
                /* Navigate   ------------------------------------------------------ */
                case KEY_NEXT: setActive(items, this._nextIdx(idx, +1, items.length)); return true;
                case KEY_PREV: setActive(items, this._nextIdx(idx, -1, items.length)); return true;
                case KEY.HOME: setActive(items, 0); return true;
                case KEY.END: setActive(items, items.length - 1); return true;

                /* Open   ---------------------------------------------------------- */
                case (isMenubar ? openKeysRoot.find(k => k === e.key) : dirOpen):
                    if (e.target.getAttribute('aria-haspopup') === 'menu') {
                        this._open(e.target);
                        return true;
                    }
                    return false;

                /* Close     ------------------------------------------------------- */
                case KEY.ESC:
                case dirClose:
                    if (isMenubar) {
                        this._closeAll(); 
                    } else {
                        const trigger = document.querySelector(`[aria-controls="${menu.id}"]`);
                        this._close(trigger, menu);
                        trigger?.focus();
                    }
                    return true;

                /* Tab = Leave component ------------------------------------------- */
                case KEY.TAB:
                    if (isMenubar) {
                        setActive(items, 0);     // Reset focus & tabIndex on menubar items.
                    } else {
                        this._closeAll();   // Close all submenus.
                    }
                    return true;
            }
            return false;
        }

        _open(trigger) {
            const menu = document.getElementById(trigger.getAttribute('aria-controls'));
            if (!menu)
                return;

            //trigger.setAttribute('aria-expanded', 'true');
            //this._dispatchEvent('ak-toggle-open', trigger, { trigger, target: menu });

            //// Apply roving‑tabindex to menu items
            //const items = this.applyRoving(menu, '[role="menuitem"]');
            //this._setCache(menu, items);
            //items[0]?.focus();

            let items = this._getCache(menu);
            if (!items) {
                items = this.applyRoving(menu, '[role="menuitem"]');
                this._setCache(menu, items);
            }

            this.toggleExpanded(trigger, true, { focusTarget: 'trigger', collapseSiblings: true });

            items[0]?.focus();
        }

        _close(trigger, menu) {
            if (!menu || !menu.classList.contains('show'))
                return;

            trigger?.setAttribute('aria-expanded', 'false');
            this._dispatchEvent('ak-toggle-close', trigger, { trigger, menu });
        }

        _closeAll() {
            document.querySelectorAll('[role="menu"].show').forEach(menu => {
                const trigger = document.querySelector(`[aria-controls="${menu.id}"][aria-expanded="true"]`);
                trigger?.setAttribute('aria-expanded', 'false');
                this._dispatchEvent('ak-toggle-close', trigger, { trigger, menu });
            });
        }
    }

    /* --------------------------------------------------
     *  TablistPlugin – Roving‑Tabindex & Panel Switching
     *  Handles all items of [role="tablist"] + [role="tab"] + [role="tabpanel"]
     * -------------------------------------------------- */
    class TablistPlugin extends AccessKitPluginBase {
        //init(container = document) {
        //    const lists = Array.from(container.querySelectorAll('[role="tablist"]'));
        //    lists.forEach((list) => this._initRovingTabindex(list));
        //};

        //_initRovingTabindex(list) {
        //    const tabs = this._tabs(list);
        //    // Find preselected tab, otherwise first - always only ONE “aria-selected=true”!
        //    let selectedIdx = tabs.findIndex(t => t.getAttribute('aria-selected') === 'true');
        //    if (selectedIdx === -1) {
        //        selectedIdx = 0;
        //    }

        //    tabs.forEach((tab, i) => {
        //        tab.tabIndex = i === selectedIdx ? 0 : -1;
        //    });

        //    this._select(tabs[selectedIdx], false);
        //}

        //_tabs(list) {
        //    return [...list.querySelectorAll('[role="tab"]')]
        //        .filter(tab => tab.closest('[role="tablist"]') === list);
        //}

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
            const rtl = this.ak?.rtl === true;

            return this.handleRovingKeys(e, tabs, {
                orientation,
                rtl,
                activateFn: (el) => this._select(el),
            });
        }

        handleKey_OLD(e) {
            const tab = e.target;
            if (!tab || tab.getAttribute('role') !== 'tab')
                return false;

            const list = tab.closest('[role="tablist"]');
            if (!list)
                return false;

            const tabs = this._getCache(list, () => this.applyRoving(list, '[role="tab"]'));
            const idx = tabs.indexOf(tab);
            if (idx === -1)
                return false;

            const orientation = list.getAttribute('aria-orientation') ?? 'horizontal';
            const [KEY_PREV, KEY_NEXT] = this._getNavKeys(orientation, this.ak.rtl);

            switch (e.key) {
                /* Navigate   ------------------------------------------------------ */
                case KEY_NEXT: {
                    const next = tabs[this._nextIdx(idx, +1, tabs.length)];
                    this._move(next);
                    return true;
                }
                case KEY_PREV: {
                    const prev = tabs[this._nextIdx(idx, -1, tabs.length)];
                    this._move(prev);
                    return true;
                }
                case KEY.HOME: {
                    this._move(tabs[0]);
                    return true;
                }
                case KEY.END: {
                    this._move(tabs[tabs.length - 1]);
                    return true;
                }
                /* Select      ------------------------------------------------------ */
                case KEY.SPACE:
                case KEY.ENTER: {
                    this._select(tab);
                    return true;
                }
            }
            return false;
        }

        // Shift roving focus to the next tab.
        _move(tab) {
            if (!tab)
                return;

            tab.focus();
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
    //class TreePlugin extends AccessKitPluginBase {
    class TreePlugin extends AccessKitExpandablePluginBase {
        //init(container = document) {
        //    const trees = Array.from(container.querySelectorAll('[role="tree"]'));
        //    trees.forEach(tree => this._initRovingTabindex(tree));
        //}

        _initRovingTabindex(tree) {
            const items = this._visibleItems(tree);
            items.forEach((item, i) => item.tabIndex = i === 0 ? 0 : -1);
        }

        //// A treeitem is included if no ancestor treeitem is set to aria-expanded=“false”. The treeitem itself may therefore be collapsed.
        //_visibleItems(tree) {
        //    return Array.from(tree.querySelectorAll('[role="treeitem"]')).filter(node => {
        //        let anc = node.parentElement?.closest('[role="treeitem"]');
        //        while (anc) {
        //            if (anc.getAttribute('aria-expanded') === 'false') return false;
        //            anc = anc.parentElement?.closest('[role="treeitem"]');
        //        }
        //        return true; // No collapsed ancestor found
        //    });
        //}

        init(container = document) {
            container.querySelectorAll('[role="tree"]').forEach(tree => {
                const items = this.applyRoving(tree, '[role="treeitem"]');
                this._setCache(tree, items);
            });
        }

        _allItems(tree) {
            return this._getCache(tree, () =>
            [...tree.querySelectorAll('[role="treeitem"]')]
                .filter(it => it.closest('[role=\"tree\"]') === tree));
        }

        _visibleItems(tree) {
            //const all = this._allItems(tree);
            //return all.filter(it => {
            //        /* Wenn das Item oder einer seiner Vorfahren per CSS/hidden ausgeblendet ist, fällt es raus. */
            //        if (it.offsetParent === null) return false;

            //        /* Ist ein Vorfahr-Treeitem collapsed (aria-expanded="false")? */
            //        const collapsedAncestor = it.closest('[role=\"treeitem\"][aria-expanded=\"false\"] [role=\"group\"]'
            //    );
            //    return !collapsedAncestor;
            //});

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
            // TODO: (wcag) (mh) Don't forget rtl and orientation.

            const item = e.target;
            if (!item || item.getAttribute('role') !== 'treeitem')
                return false;

            const tree = item.closest('[role="tree"]') || item.closest('[role="group"]');
            if (!tree)
                return false;

            const items = this._visibleItems(tree);
            const idx = items.indexOf(item);
            if (idx === -1)
                return false;

            switch (e.key) {
                /* Navigate   ------------------------------------------------------ */
                case KEY.UP: {
                    const prev = items[this._nextIdx(idx, -1, items.length)];
                    this._move(prev, tree, items);
                    return true;
                }
                case KEY.DOWN: {    
                    const next = items[this._nextIdx(idx, +1, items.length)];
                    this._move(next, tree, items);
                    return true;
                }
                case KEY.HOME: {
                    this._move(items[0], tree, items);
                    return true;
                }
                case KEY.END: {
                    this._move(items[items.length - 1], tree, items);
                    return true;
                }
                case KEY.RIGHT: {
                    //const expanded = item.getAttribute('aria-expanded') === 'true';
                    //if (!expanded) {
                    //    // Always attempt to expand, even if children not yet present (lazy loading)
                    //    this._toggle(item, true);
                    //    return true;
                    //}
                    //// After expansion, move into first child if it exists
                    //const firstChild = item.querySelector('[role="group"] [role="treeitem"]');
                    //if (firstChild) {
                    //    this._move(firstChild, tree); // siblings recalc inside _move
                    //}
                    this.toggleExpanded(item, true, { focusTarget: 'first' });
                    return true;
                }
                /* Close     ------------------------------------------------------ */
                case KEY.ESC: 
                case KEY.LEFT: {
                    //const group = item.closest('[role="group"]');
                    //if (group) {
                    //    this._toggle(item, false);
                    //    return true;
                    //}
                    //const parent = item.parentElement?.closest('[role="treeitem"]');
                    //if (parent) this._move(parent, tree, items);
                    this.toggleExpanded(item, false);
                    return true;
                }
                /* Open      ------------------------------------------------------ */
                case KEY.SPACE:
                case KEY.ENTER: {
                    item.trigger("click");
                    return false;
                }
                /* Leave component ------------------------------------------------ */
                case KEY.TAB: {
                    // INFO: Roving tab index is initialized here to ensure tabindex is set correctly in OffCanvas-AJAX scenario
                    // where we can't rely on JS initialization of the AccessKit.
                    //this._initRovingTabindex(tree);
                    this.applyRoving(tree, '[role="treeitem"]');
                    return false;
                }
            }
            return false;
        }

        /* -------- Move roving focus -------- */
        _move(item, tree = null, siblings = null) {
            if (!item)
                return;

            tree = tree || item.closest('[role="tree"]');
            siblings = siblings || this._visibleItems(tree);

            siblings.forEach((i) => (i.tabIndex = -1));
            item.tabIndex = 0;
            item.focus();
        }

        /* -------- Expand / Collapse -------- */
        //_toggle(item, expandForce = null) {
        //    const expanded = item.getAttribute('aria-expanded') === 'true';
        //    const newState = expandForce !== null ? expandForce : !expanded;
        //    item.setAttribute('aria-expanded', newState ? 'true' : 'false');

        //    // TODO: (wcag) (mh) Rename to innergroup or something like this.
        //    let group = item.querySelector('[role="group"]');
        //    if (group) {
        //        group.hidden = !newState;
        //    }

        //    const parentGroup = item.closest('[role="group"]');

        //    if (parentGroup) { 
        //        const parentId = parentGroup.getAttribute('id');
        //        if (parentId) {
        //            const parentLink = document.querySelector(`[aria-controls="${parentId}"]`);
        //            if (!newState) {
        //                parentLink.setAttribute('aria-expanded', 'false');
        //            }
        //        }
        //    }

        //    // Immer Event feuern – auch wenn group (noch) nicht vorhanden ist → Ajax-Lazy‑Load
        //    this._dispatchEvent(newState ? 'ak-tree-open' : 'ak-tree-close', item, { item, group });

        //    // Roving‑Tabindex nachträglich aktualisieren, wenn Kinder schon existieren
        //    if (group) {
        //        const tree = item.closest('[role="tree"]');
        //        //this._initRovingTabindex(tree);
        //        this.applyRoving(tree, '[role="treeitem"]');
        //    }
        //}
    }

    /* --------------------------------------------------
     *  ListboxPlugin – Roving‑Tabindex, Selection & Type‑ahead
     *  Handles widgets using [role="listbox"] with [role="option"] children
     *  Supports single‑ & multi‑select (aria-multiselectable="true")
     * -------------------------------------------------- */
    class ListboxPlugin extends AccessKitPluginBase {
        init(container = document) {
            const lists = Array.from(container.querySelectorAll('[role="listbox"]'));
            lists.forEach(list => this._initListbox(list));
        }

        _initListbox(list) {
            // Determine selection mode
            const multiselect = list.getAttribute('aria-multiselectable') === 'true';
            list.dataset.akMultiselect = multiselect;

            // Initialise roving tabindex & ensure aria-selected is set
            //const options = this._options(list);

            //options.forEach((opt, i) => {
            //    opt.tabIndex = i === 0 ? 0 : -1;
            //    if (!opt.hasAttribute('aria-selected')) {
            //        opt.setAttribute('aria-selected', 'false');
            //    }
            //});

            const options = this.applyRoving(list, '[role="option"]');
            this._setCache(list, options);
                options.forEach(opt => {
                    if (!opt.hasAttribute('aria-selected')) {
                        opt.setAttribute('aria-selected', 'false');
                    }
            });

            // Pointer interaction mirrors keyboard behaviour
            list.addEventListener('click', e => {
                const opt = e.target.closest('[role="option"]');
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
            //return [...list.querySelectorAll('[role="option"]')]
            //    .filter(opt => opt.closest('[role="listbox"]') === list);

            return this._getCache(list, () =>
                [...list.querySelectorAll('[role="option"]')]
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
            const idx = options.indexOf(opt);
            if (idx === -1)
                return false;

            // TODO: (wcag) (mh) Is a listbox always vertical or are there cases where they are horizontal?
            switch (e.key) {
                /* Navigate -------------------------------------------------- */
                case KEY.UP:
                    this._move(options[this._nextIdx(idx, -1, options.length)], list, options);
                    return true;
                case KEY.DOWN:
                    this._move(options[this._nextIdx(idx, +1, options.length)], list, options);
                    return true;
                case KEY.HOME:
                    this._move(options[0], list, options);
                    return true;
                case KEY.END:
                    this._move(options[options.length - 1], list, options);
                    return true;

                /* Select ---------------------------------------------------- */
                case KEY.SPACE:
                case KEY.ENTER:
                    this._toggleSelect(opt, list, options);
                    return true;

                /* Type‑ahead ------------------------------------------------ */
                default:
                    if (e.key.length === 1 && /\S/.test(e.key)) {
                        this._typeahead(e.key, idx, options, list);
                        return true;
                    }
            }
            return false;
        }

        /* -------- Move roving focus -------- */
        _move(opt, list, options) {
            if (!opt) return;
            options.forEach(o => o.tabIndex = -1);
            opt.tabIndex = 0;
            opt.focus();

            // In single‑select listboxes, moving also selects
            if (list.dataset.akMultiselect !== 'true') {
                this._toggleSelect(opt, list, options, /*replace*/ true);
            }
        }

        /* -------- Selection handling -------- */
        _toggleSelect(opt, list, options, replace = false) {
            const multiselect = list.dataset.akMultiselect === 'true';
            if (!multiselect || replace) {
                options.forEach(o => o.setAttribute('aria-selected', o === opt ? 'true' : 'false'));
                this._dispatchEvent('ak-listbox-select', list, { list, opt });
            } else {
                const selected = opt.getAttribute('aria-selected') === 'true';
                opt.setAttribute('aria-selected', selected ? 'false' : 'true');
                this._dispatchEvent(selected ? 'ak-listbox-deselect' : 'ak-listbox-select', list, { list, opt });
            }
        }

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

    /* Register plugins */
    AccessKit.register(MenuPlugin);
    AccessKit.register(TablistPlugin);
    AccessKit.register(TreePlugin);
    AccessKit.register(ListboxPlugin);

    /* Boot */
    const start = () => new AccessKit(window.AccessKitConfig || {});
    document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', start) : start();

    /* expose */
    window.AccessKit = AccessKit;
})(window);