/// <reference path="smartstore.accessibility.blueprint.js" />

/**
* WCAG‑2.2 keyboard navigation plugins
*/
'use strict';


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
                this.toggleSelect(opt, widget);
            }
        });

        // Make listbox focusable itself (fallback if options are removed dynamically)
        if (!list.hasAttribute('tabindex')) {
            list.tabIndex = -1;
        }
    }

    onActivateItem(element, index, widget) {
        this.toggleSelect(element, widget);
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
        super.move(opt, widget);

        // TODO: Evaluate if this is needed
        // In single‑select listboxes, moving also selects (if manualselect is not true)
        if (!widget.multiselect && !widget.manualselect) {
            this.toggleSelect(opt, widget, true /*replace*/);
        }
    }

    /* -------- Selection handling -------- */
    toggleSelect(opt, widget, replace = false) {
        const list = widget.root;

        if (!widget.multiselect || replace) {
            widget.items.forEach(o => o.setAttribute('aria-selected', o === opt ? 'true' : 'false'));
            this.events.trigger('select.listbox', list, { list, opt });

            // Call click immediately for single select lists.
            opt.click();
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
*  TreePlugin – 
*  Handles all items of [role="tree"] + [role="treeitem"]
* -------------------------------------------------- */
class TreePlugin extends AccessKitExpandablePluginBase {
    //_visibleItems(tree) {
    //    // TODO: (wcag) (mh) Correct this and use cached items. 
    //    return Array.from(tree.querySelectorAll('[role="treeitem"]')).filter(node => {
    //        let anc = node.parentElement?.closest('[role="treeitem"]');
    //        while (anc) {
    //            if (anc.getAttribute('aria-expanded') === 'false') return false;
    //            anc = anc.parentElement?.closest('[role="treeitem"]');
    //        }
    //        return true; // No collapsed ancestor found
    //    });
    //}

    //handleKeyCore(e, widget) {
    //    const item = e.target;
    //    if (!item || item.getAttribute('role') !== 'treeitem')
    //        return false;

    //    const tree = item.closest('[role="tree"]') || item.closest('[role="group"]');
    //    if (!tree)
    //        return false;

    //    const items = this._visibleItems(tree);
    //    if (!items.length)
    //        return false;

    //    const orientation = tree.getAttribute('aria-orientation') ?? 'vertical';

    //    return super.handleKeyCore(e, widget);

    //    return this.handleRovingKeys(e, items, {
    //        orientation,
    //        activateFn: (el) => (el.trigger ? el.trigger('click') : el.click()),
    //        extraKeysFn: (ev, _idx, list) => {
    //            if (ev.key === KEY.RIGHT) {
    //                this.toggleExpanded(item, true, { focusTarget: 'first' });
    //                return true;
    //            }

    //            if (ev.key === KEY.LEFT || ev.key === KEY.ESC) {
    //                this.toggleExpanded(item, false);
    //                return true;
    //            }

    //            if (ev.key === KEY.TAB) {
    //                this.applyRoving(tree, '[role="treeitem"]');
    //                return false;
    //            }

    //            return false;
    //        },
    //    });
    //}

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

        //tree = tree || item.closest('[role="tree"]');
        //siblings = siblings || this._visibleItems(tree);

        // TODO: (wcag) (mc) Filter by visible items only
        super.move(target, widget);
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
        //{
        //    ctor: MenuPlugin.ctor,
        //    name: 'menu',
        //    rootSelector: '[role="menu"], [role="menubar"]',
        //    itemSelector: '[role="menuitem"]'
        //},
        //{
        //    ctor: ComboboxPlugin.ctor,
        //    name: 'combobox',
        //    rootSelector: '[role="combobox"]',
        //    itemSelector: AccessKit.ACTIVE_OPTION_SELECTOR
        //},
        {
            ctor: ListboxPlugin.ctor,
            name: 'listbox',
            rootSelector: '[role="listbox"]',
            itemSelector: AccessKit.ACTIVE_OPTION_SELECTOR
        },
        {
            ctor: RadioGroupPlugin.ctor,
            name: 'radiogroup',
            rootSelector: '[role="radiogroup"]',
            itemSelector: 'input[type="radio"]:not([disabled])'
        },
        {
            ctor: TreePlugin.ctor,
            name: 'tree',
            rootSelector: '[role="tree"], [role="group"]',
            itemSelector: '[role="treeitem"]'
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