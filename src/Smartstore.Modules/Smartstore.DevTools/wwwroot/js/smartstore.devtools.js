const widgetZoneMenuId = '#wz-menu';
let widgetZones;
const zIndex = 1000;

export class DevTools {
    /**
     * Initialize the DevTools widget functionality: Create the widget zone menu and set up event listeners.
     */
    initialize() {
        widgetZones = [];

        this.createSideMenu(widgetZoneMenuId, zIndex);

        // Jump to the zone.
        $(widgetZoneMenuId).on("click", ".wz-zone-pointer", function (e) {
            e.preventDefault();

            let wzName = $(this).text();
            let widetzones = document.querySelectorAll('span[title="' + wzName + '"]');

            if (widetzones) {
                widetzones.forEach((wz, index) => {
                    let wzPreview = $(wz);
                    wzPreview.addClass('wz-highlight');

                    // If multiple widget zones with the same name exist, we scroll to the first one.
                    if (index == 0) {
                        let wZMenu = $(widgetZoneMenuId);

                        // Make menu see-through, so the user can see covered widget zones.
                        wZMenu.addClass('see-through');

                        // Scroll to widget zone.
                        wz.scrollIntoView({ behavior: "smooth", block: "center", inline: "center" });

                        setTimeout(() => {
                            wZMenu.removeClass('see-through');
                        }, 2000);
                    }

                    setTimeout(() => {
                        wzPreview.removeClass('wz-highlight');
                    }, 2000);
                });
            }
        });

        // Add toggle buttons to widget zone menu.
        $(widgetZoneMenuId).on("click", ".wz-toggle", (e) => {
            this.toggleAllZones($(e.currentTarget).data('persistent'));
        });

        // Add event listener to copy widget zone name to clipboard.
        $(widgetZoneMenuId).on("click", ".copy-to-clipboard", (e) => {
            e.preventDefault();
            window.copyTextToClipboard($(e.currentTarget).data('value'));
            return false;
        });

        // When the user presses "Strg + Alt + B" the widget zones will be toggled.
        $(document).on("keydown", (e) => {
            if (e.ctrlKey && e.altKey && e.key === 'b') {
                this.toggleAllZones();
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
            .append('<div class="d-flex p-2 gap-2"><a href="#" class="wz-zone-pointer flex-grow-1 text-light text-decoration-none text-break">' + zone.name
            + '</a><a href="#" class="copy-to-clipboard text-secondary" data-value="' + zone.name + '"><i class="far fa-copy"></i><a></div>')
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
     * Toggles the visibility of all widget zones. If saveInCookie is true, the state will be saved in a cookie.
     */
    toggleAllZones(saveInCookie = false) {
        const zonePreviews = $(document).find('.wz-preview');
        zonePreviews.toggleClass('d-none');

        // Save state in a cookie if requested.
        if (saveInCookie) {
            let wzState = zonePreviews.hasClass('d-none') ? 'hidden' : 'visible';
            document.cookie = '.Smart.WZVisibility=' + wzState + '; path=/; max-age=31536000';
        }
    }

    /**
     * Turns an element into a sidebar menu.
     */
    createSideMenu(selector, zIndex) {
        let menuWidth = '250'; // in px
        let menu = $(selector);

        menu.addClass("wz-sidebar position-fixed top-0 p-2 d-flex flex-column justify-content-center gap-1 bg-dark overflow-hidden")
            .removeClass("d-none")
            .width(menuWidth)
            .css({
                'height': '100%',
                'left': '-' + menuWidth * 1.2 + 'px',
                'z-index': zIndex + 1,
                'transition': '0.35s ease-in-out'
            });

        // Add close button to menu.
        menu.prepend('<button class="wz-sidebar-close btn btn-dark btn-outline-light"><i class="fa fa-times"></i></button>')
            .find(".wz-sidebar-close").on("click", () => {
                menu.css('left', '-' + menuWidth * 1.2 + 'px');
            });

        // Add toggle button outside the menu.
        // TODO: (mw) (dt) Button misses an explanatory tooltip.
        menu.after('<button class="wz-sidebar-toggle position-fixed top-0 start-0 m-2 mt-6 btn btn-dark"><i class="far fa-layer-group"></i></button>')
            .next().on("click", () => {
                menu.css('left', 0);
            }).css('z-index', zIndex);
    }
}
