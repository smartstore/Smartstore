/**
 * accessibility.generic.js (AccessKit core + MenuPlugin)
 *
 * WCAG‑2.2 keyboard framework – **browser‑only build**
 * (Nur „window“ als globales Objekt, keine AMD/CommonJS‑Wrapper mehr.)
 */
(function (window) {
    'use strict';

    /* --------------------------------------------------
     *  AccessKit core – plugin host + key dispatcher
     * -------------------------------------------------- */
    class AccessKit {
        constructor(opts = {}) {
            this.opts = opts;
            this.rtl = opts.rtl ?? (document.documentElement.dir === 'rtl');

            /* instantiate plugins */
            this._plugins = AccessKit._registry.map(Plugin => new Plugin(this));

            /* one keydown listener – capture phase */
            window.addEventListener('keydown', this._dispatchKey.bind(this), true);

            // TODO: (wcag) (mh) Is this really needed? Lets wait if we can use it anywhere before we remove it.
            /* announce init */
            document.dispatchEvent(new CustomEvent('ak:init', {
                bubbles: true,
                detail: { instance: this }
            }));
        }

        _dispatchKey(e) {
            for (const plugin of this._plugins) {
                if (typeof plugin.handleKey === 'function' && plugin.handleKey(e) === true) {
                    if (e.key !== 'Tab') e.preventDefault(); // keep natural Tab
                    e.stopPropagation();
                    return;
                }
            }
        }

        static register(Plugin) {
            AccessKit._registry.push(Plugin);
        }
    }
    AccessKit._registry = [];

    /* --------------------------------------------------
     *  Shared helpers
     * -------------------------------------------------- */
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

    // Gets directional keys based on menubar or menu  aria-orientation attribute & rtl
    function getNavKeys(orientation, rtl = false) {
        if (orientation === 'horizontal') {
            return rtl ? [KEY.RIGHT, KEY.LEFT] : [KEY.LEFT, KEY.RIGHT];
        }
        // vertical
        return [KEY.UP, KEY.DOWN];
    }

    const nextIdx = (cur, delta, len) => (cur + delta + len) % len;

    const setActive = (items, idx) => {
        items.forEach(el => el.tabIndex = -1);
        const el = items[idx];
        if (el) {
            el.tabIndex = 0;
            el.focus();
            document.dispatchEvent(new CustomEvent('ak:menu:focus', {
                bubbles: true,
                detail: { item: el }
            }));
        }
    };

    /* --------------------------------------------------
     *  MenuPlugin – Roving‑Tabindex & Sub‑menu handling
     *  Handles all items of role="menubar" based on subitems role="menu" & role="menuitem".
     * -------------------------------------------------- */
    class MenuPlugin {
        constructor(ak) {
            this.ak = ak;
            this.menubars = Array.from(document.querySelectorAll('[role="menubar"]'));
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
            return [...container.querySelectorAll('[role="menuitem"]')]
                .filter(mi => mi.closest('[role="menubar"],[role="menu"]') === container);
        }

        /* entry point for dispatcher */
        handleKey(e) {
            const el = e.target;

            if (!el || el.getAttribute('role') !== 'menuitem') return false;

            const menubar = el.closest('[role="menubar"]');
            if (menubar) return this._menubarKey(e, menubar);

            const submenu = el.closest('[role="menu"]');
            if (submenu) return this._submenuKey(e, submenu);

            return false;
        }

        /* top‑level */
        _menubarKey(e, menubar) {
            const items = this._items(menubar);
            const idx = items.indexOf(e.target);
            if (idx === -1) return false;

            //const dirNext = this.ak.rtl ? -1 : +1;
            //const dirPrev = this.ak.rtl ? +1 : -1;

            const orientation = menubar.getAttribute('aria-orientation') ?? 'horizontal';
            const [KEY_PREV, KEY_NEXT] = getNavKeys(orientation, this.ak.rtl);

            switch (e.key) {
                //case KEY.RIGHT: setActive(items, nextIdx(idx, dirNext, items.length)); return true;
                //case KEY.LEFT: setActive(items, nextIdx(idx, dirPrev, items.length)); return true;
                case KEY_NEXT: setActive(items, nextIdx(idx, +1, items.length)); return true;
                case KEY_PREV: setActive(items, nextIdx(idx, -1, items.length)); return true;
                case KEY.HOME: setActive(items, 0); return true;
                case KEY.END: setActive(items, items.length - 1); return true;
                case KEY.DOWN:
                case KEY.SPACE:
                case KEY.ENTER:
                    if (e.target.getAttribute('aria-haspopup') === 'menu') {
                        this._open(e.target); return true;
                    }
                    return false;
                case KEY.ESC: MenuPlugin.closeAll(); return true;
                case KEY.TAB: setActive(items, 0); return true;
            }
            return false;
        }

        /* submenu */
        _submenuKey(e, submenu) {
            const trigger = document.querySelector(`[aria-controls="${submenu.id}"]`);
            const items = this._items(submenu);
            const idx = items.indexOf(e.target);
            if (idx === -1) return false;

            //const dirOpen = this.ak.rtl ? KEY.LEFT : KEY.RIGHT;
            //const dirClose = this.ak.rtl ? KEY.RIGHT : KEY.LEFT;

            const orientation = submenu.getAttribute('aria-orientation') ?? 'vertical';
            const [KEY_PREV, KEY_NEXT] = getNavKeys(orientation, this.ak.rtl);
            const dirOpen = orientation === 'vertical' ? (this.ak.rtl ? KEY.LEFT : KEY.RIGHT) : KEY.DOWN;
            const dirClose = orientation === 'vertical' ? (this.ak.rtl ? KEY.RIGHT : KEY.LEFT) : KEY.UP;

            switch (e.key) {
                //case KEY.DOWN: setActive(items, nextIdx(idx, +1, items.length)); return true;
                //case KEY.UP: setActive(items, nextIdx(idx, -1, items.length)); return true;
                case KEY_NEXT: setActive(items, nextIdx(idx, +1, items.length)); return true;
                case KEY_PREV: setActive(items, nextIdx(idx, -1, items.length)); return true;
                case KEY.HOME: setActive(items, 0); return true;
                case KEY.END: setActive(items, items.length - 1); return true;
                case dirOpen:
                    if (e.target.getAttribute('aria-haspopup') === 'menu') { this._open(e.target); return true; }
                    return false;
                case dirClose: 
                case KEY.ESC: this._close(trigger, submenu); trigger?.focus(); return true;
                case KEY.TAB: MenuPlugin.closeAll(); return true;
            }
            return false;
        }

        _open(trigger) {
            const menu = document.getElementById(trigger.getAttribute('aria-controls'));    
            if (!menu) return;
            trigger.setAttribute('aria-expanded', 'true');
            trigger.dispatchEvent(new CustomEvent('ak:menu:open', { bubbles: true, detail: { trigger, menu } }));
            const items = this._items(menu);
            items.forEach((el, i) => el.tabIndex = i === 0 ? 0 : -1);
            items[0]?.focus();
        }

        _close(trigger, menu) {
            if (!menu || !menu.classList.contains('show')) return;
            trigger?.setAttribute('aria-expanded', 'false');
            trigger.dispatchEvent(new CustomEvent('ak:menu:close', { bubbles: true, detail: { trigger, menu } }));
        }

        static closeAll() {
            document.querySelectorAll('[role="menu"].show').forEach(menu => {
                const trigger = document.querySelector(`[aria-controls="${menu.id}"][aria-expanded="true"]`);
                trigger?.setAttribute('aria-expanded', 'false');
                trigger.dispatchEvent(new CustomEvent('ak:menu:close', { bubbles: true, detail: { trigger, menu } }));
            });
        }
    }

    /* Register plugins */
    AccessKit.register(MenuPlugin);
    // TODO: more plugins 

    /* Boot */
    const start = () => new AccessKit(window.AccessKitConfig || {});
    document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', start) : start();

    /* expose */
    window.AccessKit = AccessKit;
})(window);
