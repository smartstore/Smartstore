$(function () {
    // Fit header actions to the available space, retaining Save labels and buttons as long as possible.
    const sectionHeader = $('.section-header');
    const viewport = ResponsiveBootstrapToolkit;

    // Collect header actions centrally so individual Razor views do not need responsive markup.
    sectionHeader.each(function () {
        const options = this.querySelector('.options');
        const title = this.querySelector('.title');
        if (!options || !title) return;

        // Input groups and existing dropdowns own their buttons and must remain intact.
        const actions = Array.from(options.querySelectorAll('button.btn, a.btn, .vr'))
            .filter(button => !button.closest('.dropdown-menu, .input-group') && !button.matches('[data-toggle="dropdown"]'))
            .map(button => {
                // Retain each original position, including separators and nested toolbar containers.
                const placeholder = document.createComment('Header action');
                const divider = button.classList.contains('vr');
                const priority = button.matches('[value="save"], [value="save-continue"], [name="save"], [name="save-continue"]');
                const label = button.textContent.trim();
                if (!divider && label && !button.hasAttribute('aria-label')) button.setAttribute('aria-label', label);
                // Legacy Save/Continue buttons may contain bare text instead of a label span.
                if (priority) $(button).contents().filter(function () {
                    return this.nodeType === Node.TEXT_NODE && this.textContent.trim();
                }).wrapAll('<span></span>');
                const icons = Array.from(button.children).filter(child => child.matches('.icon, .icon-stack, sm-icon, sm-icon-stack'));
                // Reserve the same icon column for every menu item, including actions without artwork.
                const fixedWidthIcons = icons.filter(icon => !icon.classList.contains('icon-fw'));
                const compactIcon = priority && !icons.length
                    ? $('<sm-icon class="d-none"></sm-icon>').attr('name', button.value === 'save-continue' || button.name === 'save-continue' ? 'disk' : 'check').prependTo(button)[0]
                    : null;
                const spacer = divider || icons.length ? null : Object.assign(document.createElement('span'), { className: 'icon icon-fw' });
                spacer?.setAttribute('aria-hidden', 'true');
                return { button, placeholder, divider, priority, compactIcon, fixedWidthIcons, spacer, classes: Array.from(button.classList).filter(name => name === 'vr' || name === 'btn' || name.startsWith('btn-')) };
            });
        const buttons = actions.filter(action => !action.divider);
        if (!buttons.length) return;
        const overflowActions = actions.filter(action => !action.priority);
        const secondaryButtons = buttons.filter(action => !action.priority);

        actions.forEach(({ button, placeholder }) => button.before(placeholder));
        const dropdown = $('<div class="dropdown section-header-actions" hidden></div>').appendTo(options);
        const toggle = $('<button type="button" class="btn btn-secondary btn-icon dropdown-toggle no-chevron" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"></button>')
            .attr('aria-label', document.body.dataset.headerActionsLabel)
            .append($(document.createElement('sm-icon')).attr('name', 'ellipsis'))
            .appendTo(dropdown);
        const menu = $('<div class="dropdown-menu dropdown-menu-right"></div>').appendTo(dropdown)[0];

        let compactState = false;
        function setCompact(compact) {
            // Only move actions when their menu presentation changes.
            if (compact === compactState) return;
            compactState = compact;
            dropdown.prop('hidden', !compact || !secondaryButtons.length);
            overflowActions.forEach(({ button, placeholder, divider, fixedWidthIcons, spacer, classes }) => {
                // Apply and undo menu presentation in one place; retain functional classes and existing icons.
                button.classList.toggle(divider ? 'dropdown-divider' : 'dropdown-item', compact);
                classes.forEach(name => button.classList.toggle(name, !compact));
                fixedWidthIcons.forEach(icon => icon.classList.toggle('icon-fw', compact));
                if (compact) {
                    // Move the original nodes to preserve handlers, form ownership and submit values.
                    if (spacer) button.prepend(spacer);
                    menu.append(button);
                }
                else {
                    spacer?.remove();
                    placeholder.after(button);
                }
            });
        }

        const fits = () => title.scrollWidth <= title.clientWidth + 1
            && options.getBoundingClientRect().right <= this.getBoundingClientRect().right;

        function updateHeaderActions() {
            if (!options.getClientRects().length) return;
            // Close Bootstrap's dropdown before moving its contents.
            toggle.dropdown('hide');
            setCompact(false);
            buttons.forEach(action => {
                action.button.classList.toggle('label-hidden', false);
                action.compactIcon?.classList.toggle('d-none', true);
            });
            if (fits()) return;
            secondaryButtons.forEach(action => action.button.classList.toggle('label-hidden', true));
            if (fits()) return;
            buttons.forEach(action => {
                action.button.classList.toggle('label-hidden', true);
                action.compactIcon?.classList.toggle('d-none', false);
            });
            if (!fits()) setCompact(true);
        }

        let resizeFrame = 0;
        const scheduleUpdate = () => {
            if (!resizeFrame) resizeFrame = requestAnimationFrame(() => {
                resizeFrame = 0;
                updateHeaderActions();
            });
        };
        const observer = new ResizeObserver(scheduleUpdate);
        observer.observe(this);
        observer.observe(title);
        $(window).on('resize', scheduleUpdate);
        document.fonts.ready.then(scheduleUpdate);
        scheduleUpdate();
    });
    // Mark a scrolled page only when it has a CSS-sticky header. Pages without one need no fade effect.
    // Inspect the actual header and scrollports instead of maintaining a list of excluded page types.
    const headerScrollRoots = new Set();
    sectionHeader.each(function () {
        if (getComputedStyle(this).position !== 'sticky') return;
        headerScrollRoots.add(document.scrollingElement);
        for (let parent = this.parentElement; parent && parent !== document.body; parent = parent.parentElement) {
            if (/^(auto|scroll|overlay)$/.test(window.getComputedStyle(parent).overflowY)) {
                headerScrollRoots.add(parent);
            }
        }
    });

    let headerScrollFrame = 0;
    function updateHeaderAppearance() {
        headerScrollFrame = 0;
        const scrolled = Array.from(headerScrollRoots).some(root => root && root.scrollTop > 0);
        document.body.classList.toggle('sticky-header', scrolled);
    }
    function scheduleHeaderAppearance() {
        if (!headerScrollFrame) {
            headerScrollFrame = requestAnimationFrame(updateHeaderAppearance);
        }
    }
    document.addEventListener('scroll', function (e) {
        if (e.target === document || headerScrollRoots.has(e.target)) {
            scheduleHeaderAppearance();
        }
    }, { capture: true, passive: true });
    window.addEventListener('pageshow', updateHeaderAppearance);
    updateHeaderAppearance();

    // Refresh adaptive panes after an alert has been removed from the layout.
    $(document).on('close.bs.alert', '.alert', function () {
        // The closed event fires on the detached alert, so bind directly.
        $(this).one('closed.bs.alert', function () {
            $('.tab-pane-adaptive:visible').trigger('layout:refresh');
        });
    });

    // Let users resize adjacent panes by dragging their separator, preserving pixel or percentage sizing.
    // Notify the affected pane when dragging ends so its contents can adapt to the new width.
    $(document).on('mousedown', '.resizer', function (e) {
        var resizer = this;
        var resizeNext = resizer.classList.contains('resize-next');
        var initialPageX = e.pageX;
        var pane = resizeNext ? resizer.nextElementSibling : resizer.previousElementSibling;

        if (!pane)
            return;

        var container = resizer.parentNode;
        var initialPaneWidth = pane.offsetWidth;

        var usePercentage = !!(pane.style.width + '').match('%');

        var addEventListener = document.addEventListener;
        var removeEventListener = document.removeEventListener;

        var resize = function (initialSize, offset) {
            if (offset === void 0) offset = 0;

            if (resizeNext)
                offset = offset * -1;

            var containerWidth = container.clientWidth;
            var paneWidth = initialSize + offset;

            return (pane.style.width = usePercentage
                ? paneWidth / containerWidth * 100 + '%'
                : paneWidth + 'px');
        };

        resizer.classList.add('resizing');

        // Resize once to get current computed size
        var size = resize();

        var onMouseMove = function (ref) {
            var pageX = ref.pageX;
            size = resize(initialPaneWidth, pageX - initialPageX);
        };

        var onMouseUp = function () {
            // Run resize one more time to set computed width/height.
            size = resize(pane.clientWidth);

            resizer.classList.remove('resizing');

            removeEventListener('mousemove', onMouseMove);
            removeEventListener('mouseup', onMouseUp);

            // Create resized event
            var data = { "pane": pane, "resizer": resizer, "width": pane.style.width, "initialWidth": initialPaneWidth };
            var event = new CustomEvent("resized", { "detail": data });

            // Trigger the event
            resizer.dispatchEvent(event);
        };

        addEventListener('mousemove', onMouseMove);
        addEventListener('mouseup', onMouseUp);
    });

    // Open links marked as popup toggles in the shared modal instead of navigating away from the page.
    // This variant uses compact, scrollable popup content with an invisible backdrop.
    $(document).on('click', '.popup-toggle', function (e) {
        e.preventDefault();
        openPopup({
            url: this.href,
            large: false,
            flex: true,
            closer: false,
            keyboard: true,
            backdrop: 'invisible',
            centered: true,
            scrollable: true
        });
        return false;
    });


    // Control the main admin navigation: persist desktop expansion, expose a mobile overlay and manage flyouts.
    // Popup and other layouts without the main sidebar still use the header logic above but skip this section.
    const navbar = $("#navbar");
    const sidebarToggle = document.getElementById('navbar-sidebar-toggle');
    if (!navbar.length || !sidebarToggle) {
        return;
    }

    const navbarElement = navbar[0];
    const sidebarDesktop = () => viewport.is('>=lg');
    const sidebarMobile = () => viewport.is('xs');
    const sidebarExpanded = () => sidebarDesktop() && document.body.classList.contains('sidebar-expanded');
    const sidebarMobileToggle = document.getElementById('navbar-mobile-toggle');
    const sidebarMobileClose = document.getElementById('navbar-mobile-close');
    const sidebarStorageKey = 'smartstore.admin.sidebar';

    // Place the mobile navigation toggle in the section-header title so navigation is reachable on small screens.
    // Reuse the centrally rendered button. CSS reveals it after placement via sidebar-ready.
    // Headerless pages keep the button at its original position above the content.
    const headerTitle = sectionHeader.first().find('.title')[0];
    if (sidebarMobileToggle && headerTitle) {
        headerTitle.prepend(sidebarMobileToggle);
    }

    // Open the existing sidebar over the page at XS, keeping its focus and accessibility state in sync.
    // Closing the overlay must not change the user's saved desktop sidebar preference.
    function setMobileSidebarOpen(open, restoreFocus = true) {
        // The mobile overlay is transient and never overwrites the persisted desktop expansion state.
        const mobile = sidebarMobile();
        open = mobile && open;
        closeSidebarMenus();
        hideSidebarTooltip();
        document.body.classList.toggle('sidebar-mobile-open', open);
        // Offscreen navigation must not remain reachable by keyboard or assistive technology.
        navbarElement.inert = mobile && !open;
        sidebarMobileToggle?.setAttribute('aria-expanded', String(open));
        if (open) {
            navbarElement.querySelector('a[href]')?.focus({ preventScroll: true });
        }
        else if (restoreFocus && mobile) {
            sidebarMobileToggle?.focus({ preventScroll: true });
        }
    }

    // Provide mobile open/close controls and dismiss the overlay on outside interaction or Escape.
    // Explicit close actions return focus to the toggle; outside interaction keeps focus on its target.
    sidebarMobileToggle?.addEventListener('click', function () {
        setMobileSidebarOpen(!document.body.classList.contains('sidebar-mobile-open'));
    });
    sidebarMobileClose?.addEventListener('click', () => setMobileSidebarOpen(false));
    // Pointer events cover mouse, pen and touch. Let the outside target retain its normal focus/action.
    document.addEventListener('pointerdown', function (e) {
        if (document.body.classList.contains('sidebar-mobile-open')
            && !navbarElement.contains(e.target) && !sidebarMobileToggle?.contains(e.target)) {
            setMobileSidebarOpen(false, false);
        }
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.body.classList.contains('sidebar-mobile-open')) {
            e.preventDefault();
            setMobileSidebarOpen(false);
        }
    });

    // Compact navigation needs labels as tooltips; expanded/mobile navigation already shows the text.
    // Block a recently activated trigger until pointer intent changes, preventing tooltip flashes.
    const sidebarTooltipTargets = navbar.find('#navbar-menu > .navbar-nav > .nav-item > .nav-link, #current-user, #navbar-sidebar-toggle');
    const sidebarTooltipBlocked = new WeakSet();
    let sidebarTooltipTransitionTimer = 0;
    let sidebarTooltipVisible = null;
    let sidebarChanging = false;
    let sidebarDropdownOpening = false;

    function sidebarTooltipsAllowed() {
        return !sidebarMobile() && !sidebarExpanded()
            && !sidebarChanging && !sidebarDropdownOpening && !navbar.find('.nav-item.dropdown.show').length;
    }

    function hideSidebarTooltip() {
        if (sidebarTooltipVisible) {
            const tooltip = $(sidebarTooltipVisible);
            sidebarTooltipVisible = null;
            const instance = tooltip.data('bs.tooltip');
            if (instance) {
                // Drain the entrance callback before reusing the tip; dismissal never waits for a fade.
                $(instance.getTipElement()).trigger('transitionend').removeClass('fade');
            }
            tooltip.tooltip('hide');
        }
    }

    // Finish the same transition state for both transitionend and the reduced-motion timeout fallback.
    function finishSidebarTransition() {
        clearTimeout(sidebarTooltipTransitionTimer);
        sidebarChanging = false;
        updateSidebarToggleIcon();
    }

    function suspendSidebarTooltips() {
        // The timeout also updates the icon when reduced motion or missing transitions suppress transitionend.
        sidebarChanging = true;
        hideSidebarTooltip();
        requestAnimationFrame(() => {
            clearTimeout(sidebarTooltipTransitionTimer);
            const duration = parseFloat(getComputedStyle(navbarElement).transitionDuration) * 1000;
            sidebarTooltipTransitionTimer = setTimeout(finishSidebarTransition, duration + 16);
        });
    }

    // Manual intent handling starts the Bootstrap entrance animation immediately.
    sidebarTooltipTargets.tooltip({
        trigger: 'manual',
        delay: 0,
        template: '<div class="tooltip sidebar-tooltip" role="tooltip"><div class="arrow"></div><div class="tooltip-inner"></div></div>',
        placement: 'right',
        fallbackPlacement: ['right'],
        boundary: 'viewport',
        container: 'body',
        animation: true,
        html: false,
        title: function () { return this.getAttribute('aria-label'); }
    }).on('show.bs.tooltip.sidebarTooltip', function (e) {
        if (!sidebarTooltipsAllowed() || sidebarTooltipVisible !== this || sidebarTooltipBlocked.has(this)) {
            e.preventDefault();
        }
    }).on('mouseenter.sidebarTooltip focusin.sidebarTooltip', function (e) {
        if (e.type === 'mouseenter') sidebarTooltipBlocked.delete(this);
        if (sidebarTooltipBlocked.has(this)) return;
        hideSidebarTooltip();
        if (!sidebarTooltipsAllowed()) return;
        sidebarTooltipVisible = this;
        $(this).tooltip('show');
    }).on('mouseleave.sidebarTooltip focusout.sidebarTooltip', function (e) {
        if (e.type === 'mouseleave' || !navbar.find('.nav-item.dropdown.show').length) {
            sidebarTooltipBlocked.delete(this);
        }
        if (sidebarTooltipVisible === this) hideSidebarTooltip();
    }).on('pointerdown.sidebarTooltip click.sidebarTooltip keydown.sidebarTooltip', function () {
        sidebarTooltipBlocked.add(this);
        hideSidebarTooltip();
    });

    function closeSidebarMenus() {
        navbar.find('.nav-item.dropdown.show > [data-toggle="dropdown"]').each(function () {
            $(this).dropdown('hide').attr('aria-expanded', 'false');
        });
    }

    function updateSidebarToggleIcon() {
        sidebarToggle.classList.toggle('expanded', sidebarExpanded());
    }

    function updateSidebarToggle() {
        const desktop = sidebarDesktop();
        const expanded = desktop && document.body.classList.contains('sidebar-expanded');
        const label = navbarElement.dataset[expanded ? 'collapseLabel' : 'expandLabel'];
        sidebarToggle.disabled = !desktop;
        sidebarToggle.setAttribute('aria-expanded', String(expanded));
        sidebarToggle.setAttribute('aria-label', label);
        sidebarToggle.removeAttribute('title');
        sidebarToggle.setAttribute('data-original-title', label);
        sidebarTooltipTargets.tooltip(expanded ? 'disable' : 'enable');
    }

    // Toggle and persist the desktop sidebar width. Keep menus closed during the transition and update
    // the toggle icon only after the animation, then notify editors that their available width has changed.
    sidebarToggle.addEventListener('click', function () {
        suspendSidebarTooltips();
        closeSidebarMenus();
        const expanded = document.body.classList.toggle('sidebar-expanded');
        try {
            localStorage.setItem(sidebarStorageKey, expanded ? 'expanded' : 'compact');
        }
        catch { /* Storage may be unavailable in restricted browser contexts. */ }
        updateSidebarToggle();
        // Existing editors also need a resize notification when sidebar geometry changes.
        requestAnimationFrame(() => window.dispatchEvent(new Event('resize')));
    });
    navbarElement.addEventListener('transitionend', function (e) {
        // Keep the current toggle icon stable until the sidebar's width animation has completed.
        if (e.target === this && e.propertyName === 'width') {
            finishSidebarTransition();
            window.dispatchEvent(new Event('resize'));
        }
    });
    // Adapt sidebar controls when the Sass-defined viewport tier changes, closing transient mobile navigation.
    EventBroker.subscribe('page.gridtierchanged', function () {
        // Discard transient menus when CSS switches layout tiers; preserve the desktop preference.
        suspendSidebarTooltips();
        setMobileSidebarOpen(false, false);
        updateSidebarToggle();
    });
    // Apply the initial sidebar state before CSS enables transitions and reveals the mobile toggle.
    navbarElement.inert = sidebarMobile();
    updateSidebarToggle();
    updateSidebarToggleIcon();
    requestAnimationFrame(() => requestAnimationFrame(() => document.body.classList.add('sidebar-ready')));

    // Position root flyouts beside their trigger, shifting upward only as needed to fit the viewport.
    // The account menu opens above its trigger; separate arrows remain aligned with the trigger center.
    function positionSidebarMenu(dropdown) {
        const toggle = dropdown.querySelector(':scope > [data-toggle="dropdown"]');
        const menu = dropdown.querySelector(':scope > .dropdown-menu');
        if (!toggle || !menu) return;

        const { clientWidth: viewportWidth, clientHeight: viewportHeight } = document.documentElement;
        const trigger = toggle.getBoundingClientRect();
        const opensAbove = dropdown.closest('.navbar-account') !== null;
        const menuStyle = getComputedStyle(menu);
        // Sass supplies the mobile inward offset; desktop menus align with the sidebar's outer edge.
        const left = opensAbove ? Math.max(8, trigger.left)
            : navbarElement.getBoundingClientRect().right + (parseFloat(menuStyle.getPropertyValue('--sidebar-menu-offset-x')) || 0);
        const bottom = opensAbove ? trigger.top - 4 : viewportHeight - 8;
        const availableHeight = Math.max(0, bottom - 8);
        menu.style.setProperty('--sidebar-menu-left', `${left}px`);
        menu.style.setProperty('--sidebar-menu-max-width', `${Math.max(0, viewportWidth - left - 8)}px`);
        menu.style.setProperty('--sidebar-menu-max-height', `${availableHeight}px`);
        const borderHeight = menu.offsetHeight - menu.clientHeight;
        menu.classList.toggle('sidebar-menu-scrollable', menu.offsetHeight >= availableHeight - 1 && menu.scrollHeight + borderHeight > availableHeight);
        const height = menu.getBoundingClientRect().height;
        // The account menu ends above its trigger; other menus only shift by their bottom overflow.
        const overflow = Math.max(0, trigger.top + height - bottom);
        const top = opensAbove ? bottom - height : trigger.top - overflow;
        menu.style.setProperty('--sidebar-menu-top', `${Math.max(8, top)}px`);

        const arrow = dropdown.querySelector(':scope > .sidebar-menu-arrow');
        if (arrow) {
            // The arrow is a sibling outside the scrolling menu and always points to the trigger center.
            arrow.style.setProperty('--sidebar-menu-arrow-x', `${left + parseFloat(menuStyle.borderLeftWidth)}px`);
            arrow.style.setProperty('--sidebar-menu-arrow-y', `${trigger.top + trigger.height / 2}px`);
            arrow.style.setProperty('--sidebar-menu-arrow-bg', menuStyle.backgroundColor);
            arrow.style.setProperty('--sidebar-menu-arrow-border-color', menuStyle.borderLeftColor);
        }
    }

    function positionOpenSidebarMenus() {
        navbar.find('.nav-item.dropdown.show').each(function () {
            positionSidebarMenu(this);
        });
    }

    // Keep open flyouts within the viewport as sidebar geometry or menu content changes.
    // Bootstrap lifecycle events also suppress conflicting tooltips and reset closed submenu state.
    const menuObserver = new ResizeObserver(positionOpenSidebarMenus);
    menuObserver.observe(navbarElement);
    navbar.find('.nav-item.dropdown').on('show.bs.dropdown shown.bs.dropdown', function (e) {
        hideSidebarTooltip();
        if (e.type === 'show') {
            sidebarDropdownOpening = true;
            const trigger = this.querySelector(':scope > [data-toggle="dropdown"]');
            if (trigger) sidebarTooltipBlocked.add(trigger);
            queueMicrotask(() => sidebarDropdownOpening = false);
        }
        positionSidebarMenu(this);
        if (e.type === 'shown') {
            menuObserver.observe(this.querySelector(':scope > .dropdown-menu'));
        }
    }).on('hidden.bs.dropdown', function () {
        hideSidebarTooltip();
        const trigger = this.querySelector(':scope > [data-toggle="dropdown"]');
        if (trigger) {
            // Closing a menu must not immediately reshow a tooltip on restored or retained focus.
            sidebarTooltipBlocked.add(trigger);
            queueMicrotask(() => {
                if (document.activeElement !== trigger && !trigger.matches(':hover')) sidebarTooltipBlocked.delete(trigger);
            });
        }
        const menu = this.querySelector(':scope > .dropdown-menu');
        if (menu) {
            menuObserver.unobserve(menu);
            closeDrop($(menu).find('.dropdown-group'));
            menu.scrollTop = 0;
        }
    });

    // Let users return from an overlapping submenu to its parent without closing the whole navigation.
    navbar.on('click', '.sidebar-submenu-back', function (e) {
        e.preventDefault();
        e.stopPropagation();
        const group = $(this).closest('.dropdown-group');
        closeDrop(group);
        group.children('.dropdown-item').trigger('focus');
    });

    // Open submenus with Right Arrow or Space and move keyboard focus to their first available action.
    navbar.on('keydown', '.dropdown-group > .dropdown-item', function (e) {
        if (e.key === 'ArrowRight' || e.key === ' ') {
            e.preventDefault();
            e.stopPropagation();
            const group = $(this).parent();
            clearTimeout(group.data('closeTimeout'));
            navbar.find('.dropdown-group.show').each(function () {
                if (!$.contains(this, group[0]) && !$.contains(group[0], this) && this !== group[0]) {
                    closeDrop($(this));
                }
            });
            const focusFirst = () => group.find('> .dropdown-menu .dropdown-item:not(.disabled):visible').first().trigger('focus');
            if (!group.hasClass('show')) {
                showDrop(group, focusFirst);
            }
            else {
                focusFirst();
            }
        }
    });

    // Navigate submenu actions with arrow/Home/End keys and return to the parent with Escape or Left Arrow.
    navbar.on('keydown', '.dropdown-group > .dropdown-menu', function (e) {
        const group = $(this).parent();
        if (e.key === 'Escape' || e.key === 'ArrowLeft') {
            e.preventDefault();
            e.stopPropagation();
            closeDrop(group);
            group.children('.dropdown-item').trigger('focus');
        }
        else if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(e.key)) {
            e.preventDefault();
            e.stopPropagation();
            const items = $(this).find('.dropdown-item:not(.disabled):visible');
            let index = items.index(document.activeElement);
            if (e.key === 'Home') index = 0;
            else if (e.key === 'End') index = items.length - 1;
            else index = Math.max(0, Math.min(items.length - 1, index + (e.key === 'ArrowDown' ? 1 : -1)));
            items.eq(index).trigger('focus');
        }
    });

    // Keep flyouts attached to their triggers while the viewport or the scrollable navigation changes.
    $(window).on('resize', positionOpenSidebarMenus);
    $('#navbar-menu').on('scroll', positionOpenSidebarMenus);

    // Run the sidebar's cache-clearing actions without leaving the page, then reload to show fresh content.
    $("#clear-database-cache, #clear-cache").on('click', function () {
        $({}).ajax({
            url: $(this).data("url"),
            global: false,
            success: function (data) {
                location.reload();
            }
        });
    });
});
