const widgetZoneMenuId = '#wz-menu';
let widgetZones;
const zIndex = 1000;
const showZoneClass = 'show-wz';

export class DevTools {
    Res = {};

    /**
     * Initialize the DevTools widget functionality: Create the widget zone menu and set up event listeners.
     */
    initialize(canToggleVisibilityInitially) {
        widgetZones = [];
        let wzMenu = $(widgetZoneMenuId);

        let wzMenuToggle = $('#wz-menu-toggle');
        wzMenuToggle.css('z-index', zIndex);

        let toolDividers = wzMenu.find('.tool-divider');
        let persistentToggleButton = wzMenu.find('.wz-toggle[data-persistent]');
        let temporaryToggleButton = wzMenu.find('.wz-toggle:not([data-persistent])').first();
        persistentToggleButton.data('persistent', canToggleVisibilityInitially);

        // Make sure tooltips are displayed on offcanvas.
        $('#wz-toolbar [data-toggle="tooltip"]').tooltip({
            placement: 'top',
            container: '#wz-toolbar'
        });

        // Add widget zone menu close button.
        wzMenu.on('click', '.wz-sidebar-close', (e) => {
            e.preventDefault();
            wzMenuToggle.click();
            wzMenu.find(".wz-zone-pointer-container.active").removeClass('active');
            return false;
        });

        // Add event listener to copy widget zone name to clipboard.
        wzMenu.on("click", ".copy-to-clipboard", (e) => {
            e.preventDefault();
            window.copyTextToClipboard($(e.currentTarget).data('value'));
            return false;
        });

        // Jump to the zone.
        wzMenu.on("click", ".wz-zone-pointer", (e) => {
            e.preventDefault();

            wzMenu.find(".wz-zone-pointer-container.active").removeClass('active');
            $(e.currentTarget).parent().addClass('active');

            let wzName = $(e.currentTarget).text();
            let widetzones = $('span[title="' + wzName + '"]');

            if (widetzones.length > 0) {
                let wzFirstPreview = widetzones.first();
                let wzIsHidden = wzFirstPreview.hasClass('d-none');

                if (wzIsHidden) {
                    // Must be visible to scroll to it.
                    widetzones.removeClass('d-none');
                }

                // Scroll to widget zone and add highlight.
                this.scrollToElementAndThen(wzFirstPreview[0]).then(() => {
                    widetzones.addClass('wz-highlight');
                    
                    setTimeout(() => {
                        widetzones.removeClass('wz-highlight');

                        if (wzIsHidden) {
                            wzFirstPreview.addClass('d-none');
                        }
                    }, 2400);
                });
            }
        });

        // Add toggle buttons to widget zone menu.
        // Persistent toggle button.
        wzMenu.on("click", ".wz-toggle[data-persistent]", (e) => {
            e.preventDefault();

            let wzToggleButton = $(e.currentTarget);
            let canToggleVisibility = persistentToggleButton.data('persistent');

            canToggleVisibility = !canToggleVisibility;
            wzToggleButton.data('persistent', canToggleVisibility);

            // Save state in a cookie if requested.
            document.cookie = '.Smart.WZVisibility=' + canToggleVisibility + '; path=/; max-age=31536000; SameSite=Lax';

            // Set both buttons to the same state.
            if (canToggleVisibility) {
                temporaryToggleButton.removeClass('disabled').addClass(showZoneClass);
                persistentToggleButton.addClass(showZoneClass);
            } else {
                temporaryToggleButton.addClass('disabled').removeClass(showZoneClass);
                persistentToggleButton.removeClass(showZoneClass);
            }

            this.setVisibilityForAllZones(temporaryToggleButton.hasClass(showZoneClass));
        });

        // Temporary toggle button.
        wzMenu.on("click", ".wz-toggle:not([data-persistent])", (e) => {
            e.preventDefault();

            if (persistentToggleButton.data('persistent')) {
                $(e.currentTarget).toggleClass(showZoneClass);
            }

            this.setVisibilityForAllZones(temporaryToggleButton.hasClass(showZoneClass));
        });

        // Animate dividers.
        wzMenu.on('mouseover', '.wz-tool:not(.disabled)', function (e) {
            if (e.currentTarget == temporaryToggleButton[0]) {
                toolDividers.eq(0).css('opacity', '0');
            }
            else if (e.currentTarget !== persistentToggleButton[0]) {
                toolDividers.eq(1).css('opacity', '0');
            }
            else {
                toolDividers.css('opacity', '0');
            }
        });
        wzMenu.on('mouseout', '.wz-tool', function () {
            toolDividers.css('opacity', '1');
        });

        // Add event listener to copy widget zone name to clipboard.
        wzMenu.on("click", ".copy-to-clipboard", (e) => {
            e.preventDefault();
            window.copyTextToClipboard($(e.currentTarget).data('value'));
            return false;
        });

        // When the user presses "Alt + K" the widget zones will be toggled.
        $(document).on("keydown", (e) => {
            if (e.altKey && e.key === 'k') {
                wzMenu.find('.wz-toggle:not([data-persistent])').first().click();
            }
        });
    }

    /**
     * Register a widget zone and add it to the zone list.
     */
    registerWidgetZone(zone) {
        // If the widget zone is already registered, we skip it.
        if (widgetZones.findIndex(wz => wz.name === zone.name) !== -1) {
            return;
        };

        widgetZones.push(zone);

        let groupName = this.getWidgetZoneGroup(zone.name);

        // Place the widget zone in the correct group and make sure the group is visible.
        $('.wz-zone-group[data-group="' + groupName + '"]')
            .append('<div class="wz-zone-pointer-container"><a href="#" class="wz-zone-pointer text-truncate" title="' + zone.name + '">' + zone.name + '</a>' +
                '<a href="#" class="copy-to-clipboard text-secondary" data-value="' + zone.name + '" title="' + this.Res['Common.CopyToClipboard'] +
                '"><i class="far fa-copy"></i><a></div>')
            .removeClass('d-none');
    }

    /**
     * Returns the group the widget zone should be placed in, using .wz-zone-group[data-zones="..."].
     */
    getWidgetZoneGroup(zoneName) {
        let groups = $('.wz-zone-group');
        let group = groups.filter(function () {
            return $(this).data('groups').split(' ').includes(zoneName);
        });

        // If no group was found, use the last one (custom).
        if (!group.length) {
            // Check if the widget zone has a meta preview tag.
            let wz = widgetZones.find(wz => wz.name === zoneName);
            if (wz.previewTagName == 'meta') {
                return 'Meta';
            }

            group = groups.last();
        }

        return group.data('group');
    }

    /**
     * Sets the visibility of all widget zones.
     */
    setVisibilityForAllZones(showZones) {
        const zonePreviews = $(document).find('.wz-preview');

        if (showZones) {
            zonePreviews.removeClass('d-none');
            $('#wz-toolbar .wz-invisible').addClass('d-none');
            $('#wz-toolbar .wz-visible').removeClass('d-none');
        }
        else
        {
            zonePreviews.addClass('d-none');
            $('#wz-toolbar .wz-invisible').removeClass('d-none');
            $('#wz-toolbar .wz-visible').addClass('d-none');
        }
    }

    /**
     * Returns a promise to smoothly scroll to an element and resolve.
     * @param {number} [timeDelay=100] Set the number of milliseconds between the last scroll and resolution.
     */
    scrollToElementAndThen(element, timeDelay = 50) {
        return new Promise((resolve) => {

            // Check whether scrolling is necessary or not.
            const rect = element.getBoundingClientRect();
            const isInViewport = (
                rect.top >= 0 &&
                rect.left >= 0 &&
                rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
                rect.right <= (window.innerWidth || document.documentElement.clientWidth)
            );

            if (isInViewport) {
                resolve();
            } else {
                element.scrollIntoView({ behavior: "smooth", block: "center", inline: "center" });

                let isScrolling;

                function onScroll() {
                    window.clearTimeout(isScrolling);

                    // Set a timeout to run after scrolling ends
                    isScrolling = setTimeout(() => {
                        window.removeEventListener('scroll', onScroll);
                        resolve();
                    }, timeDelay);
                }

                window.addEventListener('scroll', onScroll);
            }
        });
    }
}
