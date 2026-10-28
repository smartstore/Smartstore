$(function () {
    // Header appearance also follows nested scrolling containers.
    const sectionHeader = $('.section-header');

    const headerScrollRoots = new Set([document.scrollingElement]);
    sectionHeader.each(function () {
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
        document.body.classList.toggle('section-header-scrolled', scrolled);
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

    // SPA and module layouts may still reserve an absolute header outside their scrollable content.
    const fixedSectionHeaders = sectionHeader.filter(function () {
        return !this.classList.contains('nofix') && getComputedStyle(this).position === 'absolute';
    });
    if (fixedSectionHeaders.find('.options').children().length) {
        $(window).on('scroll resize', function () {
            const sticky = window.scrollY >= 1;
            fixedSectionHeaders.toggleClass('sticky', sticky);
            $(document.body)
                .toggleClass('sticky-header', sticky)
                .css('--content-offset', sticky ? fixedSectionHeaders.outerHeight() + 'px' : '');
        }).trigger('resize');
    }

    // Refresh adaptive panes after an alert has been removed from the layout.
    $(document).on('close.bs.alert', '.alert', function () {
        // The closed event fires on the detached alert, so bind directly.
        $(this).one('closed.bs.alert', function () {
            $('.tab-pane-adaptive:visible').trigger('layout:refresh');
        });
    });

    // Pane resizer
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

        resizer.classList.add('is-resizing');

        // Resize once to get current computed size
        var size = resize();

        var onMouseMove = function (ref) {
            var pageX = ref.pageX;
            size = resize(initialPaneWidth, pageX - initialPageX);
        };

        var onMouseUp = function () {
            // Run resize one more time to set computed width/height.
            size = resize(pane.clientWidth);

            resizer.classList.remove('is-resizing');

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

    // Popup toggle
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


    const navbar = $("#navbar");
    const sidebarToggle = document.getElementById('navbar-sidebar-toggle');
    if (!navbar.length || !sidebarToggle) {
        return;
    }

    const sidebarDesktop = window.matchMedia('(min-width: 992px)');
    const sidebarStorageKey = 'smartstore.admin.sidebar';

    const sidebarTooltipTargets = navbar.find('#navbar-menu > .navbar-nav > .nav-item > .nav-link, #current-user, #navbar-sidebar-toggle');
    const sidebarTooltipBlocked = new WeakSet();
    let sidebarTooltipTransitionTimer = 0;
    let sidebarTooltipTarget = null;
    let sidebarTooltipVisible = null;
    let sidebarChanging = false;
    let sidebarDropdownOpening = false;

    function sidebarTooltipsAllowed() {
        return !(sidebarDesktop.matches && document.body.classList.contains('admin-sidebar-expanded'))
            && !sidebarChanging && !sidebarDropdownOpening && !navbar.find('.nav-item.dropdown.show').length;
    }

    function hideSidebarTooltip() {
        sidebarTooltipTarget = null;
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

    function suspendSidebarTooltips() {
        sidebarChanging = true;
        hideSidebarTooltip();
        requestAnimationFrame(() => {
            clearTimeout(sidebarTooltipTransitionTimer);
            const duration = parseFloat(getComputedStyle(navbar[0]).transitionDuration) * 1000;
            sidebarTooltipTransitionTimer = setTimeout(() => sidebarChanging = false, duration + 16);
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
        if (!sidebarTooltipsAllowed() || sidebarTooltipTarget !== this || sidebarTooltipBlocked.has(this)) {
            e.preventDefault();
        }
    }).on('mouseenter.sidebarTooltip focusin.sidebarTooltip', function (e) {
        if (e.type === 'mouseenter') sidebarTooltipBlocked.delete(this);
        if (sidebarTooltipBlocked.has(this)) return;
        hideSidebarTooltip();
        if (!sidebarTooltipsAllowed()) return;
        sidebarTooltipTarget = this;
        sidebarTooltipVisible = this;
        $(this).tooltip('show');
    }).on('mouseleave.sidebarTooltip focusout.sidebarTooltip', function (e) {
        if (e.type === 'mouseleave' || !navbar.find('.nav-item.dropdown.show').length) {
            sidebarTooltipBlocked.delete(this);
        }
        if (sidebarTooltipTarget === this || sidebarTooltipVisible === this) hideSidebarTooltip();
    }).on('pointerdown.sidebarTooltip click.sidebarTooltip keydown.sidebarTooltip', function () {
        sidebarTooltipBlocked.add(this);
        hideSidebarTooltip();
    });

    function closeSidebarMenus() {
        navbar.find('.nav-item.dropdown.show > [data-toggle="dropdown"]').each(function () {
            $(this).dropdown('hide').attr('aria-expanded', 'false');
        });
    }

    function updateSidebarToggle() {
        const expanded = sidebarDesktop.matches && document.body.classList.contains('admin-sidebar-expanded');
        const label = navbar[0].dataset[expanded ? 'collapseLabel' : 'expandLabel'];
        sidebarToggle.disabled = !sidebarDesktop.matches;
        sidebarToggle.setAttribute('aria-expanded', String(expanded));
        sidebarToggle.setAttribute('aria-label', label);
        sidebarToggle.removeAttribute('title');
        sidebarToggle.setAttribute('data-original-title', label);
        sidebarTooltipTargets.tooltip(expanded ? 'disable' : 'enable');
        sidebarToggle.querySelector('.navbar-label').textContent = navbar[0].dataset[expanded ? 'collapseText' : 'expandText'];
    }

    sidebarToggle.addEventListener('click', function () {
        suspendSidebarTooltips();
        closeSidebarMenus();
        const expanded = document.body.classList.toggle('admin-sidebar-expanded');
        try {
            localStorage.setItem(sidebarStorageKey, expanded ? 'expanded' : 'compact');
        }
        catch { /* Storage may be unavailable in restricted browser contexts. */ }
        updateSidebarToggle();
        // Existing editors also need a resize notification when sidebar geometry changes.
        requestAnimationFrame(() => window.dispatchEvent(new Event('resize')));
    });
    navbar[0].addEventListener('transitionend', function (e) {
        if (e.target === this && e.propertyName === 'width') {
            clearTimeout(sidebarTooltipTransitionTimer);
            sidebarChanging = false;
            window.dispatchEvent(new Event('resize'));
        }
    });
    sidebarDesktop.addEventListener('change', function () {
        suspendSidebarTooltips();
        closeSidebarMenus();
        updateSidebarToggle();
    });
    updateSidebarToggle();
    requestAnimationFrame(() => requestAnimationFrame(() => document.body.classList.add('admin-sidebar-ready')));

    function positionSidebarMenu(dropdown) {
        const toggle = dropdown.querySelector(':scope > [data-toggle="dropdown"]');
        const menu = dropdown.querySelector(':scope > .dropdown-menu');
        if (!toggle || !menu) return;

        const viewportHeight = document.documentElement.clientHeight;
        const trigger = toggle.getBoundingClientRect();
        const opensAbove = dropdown.closest('.navbar-account') !== null;
        const menuStyle = getComputedStyle(menu);
        const left = opensAbove ? Math.max(8, trigger.left) : navbar[0].getBoundingClientRect().right;
        const bottom = opensAbove ? trigger.top - 4 : viewportHeight - 8;
        const availableHeight = Math.max(0, bottom - 8);
        menu.style.setProperty('--sidebar-menu-left', `${left}px`);
        menu.style.setProperty('--sidebar-menu-max-width', `${Math.max(0, document.documentElement.clientWidth - left - 8)}px`);
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

    const menuObserver = new ResizeObserver(positionOpenSidebarMenus);
    menuObserver.observe(navbar[0]);
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

    navbar.on('click', '.sidebar-submenu-back', function (e) {
        e.preventDefault();
        e.stopPropagation();
        const group = $(this).closest('.dropdown-group');
        closeDrop(group);
        group.children('.dropdown-item').trigger('focus');
    });

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

    $(window).on('resize', positionOpenSidebarMenus);
    $('#navbar-menu').on('scroll', positionOpenSidebarMenus);

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
