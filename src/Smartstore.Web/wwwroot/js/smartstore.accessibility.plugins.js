/// <reference path="smartstore.accessibility.blueprint.js" />

/**
* WCAG‑2.2 keyboard navigation plugins
*/
'use strict';

/* --------------------------------------------------
 *  MenuPlugin – Roving‑Tabindex & Sub‑menu handling
 *  Handles [role="menubar"] with nested [role="menu"] and [role="menuitem"].
 * -------------------------------------------------- */
class MenuPlugin extends AccessKitExpandablePluginBase {
    getRovingItems(root) {
        // TODO: (mh) This is the anti-pattern I wanted to avoid with the refactoring! TBD with MC.
        // RE: We must do this here because of the simple menu which can contain menuitems in further dropdowns.
        // Which is not correct. A menu widget must only hold its own navigatable items.
        // Maybe I didn't understand what you did mean by anti-pattern as this is the same code as in the base plugin.
        // It only differs by checking that the menuitems are direct children of the current menu/root.
        // Hopefully you did refer to the method below, which was obsolete anyway and which I have removed now.
        // Lets talk :-)
        return Array.from(root.querySelectorAll('[role="menuitem"]'))
            .filter(mi => mi.closest('[role="menubar"],[role="menu"]') === root);
    }

    initWidgetCore(widget) {
        if (widget.root.getAttribute('role') === 'menubar') {
            widget.orientation = 'horizontal';
        }
    }

    onActivateItem(element, index, widget) {
        if (element.getAttribute('aria-haspopup') === 'menu') {
            this._open(element);
        } else {
            element.click();
        }
        return true;
    }

    onItemKeyPress(event, index, widget) {
        const k = AccessKit.KEY;
        const isMenubar = widget.root.getAttribute('role') === 'menubar';
        const isVertical = widget.orientation === 'vertical';
        const dirOpen = isMenubar ? null : (isVertical ? (widget.rtl ? k.LEFT : k.RIGHT) : k.DOWN);
        const dirClose = isMenubar ? null : (isVertical ? (widget.rtl ? k.RIGHT : k.LEFT) : k.UP);

        if (isMenubar && event.key === k.DOWN) {
            if (event.target.getAttribute('aria-haspopup') === 'menu') {
                this._open(event.target);
                return true;
            }
        }

        if (!isMenubar && event.key === dirOpen) {
            if (event.target.getAttribute('aria-haspopup') === 'menu') {
                this._open(event.target);
                return true;
            }
        }

        if (event.key === k.ESC || event.key === dirClose) {
            if (isMenubar) {
                this._closeAll();
            } else {
                const trigger = document.querySelector(`[aria-controls="${widget.root.id}"]`);
                this._close(trigger, widget.root);
                trigger?.focus();
            }
            return true;
        }

        if (event.key === k.TAB) {
            if (isMenubar) {
                this.applyRoving(widget);
            } else {
                this._closeAll();
            }
            return true;
        }

        return false;
    }

    _open(trigger) {
        const menu = document.getElementById(trigger.getAttribute('aria-controls'));
        if (!menu) return;

        this.addWidget(menu, true);
        const menuWidget = this.getWidget(menu);
        const items = menuWidget.items;
        this.toggleExpanded(trigger, true, { focusTarget: items[0] });
    }

    _close(trigger, menu) {
        if (!menu) return;
        this.toggleExpanded(trigger, false, { focusTarget: 'trigger' });
    }

    _closeAll() {
        document.querySelectorAll('[role="menu"][aria-hidden="false"]').forEach(menu => {
            const trigger = document.querySelector(`[aria-controls="${menu.id}"][aria-expanded="true"]`);
            this.toggleExpanded(trigger, false, { focusTarget: 'trigger' });
        });
    }
}


/* --------------------------------------------------
* ComboboxPlugin – Expand / Collapse & Option Commit
* Handles[role = "combobox"] that control a[role = "listbox"]
* -------------------------------------------------- */
class ComboboxPlugin extends AccessKitExpandablePluginBase {
    getRovingItems(root) {
        return [root];
    }

    initWidgetCore(widget) {
        const cb = widget.root;
        const list = document.getElementById(cb.getAttribute('aria-controls'));
        if (!list || list.getAttribute('role') !== 'listbox') return;

        // Commit selection on pointer activation
        list.addEventListener('click', e => {
            const opt = e.target.closest('[role="option"]');
            if (opt && list.contains(opt)) {
                this._commitSelection(opt, cb);
            }
        });

        // Close when clicking outside
        const outsideClick = e => {
            if (!cb.contains(e.target) && cb.getAttribute('aria-expanded') === 'true' && !list.contains(e.target)) {
                e.preventDefault();
                this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
            }
        };

        cb.addEventListener('expand.ak', e => {
            e.stopPropagation();
            if (cb.getAttribute('aria-expanded') !== 'true') {
                cb.click();
                document.addEventListener('mousedown', outsideClick, true);
            }
        });

        cb.addEventListener('collapse.ak', () => {
            document.removeEventListener('mousedown', outsideClick, true);
        });

        // Handle close/commit keys inside listbox
        list.addEventListener('keydown', e => {
            const k = AccessKit.KEY;
            if (e.key === k.ESC || e.key === k.TAB || (e.key === k.UP && e.altKey)) {
                if (e.key !== k.TAB) e.preventDefault();
                this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
                return;
            }
            if (e.key === k.ENTER) {
                const sel = list.querySelector('[role="option"][aria-selected="true"]') || e.target.closest('[role="option"]');
                if (sel) this._commitSelection(sel, cb);
            }
        });

        // Sync trigger when ListboxPlugin selects
        list.addEventListener('select.listbox.ak', e => {
            const { opt } = e.detail || {};
            if (opt) this._syncToTrigger(cb, opt);
        });
    }

    handleKeyCore(e, widget) {
        if (e.target !== widget.root) return false;

        const cb = widget.root;
        const list = document.getElementById(cb.getAttribute('aria-controls'));
        if (!list) return false;

        const k = AccessKit.KEY;
        const firstOpt = list.querySelector(AccessKit.ACTIVE_OPTION_SELECTOR);
        const opts = list.querySelectorAll(AccessKit.ACTIVE_OPTION_SELECTOR);
        const lastOpt = opts[opts.length - 1] || null;
        const isOpen = cb.getAttribute('aria-expanded') === 'true';

        switch (e.key) {
            case k.DOWN:
                this.toggleExpanded(cb, true, { focusTarget: firstOpt });
                return true;
            case k.UP:
                this.toggleExpanded(cb, true, { focusTarget: lastOpt });
                return true;
            case k.ENTER:
            case k.SPACE:
                this.toggleExpanded(cb, !isOpen, { focusTarget: isOpen ? 'trigger' : firstOpt });
                return true;
            case k.ESC:
            case k.TAB:
                if (isOpen) {
                    this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
                }
                return e.key !== k.TAB;
        }

        return false;
    }

    _commitSelection(opt, cb) {
        const list = opt.closest('[role="listbox"]');
        if (list && list.getAttribute('data-ak-multiselect') === 'true') {
            this._syncToTrigger(cb, opt);
            this.toggleExpanded(cb, false, { focusTarget: 'trigger' });
        }
    }

    _syncToTrigger(cb, opt) {
        cb.value = (opt.textContent || '').trim();
        cb.setAttribute('aria-activedescendant', opt.id || (opt.id = `ak-opt-${crypto.randomUUID()}`));
    }
}


/* --------------------------------------------------
 *  ListboxPlugin – Roving‑Tabindex, Selection & Type‑ahead
 *  Handles widgets using [role="listbox"] with [role="option"] children
 *  Supports single‑ & multi‑select (aria-multiselectable="true")
 * -------------------------------------------------- */
class ListboxPlugin extends AccessKitPluginBase {
    initWidgetCore(widget) {
        const list = widget.root;
        const options = widget.items;

        // Initialise roving tabindex & ensure aria-selected is set
        options.forEach(opt => {
            if (!opt.hasAttribute('aria-selected')) {
                opt.setAttribute('aria-selected', 'false');
            }
        });

        // Pointer interaction mirrors keyboard behaviour
        list.addEventListener('click', e => {
            const opt = e.target.closest(widget.itemSelector);
            if (opt) {
                this.move(opt, widget);
                this.toggleSelect(opt, widget, false, true);
            }
        });

        // Make listbox focusable itself (fallback if options are removed dynamically)
        if (!list.hasAttribute('tabindex')) {
            list.tabIndex = -1;
        }
    }

    onActivateItem(element, index, widget) {
        this.toggleSelect(element, widget, false, true);
        return true;
    }

    onItemKeyPress(event, index, widget) {
        if (event.key.length === 1 && /\S/.test(event.key)) {
            this._typeahead(event.key, index, widget);
            return true;
        }

        return false;
    }

    move(opt, widget) {
        const handled = super.move(opt, widget);

        // In single‑select listboxes, moving also selects (if manualselect is not true)
        if (!widget.multiselect && !widget.manualselect) {
            this.toggleSelect(opt, widget, true, false);
        }

        return handled;
    }

    /**
     * Toggles the selection state of an option inside a listbox widget.
     *
     * In single‑select mode (or when `replace` is true) all other options are
     * deselected before `opt` is marked as selected. Otherwise the current selection state of `opt` is flipped.
     *
     * @param {HTMLElement} opt                 The option element to toggle.
     * @param {Object}      widget              Widget context containing listbox data.
     * @param {boolean}     [replace=false]     If true, replaces any existing selection.
     * @param {boolean}     [fireClick=true]    If true, triggers a click on `opt`.
     */
    toggleSelect(opt, widget, replace = false, fireClick = true) {
        const list = widget.root;

        if (!widget.multiselect || replace) {
            widget.items.forEach(o => o.setAttribute('aria-selected', o === opt ? 'true' : 'false'));
            this.events.trigger('select.listbox', list, { list, opt });

            // Call click immediately for single select lists.
            if (fireClick) {
                opt.click();
            }
        }
        else {
            const selected = opt.getAttribute('aria-selected') === 'true';
            opt.setAttribute('aria-selected', selected ? 'false' : 'true');
            this.events.trigger(selected ? 'deselect.listbox' : 'select.listbox', list, { list, opt });
        }
    }

    // TODO: (wcag) (mh) This seems to be to expensive. We don't listen for these keys right now.
    // Either find a way to reigister listing for these keys in a smarter way or throw away. See _onKey in base constructor.
    /* -------- First‑character type‑ahead -------- */
    _typeahead(char, startIdx, widget) {
        char = char.toLowerCase();
        const len = widget.items.length;
        for (let i = 1; i <= len; i++) {
            const opt = widget.items[(startIdx + i) % len];
            if ((opt.textContent || '').trim().toLowerCase().startsWith(char)) {
                this.move(opt, widget);
                break;
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

    onActivateItem(element, index, widget) {
        this.select(element, widget)
        return true;
    }

    // Shift roving focus to the next tab.
    move(tab, widget) {
        if (!tab) return;
        const handled = super.move(tab, widget);
        this.select(tab, widget, false); // Do not display tabpanel on move.
        return handled;
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

// TODO: (wcag) (mh) Test with simple menu
/* --------------------------------------------------
*  TreePlugin – 
*  Handles all items of [role="tree"] + [role="treeitem"]
* -------------------------------------------------- */
class TreePlugin extends AccessKitExpandablePluginBase {
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

    getRovingItems(root) {
        return this._visibleItems(root);
    }

    handleKeyCore(e, widget) {
        widget.items = this.getRovingItems(widget.root);
        return super.handleKeyCore(e, widget);
    }

    onActivateItem(element, index, widget) {
        element.trigger ? element.trigger('click') : element.click()
        return true;
    }

    onItemKeyPress(event, index, widget) {
        const key = event.key;
        const k = AccessKit.KEY;
        const item = event.target;

        if (key === k.RIGHT) {
            this.toggleExpanded(item, true, { focusTarget: 'first' });
            return true;
        }

        if (key === k.LEFT || key === k.ESC) {
            this.toggleExpanded(item, false);
            return true;
        }

        if (key === k.TAB) {
            this.applyRoving(widget);
            return false;
        }

        return false;
    }

    move(target, widget) {
        if (!target) return;
        widget.items = this._visibleItems(widget.root);
        return super.move(target, widget);
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

    onActivateItem(element, index, widget) {
        element.click();
        return true;
    }

    onItemKeyPress(event, index, widget) {
        // INFO: This prevents default browser navigation
        // which is possible for radiogroups via ↑ & ← for backward and ↓ & → for forward navigation with no regard of orientation
        return !this._getNavKeys(widget).includes(event.key);
    }

    move(target, widget) {
        const handled = super.move(target, widget);

        // TODO: (wcag) (mh) Evaluate if this is needed
        // In radiogroups, moving also selects (if manualselect is not true)
        if (!widget?.manualselect) target.click();

        return handled;
    }
}

// Boot
(function () {
    // Register default strategies
    AccessKit.register([
        {
            ctor: MenuPlugin,
            name: 'menu',
            rootSelector: '[role="menu"], [role="menubar"]',
            itemSelector: '[role="menuitem"]'
        },
        {
            ctor: ComboboxPlugin,
            name: 'combobox',
            rootSelector: '[role="combobox"]',
            itemSelector: AccessKit.ACTIVE_OPTION_SELECTOR
        },
        {
            ctor: ListboxPlugin,
            name: 'listbox',
            rootSelector: '[role="listbox"]',
            itemSelector: AccessKit.ACTIVE_OPTION_SELECTOR
        },
        {
            ctor: RadioGroupPlugin,
            name: 'radiogroup',
            rootSelector: '[role="radiogroup"]',
            itemSelector: 'input[type="radio"]:not([disabled])'
        },
        {
            ctor: TreePlugin,
            name: 'tree',
            rootSelector: '[role="tree"], [role="group"]',
            itemSelector: '[role="treeitem"]'
        },
        {
            ctor: TablistPlugin,
            name: 'tablist',
            rootSelector: '[role="tablist"]',
            itemSelector: '[role="tab"]',
            defaultOrientation: 'horizontal' 
        },
    ]);
    
    document.addEventListener('DOMContentLoaded', () => {
        window.AK = AccessKit.create(window.AccessKitOptions || {});
    });
})();