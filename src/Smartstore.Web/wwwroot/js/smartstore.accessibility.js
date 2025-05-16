window.AccessibilityKeyHandler = class {
    static KEY = {
        LEFT: 'ArrowLeft',
        UP: 'ArrowUp',
        RIGHT: 'ArrowRight',
        DOWN: 'ArrowDown',
        HOME: 'Home',
        END: 'End',
        ESC: 'Escape',
        SPACE: ' ',
        TAB: 'Tab'
    };

    static init(handler, ...args) {
        const method = this[handler];
        if (typeof method === "function") {
            method.call(this, ...args);
        } else {
            console.warn(`AccessibilityKeyHandler: No handler "${handler}" found.`);
        }
    }

    static MegaMenu(menuPlugin, isSimple) {
        const megaMenuAPI = menuPlugin.data('megaMenuAPI');
        const megamenu = megaMenuAPI.megamenu;
        const navElems = megaMenuAPI.navElems;
        const self = this;

        // Handles key events for main nav items.
        navElems.on("keydown", (e) => {
            const key = e.key;
            const currentIndex = navElems.index($(e.currentTarget));

            // ←, →, Home, End
            const directionalKeys = [KEY.RIGHT, KEY.LEFT, KEY.HOME, KEY.END];
            if (directionalKeys.includes(key)) {
                e.preventDefault();

                let newIndex;
                if (key === KEY.HOME) newIndex = 0;
                else if (key === KEY.END) newIndex = navElems.length - 1;
                else {
                    const dir = key === KEY.RIGHT ? 1 : -1;
                    newIndex = (currentIndex + dir + navElems.length) % navElems.length;
                }

                this.setTabIndex(navElems.find('.nav-link'), newIndex);

                return;
            }

            // ↓ / ↑ / Space → Dropdown open
            if (key === KEY.DOWN || key === KEY.UP || key === KEY.SPACE) {
                e.preventDefault();

                const activeLink = megamenu.find(".nav-item .nav-link[tabindex='0']");

                megaMenuAPI.tryOpen(activeLink);

                const dropdown = $('#' + activeLink.attr('aria-controls'));
                if (!dropdown.length) return;

                if (isSimple) {
                    megaMenuAPI.alignDrop(activeLink.parent(), dropdown.find(".dropdown-menu"), megamenu);
                }

                const items = dropdown.find('[role="menuitem"]:visible');
                const newIndex = key === KEY.UP ? items.length - 1 : 0;

                this.setTabIndex(items, newIndex);
                return;
            }

            // KEY.TAB must reset all index attributes and set the first element to tabindex=0.
            if (key === KEY.TAB) {
                this.setTabIndex(navElems.find('.nav-link'), 0);
            }
        });

        // Handles key events for dropdown nav items.
        $('.mega-menu-dropdown').on('keydown', '[role="menuitem"]', (e) => {
            const $el = $(e.currentTarget);
            const isBrandMenu = $el.closest('#dropdown-menu-brand.brand-picture-grid').length > 0;
            if (isBrandMenu) {
                self._handleBrandMenuNavigation(e, e.currentTarget);
                return;
            }

            const items = $('[role="menuitem"]:visible', $el.parents('.mega-menu-dropdown'));
            const currentIndex = items.index(e.currentTarget);
            let newIndex = currentIndex;

            switch (e.key) {
                case KEY.DOWN:
                    e.preventDefault();
                    newIndex = (currentIndex + 1) % items.length;
                    break;

                case KEY.UP:
                    e.preventDefault();
                    newIndex = (currentIndex - 1 + items.length) % items.length;
                    break;

                case KEY.RIGHT:
                    e.preventDefault();
                    newIndex = self._getItemInNextColumn(e.currentTarget, +1, items);
                    break;

                case KEY.LEFT:
                    e.preventDefault();
                    newIndex = self._getItemInNextColumn(e.currentTarget, -1, items);
                    break;

                case KEY.HOME:
                    e.preventDefault();
                    newIndex = 0;
                    break;

                case KEY.END:
                    e.preventDefault();
                    newIndex = items.length - 1;
                    break;

                case KEY.ESC:
                    e.preventDefault();
                    var activeLink = megamenu.find(".nav-item.active .nav-link");
                    megaMenuAPI.closeNow(activeLink);
                    activeLink.focus();
                    return;

                case KEY.TAB:
                    e.preventDefault();
                    var activeLink = megamenu.find(".nav-item.active .nav-link");
                    megaMenuAPI.closeNow(activeLink);
                    if (e.shiftKey) {
                        activeLink.parent().prev().find(".nav-link").focus();
                    }
                    else {
                        activeLink.parent().next().find(".nav-link").focus();
                    }
                    return;

                default:
                    return;
            }

            this.setTabIndex(items, newIndex);
        });

        $('.megamenu-dropdown-container.simple').on('keydown', '[role="menuitem"]', function (e) {
            const $el = $(this);
            const key = e.key;
            const menu = $el.closest('[role="menu"]');

            // Find direct menuitems - no items of submenus!!!
            function findDirectMenuitems(menu) {
                menu.find('[role="menuitem"]:visible').filter(function () {
                    return $(this).closest('[role="menu"]')[0] === menu[0];
                });
            }

            const menuItems = findDirectMenuitems(menu);
            const currentIndex = menuItems.index($el);

            if (key === KEY.DOWN) {
                e.preventDefault();
                const nextItem = menuItems.eq(currentIndex + 1);
                nextItem.focus();
            }

            if (key === KEY.UP) {
                e.preventDefault();
                const prevIndex = (currentIndex - 1 + menuItems.length) % menuItems.length;
                const prevItem = menuItems.eq(prevIndex);
                prevItem.focus();
            }

            if (key === KEY.RIGHT) {
                e.preventDefault();
                if ($el.attr('aria-haspopup') === 'menu') {
                    const submenu = $('#' + $el.attr('aria-controls'));
                    const submenuItems = findDirectMenuitems(submenu)

                    if (submenuItems.length) {
                        showDrop($el.parent());
                        submenuItems.first().focus();
                    }
                }
            }

            if (key === KEY.LEFT || key === KEY.ESC) {
                e.preventDefault();
                const parentGroup = menu.closest('.dropdown-group');
                if (parentGroup.length) {
                    const parentLink = parentGroup.children('[role="menuitem"]');
                    if (parentLink.length) {
                        menu.removeClass('show');
                        parentGroup.removeClass('show');
                        parentLink.focus();
                    }
                } else {
                    // Fallback to close the main dropdown.
                    const activeMainItem = megamenu.find('.nav-item .nav-link[tabindex="0"]');
                    activeMainItem.focus();
                    const submenu = $('#' + activeMainItem.attr('aria-controls'));
                    submenu.removeClass('show');
                    activeMainItem.parent().removeClass('active');
                }
            }
        });
    }

    static ContentSlider() {
        // TODO
    }

    // Public methods
    static resetTabIndexes(context) {
        context.find('[tabindex="0"]').attr('tabindex', '-1');
    }

    static setTabIndex(items, activeIndex) {
        items.attr('tabindex', '-1');
        items.eq(activeIndex).attr('tabindex', '0').focus();
    }

    // Private methods
    static _handleBrandMenuNavigation(e, currentItem) {
        const allItems = $('#dropdown-menu-brand').find('[role="menuitem"]:visible');
        const currentIndex = allItems.index(currentItem);
        const match = $('#dropdown-menu-brand .artlist').attr('class').match(/artlist-(\d+)-cols/);
        const cols = match ? parseInt(match[1], 10) : 8;

        let newIndex = currentIndex;

        switch (e.key) {
            case KEY.DOWN:
                newIndex = (currentIndex + cols) % allItems.length;
                break;
            case KEY.UP:
                newIndex = (currentIndex - cols + allItems.length) % allItems.length;
                break;
            case KEY.RIGHT:
                newIndex = (currentIndex + 1) % allItems.length;
                break;
            case KEY.LEFT:
                newIndex = (currentIndex - 1 + allItems.length) % allItems.length;
                break;
            case KEY.HOME: {
                const rowStart = Math.floor(currentIndex / cols) * cols;
                newIndex = rowStart;
                break;
            }
            case KEY.END: {
                const rowStart = Math.floor(currentIndex / cols) * cols;
                const rowEnd = Math.min(rowStart + cols - 1, allItems.length - 1);
                newIndex = rowEnd;
                break;
            }
            default:
                return;
        }

        e.preventDefault();

        this.setTabIndex(allItems, newIndex);
    }

    static _getItemInNextColumn(el, direction, items) {
        const $el = $(el);
        const col = $el.closest('.col-md-3');
        const cols = col.parent().children('.col-md-3');
        const currentColIndex = cols.index(col);
        const nextColIndex = (currentColIndex + direction + cols.length) % cols.length;
        const nextCol = cols.eq(nextColIndex);
        const nextItems = nextCol.find('[role="menuitem"]:visible');

        // Fallback: Remain in the current index if there is no matching index in another column.
        return nextItems.length > 0 ? items.index(nextItems.eq(0)) : items.index($el);
    }
}

const { KEY } = AccessibilityKeyHandler;  