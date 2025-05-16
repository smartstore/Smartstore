window.AccessibilityKeyHandler = class {
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
        const navElems = megaMenuAPI.navElems;
        const self = this;

        // Handles key events for main nav items.
        navElems.on("keydown", (e) => {
            const key = e.key;
            const currentIndex = navElems.index($(e.currentTarget));

            // ←, →, Home, End
            const directionalKeys = ['ArrowRight', 'ArrowLeft', 'Home', 'End'];
            if (directionalKeys.includes(key)) {
                e.preventDefault();

                let newIndex;
                if (key === 'Home') newIndex = 0;
                else if (key === 'End') newIndex = navElems.length - 1;
                else {
                    const dir = key === 'ArrowRight' ? 1 : -1;
                    newIndex = (currentIndex + dir + navElems.length) % navElems.length;
                }

                navElems.find('.nav-link').attr('tabindex', '-1');
                navElems.eq(newIndex).find('.nav-link').attr('tabindex', '0').focus();
                return;
            }

            // ↓ / ↑ / Space → Dropdown open
            if (key === 'ArrowDown' || key === 'ArrowUp' || key === ' ') {
                e.preventDefault();

                const activeLink = $(".nav-item .nav-link[tabindex='0']");

                megaMenuAPI.tryOpen(activeLink);

                const dropdown = $('#' + activeLink.attr('aria-controls'));
                if (!dropdown.length) return;

                if (isSimple) {
                    megaMenuAPI.alignDrop(activeLink.parent(), dropdown.find(".dropdown-menu"), megamenu);
                }

                const items = dropdown.find('[role="menuitem"]:visible');
                const focusIndex = key === 'ArrowUp' ? items.length - 1 : 0;

                items.attr('tabindex', '-1');
                items.eq(focusIndex).attr('tabindex', '0').focus();
                return;
            }

            // Tab must reset all index attributes and set the first element to tabindex=0. 
            if (key === 'Tab') {
                self.resetTabIndexes(navElems);
                navElems.eq(0).find('.nav-link').attr('tabindex', '0');
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
                case 'ArrowDown':
                    e.preventDefault();
                    newIndex = (currentIndex + 1) % items.length;
                    break;

                case 'ArrowUp':
                    e.preventDefault();
                    newIndex = (currentIndex - 1 + items.length) % items.length;
                    break;

                case 'ArrowRight':
                    e.preventDefault();
                    newIndex = self._getItemInNextColumn(e.currentTarget, +1, items);
                    break;

                case 'ArrowLeft':
                    e.preventDefault();
                    newIndex = self._getItemInNextColumn(e.currentTarget, -1, items);
                    break;

                case 'Home':
                    e.preventDefault();
                    newIndex = 0;
                    break;

                case 'End':
                    e.preventDefault();
                    newIndex = items.length - 1;
                    break;

                case 'Escape':
                    e.preventDefault();
                    var activeLink = $(".nav-item.active .nav-link");
                    megaMenuAPI.closeNow(activeLink);
                    activeLink.focus();
                    return;

                case 'Tab':
                    e.preventDefault();
                    var activeLink = $(".nav-item.active .nav-link");
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

            items.attr('tabindex', '-1');
            items.eq(newIndex).attr('tabindex', '0').focus();
        });

        $('.megamenu-dropdown-container.simple').on('keydown', '[role="menuitem"]', function (e) {
            const $el = $(this);
            const key = e.key;
            const menu = currentItem.closest('[role="menu"]');

            const menuItems = menu.find('[role="menuitem"]:visible').filter(function () {
                return $el.closest('[role="menu"]')[0] === menu[0];
            });

            const currentIndex = menuItems.index($el);

            if (key === 'ArrowDown') {
                e.preventDefault();
                const nextItem = menuItems.eq(currentIndex + 1);
                nextItem.focus();
            }

            if (key === 'ArrowUp') {
                e.preventDefault();
                const prevIndex = (currentIndex - 1 + menuItems.length) % menuItems.length;
                const prevItem = menuItems.eq(prevIndex);
                prevItem.focus();
            }

            if (key === 'ArrowRight') {
                e.preventDefault();
                if ($el.attr('aria-haspopup') === 'menu') {
                    const submenuId = $el.attr('aria-controls');
                    const submenu = $('#' + submenuId);
                    // TODO: Maybe build a function for this? See identical code above
                    const submenuItems = submenu.find('[role="menuitem"]:visible').filter(function () {
                        return $el.closest('[role="menu"]')[0] === submenu[0];
                    });

                    if (submenuItems.length) {
                        showDrop($el.parent());
                        submenuItems.first().focus();
                    }
                }
            }

            if (key === 'ArrowLeft' || key === 'Escape') {
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
                    const activeMainItem = $('.nav-item .nav-link[tabindex="0"]');
                    activeMainItem.focus();
                    const submenuId = activeMainItem.attr('aria-controls');
                    const submenu = $('#' + submenuId);
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

    // Private methods
    static _handleBrandMenuNavigation(e, currentItem) {
        const allItems = $('#dropdown-menu-brand.').find('[role="menuitem"]:visible');
        const currentIndex = allItems.index(currentItem);
        // TODO: WCDG (mh) Should we extract this from artlist class 'artlist-8-cols'? or count by ourseleves?
        const cols = 8;

        // INFO: Code for col extraction from artlist class.
        //const container = $('#dropdown-menu-brand .artlist');
        //const match = container.attr('class').match(/artlist-(\d+)-cols/);
        //const cols = match ? parseInt(match[1], 10) : 1; // Fallback: 1 Spalte

        let newIndex = currentIndex;

        switch (e.key) {
            case 'ArrowDown':
                newIndex = (currentIndex + cols) % allItems.length;
                break;
            case 'ArrowUp':
                newIndex = (currentIndex - cols + allItems.length) % allItems.length;
                break;
            case 'ArrowRight':
                newIndex = (currentIndex + 1) % allItems.length;
                break;
            case 'ArrowLeft':
                newIndex = (currentIndex - 1 + allItems.length) % allItems.length;
                break;
            case 'Home': {
                const rowStart = Math.floor(currentIndex / cols) * cols;
                newIndex = rowStart;
                break;
            }
            case 'End': {
                const rowStart = Math.floor(currentIndex / cols) * cols;
                const rowEnd = Math.min(rowStart + cols - 1, allItems.length - 1);
                newIndex = rowEnd;
                break;
            }
            default:
                return;
        }

        e.preventDefault();
        allItems.attr('tabindex', '-1');
        allItems.eq(newIndex).attr('tabindex', '0').focus();
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