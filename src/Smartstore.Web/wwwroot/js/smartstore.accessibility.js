/**
 * WCAG‑2.2 keyboard framework
 * TODO: (wcag) (mh) Docs
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

    // Gets directional keys based on menubar or menu  aria-orientation attribute & rtl
    const getNavKeys = (orientation, rtl = false) => {
        return orientation === 'horizontal'
            ? (rtl ? [KEY.RIGHT, KEY.LEFT] : [KEY.LEFT, KEY.RIGHT])
            : [KEY.UP, KEY.DOWN];
    }

    const nextIdx = (cur, delta, len) => (cur + delta + len) % len;

    const setActive = (items, idx) => {
        items.forEach(el => el.tabIndex = -1);
        const el = items[idx];
        if (el) {
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
            this.init(document);
        }

        // Will be called at initialization of the plugin and can be called after AJAX updates to re-initialize the plugin.
        // @param {Element|Document} container => context
        init(container = document) { }

        //Optional destroy method to clean up resources like events.
        destroy() { }

        // Handle 'keydown' events .
        // Must return `true` if the event has been processed (AccessKit then calls preventDefault / stopPropagation).
        // @param {KeyboardEvent} e
        // @returns {Boolean} handled?
        handleKey(e) { return false; }

        // Optional addition for 'keyup' cases (e.g. cancel gestures, space-to-click semantics). Only implement if needed.
        // @param {KeyboardEvent} e
        // @returns {Boolean} handled?
        handleKeyUp(e) { return false; }
    }

    // Export, so that specific plugins can use `extends AccessKitPlugin`.
    window.AccessKitPluginBase = AccessKitPluginBase;

    /* --------------------------------------------------
     *  MenuPlugin – Roving‑Tabindex & Sub‑menu handling
     *  Handles all items of role="menubar" based on subitems role="menu" & role="menuitem".
     * -------------------------------------------------- */
    class MenuPlugin extends AccessKitPluginBase {
        // TODO: (wcag) (mh) Very obfuscated code. Bad naming conventions, no comments. Don't trust ChatGPT unmoderated! TBD with MC.
        // TODO: (wcag) (mh) A special "key handler plugin" belongs to the plugin file if it excsts. In this case: smartstore.megamenu.js. But not if it is generic enough to handle more than one widget type.
        
        init(container = document) {
            // TODO: (wcag) (mh) Slow!
            this.menubars = Array.from(container.querySelectorAll('[role="menubar"]'));
            this._initRovingTabindex();

            // TODO: (wcag) (mh) Evaluate with ChatGPT which is better to use.
            // 1. The current implmentation where we use the keydown event on the window object
            // 2. The implementation where we use the keydown event on the menuitem elements or other interceptable roles.
            //$(window).on('keydown', '[role=menuitem]', (e) => {
            //    if (isFirstOfMAinMenu) {
            //        // Init
            //    }
            //});
        }

        _initRovingTabindex() {
            this.menubars.forEach(bar => {
                this._items(bar).forEach((el, i) => el.tabIndex = i === 0 ? 0 : -1);
            });
        }

        _items(container) {
            // TODO: (wcag) (mh) Slow!
            return [...container.querySelectorAll('[role="menuitem"]')]
                .filter(mi => mi.closest('[role="menubar"], [role="menu"]') === container);
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
            const idx = items.indexOf(e.target);
            if (idx === -1) return false;

            const orientation = menu.getAttribute('aria-orientation') ?? (isMenubar ? 'horizontal' : 'vertical');
            const [KEY_PREV, KEY_NEXT] = getNavKeys(orientation, this.ak.rtl);
            const isVertical = orientation === 'vertical';
            const dirOpen = isMenubar ? null : (isVertical ? (this.ak.rtl ? KEY.LEFT : KEY.RIGHT) : KEY.DOWN);
            const dirClose = isMenubar ? null : (isVertical ? (this.ak.rtl ? KEY.RIGHT : KEY.LEFT) : KEY.UP);
            // TODO: (wcag) (mh) These should also open submenus in simple menu. But currently they don't.
            const openKeysRoot = [KEY.DOWN, KEY.SPACE, KEY.ENTER];

            switch (e.key) {
                /* Navigate   ------------------------------------------------------ */
                case KEY_NEXT: setActive(items, nextIdx(idx, +1, items.length)); return true;
                case KEY_PREV: setActive(items, nextIdx(idx, -1, items.length)); return true;
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
                        MenuPlugin.closeAll();
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
                        MenuPlugin.closeAll();   // Close all submenus.
                    }
                    return true;
            }
            return false;
        }

        _open(trigger) {
            const menu = document.getElementById(trigger.getAttribute('aria-controls'));    
            if (!menu)
                return;

            trigger.setAttribute('aria-expanded', 'true');
            trigger.dispatchEvent(new CustomEvent('ak-menu-open', { bubbles: true, detail: { trigger, menu } }));

            // Apply roving‑tabindex to menu items
            const items = this._items(menu);
            items.forEach((el, i) => el.tabIndex = i === 0 ? 0 : -1);
            items[0]?.focus();
        }

        _close(trigger, menu) {
            if (!menu || !menu.classList.contains('show'))
                return;

            trigger?.setAttribute('aria-expanded', 'false');
            trigger.dispatchEvent(new CustomEvent('ak-menu-close', { bubbles: true, detail: { trigger, menu } }));
        }

        static closeAll() {
            document.querySelectorAll('[role="menu"].show').forEach(menu => {
                const trigger = document.querySelector(`[aria-controls="${menu.id}"][aria-expanded="true"]`);
                trigger?.setAttribute('aria-expanded', 'false');
                trigger.dispatchEvent(new CustomEvent('ak-menu-close', { bubbles: true, detail: { trigger, menu } }));
            });
        }
    }

    /* --------------------------------------------------
     *  TablistPlugin – Roving‑Tabindex & Panel Switching
     *  Handles all items of [role="tablist"] + [role="tab"] + [role="tabpanel"]
     * -------------------------------------------------- */
    class TablistPlugin extends AccessKitPluginBase {
        init(container = document) {
            const lists = Array.from(container.querySelectorAll('[role="tablist"]'));
            lists.forEach((list) => this._initRovingTabindex(list));
        };

        _initRovingTabindex(list) {
            const tabs = this._tabs(list);
            // Find preselected tab, otherwise first - always only ONE “aria-selected=true”!
            let selectedIdx = tabs.findIndex(t => t.getAttribute('aria-selected') === 'true');
            if (selectedIdx === -1) {
                selectedIdx = 0;
            }
            
            tabs.forEach((tab, i) => {
                tab.tabIndex = i === selectedIdx ? 0 : -1;
            });

            this._select(tabs[selectedIdx], false);
        }

        _tabs(list) {
            return [...list.querySelectorAll('[role="tab"]')]
                .filter(tab => tab.closest('[role="tablist"]') === list);
        }

        handleKey(e) {
            const tab = e.target;
            if (!tab || tab.getAttribute('role') !== 'tab')
                return false;

            const list = tab.closest('[role="tablist"]');
            if (!list)
                return false;

            const tabs = this._tabs(list);
            const idx = tabs.indexOf(tab);
            if (idx === -1)
                return false;

            const orientation = list.getAttribute('aria-orientation') ?? 'horizontal';
            const [KEY_PREV, KEY_NEXT] = getNavKeys(orientation, this.ak.rtl);

            switch (e.key) {
                /* Navigate   ------------------------------------------------------ */
                case KEY_NEXT: {
                    const next = tabs[nextIdx(idx, +1, tabs.length)];
                    this._move(next);
                    return true;
                }
                case KEY_PREV: {
                    const prev = tabs[nextIdx(idx, -1, tabs.length)];
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

            const tabs = this._tabs(tab.closest('[role="tablist"]'));

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
    class TreePlugin extends AccessKitPluginBase {
        init(container = document) {
            const trees = Array.from(container.querySelectorAll('[role="tree"]'));
            trees.forEach(tree => this._initRovingTabindex(tree));
        }

        _initRovingTabindex(tree) {
            const items = this._visibleItems(tree);
            items.forEach((item, i) => item.tabIndex = i === 0 ? 0 : -1);
        }

        // A treeitem is included if no ancestor treeitem is set to aria-expanded=“false”. The treeitem itself may therefore be collapsed.
        _visibleItems(tree) {
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
                    const prev = items[nextIdx(idx, -1, items.length)];
                    this._move(prev, tree, items);
                    return true;
                }
                case KEY.DOWN: {    
                    const next = items[nextIdx(idx, +1, items.length)];
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
                    const expanded = item.getAttribute('aria-expanded') === 'true';
                    if (!expanded) {
                        // Always attempt to expand, even if children not yet present (lazy loading)
                        this._toggle(item, true);
                        return true;
                    }
                    // After expansion, move into first child if it exists
                    const firstChild = item.querySelector('[role="group"] [role="treeitem"]');
                    if (firstChild) {
                        this._move(firstChild, tree); // siblings recalc inside _move
                    }
                    return true;
                }
                /* Close     ------------------------------------------------------ */
                case KEY.ESC: 
                case KEY.LEFT: {
                    const group = item.closest('[role="group"]');
                    if (group) {
                        this._toggle(item, false);
                        return true;
                    }
                    const parent = item.parentElement?.closest('[role="treeitem"]');
                    if (parent) this._move(parent, tree, items);
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
                    this._initRovingTabindex(tree);
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
        _toggle(item, expandForce = null) {
            const expanded = item.getAttribute('aria-expanded') === 'true';
            const newState = expandForce !== null ? expandForce : !expanded;
            item.setAttribute('aria-expanded', newState ? 'true' : 'false');

            // TODO: (wcag) (mh) Rename to innergroup or something like this.
            let group = item.querySelector('[role="group"]');
            if (group) {
                group.hidden = !newState;
            }

            const parentGroup = item.closest('[role="group"]');

            if (parentGroup) { 
                const parentId = parentGroup.getAttribute('id');
                if (parentId) {
                    const parentLink = document.querySelector(`[aria-controls="${parentId}"]`);
                    if (!newState) {
                        parentLink.setAttribute('aria-expanded', 'false');
                    }
                }
            }

            // Immer Event feuern – auch wenn group (noch) nicht vorhanden ist → Ajax-Lazy‑Load
            item.dispatchEvent(new CustomEvent(newState ? 'ak-tree-open' : 'ak-tree-close', {
                bubbles: true,
                detail: { item, group }
            }));

            // Roving‑Tabindex nachträglich aktualisieren, wenn Kinder schon existieren
            if (group) {
                const tree = item.closest('[role="tree"]');
                this._initRovingTabindex(tree);
            }
        }
    }

    /* Register plugins */
    AccessKit.register(MenuPlugin);
    AccessKit.register(TablistPlugin);
    AccessKit.register(TreePlugin);

    /* Boot */
    const start = () => new AccessKit(window.AccessKitConfig || {});
    document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', start) : start();

    /* expose */
    window.AccessKit = AccessKit;
})(window);
