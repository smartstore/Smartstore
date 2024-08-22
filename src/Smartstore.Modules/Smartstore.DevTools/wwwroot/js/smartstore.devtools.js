const widgetZoneMenuId = '#wz-menu';
let widgetZones;
const zIndex = 1000;

export class DevTools {
    Res = {};

    /**
     * Initialize the DevTools widget functionality: Create the widget zone menu and set up event listeners.
     */
    initialize() {
        widgetZones = [];
        let wzMenu = $(widgetZoneMenuId);

        let wzMenuToggle = $('#wz-menu-toggle');
        wzMenuToggle.css('z-index', zIndex);

        $('#wz-toolbar [data-toggle="tooltip"]').tooltip({
            placement: 'top', // Testweise eine feste Platzierung setzen
            container: '#wz-toolbar'
        });

        // Add widget zone menu close button.
        wzMenu.on('click', '.wz-sidebar-close', (e) => {
            e.preventDefault();
            wzMenuToggle.click();
            return false;
        });

        // Add event listener to copy widget zone name to clipboard.
        wzMenu.on("click", ".copy-to-clipboard", (e) => {
            e.preventDefault();
            window.copyTextToClipboard($(e.currentTarget).data('value'));
            return false;
        });

        // Jump to the zone.
        wzMenu.on("click", ".wz-zone-pointer", function (e) {
            e.preventDefault();

            let wzName = $(this).text();
            let widetzones = document.querySelectorAll('span[title="' + wzName + '"]');

            if (widetzones) {
                widetzones.forEach((wz, index) => {
                    let wzPreview = $(wz);
                    let wzIsHidden = wzPreview.hasClass('d-none');

                    // If multiple widget zones with the same name exist, we scroll to the first one.
                    if (index == 0) {

                        if (wzIsHidden) {
                            // Must be visible to scroll to it.
                            wzPreview.removeClass('d-none');
                        }

                        // Scroll to widget zone.
                        wz.scrollIntoView({ behavior: "smooth", block: "center", inline: "center" });

                        wzPreview.addClass('wz-highlight');
                    }

                    setTimeout(() => {
                        wzPreview.removeClass('wz-highlight');
                    }, 2000);

                    if (wzIsHidden) {
                        wzPreview.addClass('d-none');
                    }
                });
            }
        });

        // Add toggle buttons to widget zone menu.
        wzMenu.on("click", ".wz-toggle", (e) => {
            e.preventDefault();

            let wzToggleButton = $(e.currentTarget);
            let isPersistentToggleButton = wzToggleButton.attr('data-persistent') !== undefined;

            let persistentToggleButton = wzMenu.find('.wz-toggle[data-persistent]');
            let tempToggle = wzMenu.find('.wz-toggle:not([data-persistent])').first();
            const showZoneClass = 'show-wz';

            let canToggleVisibility = persistentToggleButton.attr('data-persistent') === 'true';

            // TODO: (mw) (dt) Separate toggles into two different classes?

            if (isPersistentToggleButton) {
                canToggleVisibility = !canToggleVisibility;
                // TODO: (mw) (dt) Check why data.('persistent') doesn't work. Using attr temporarily.
                //wzToggleButton.data('persistent', canToggleVisibility);
                wzToggleButton.attr('data-persistent', canToggleVisibility);

                // Save state in a cookie if requested.
                document.cookie = '.Smart.WZVisibility=' + canToggleVisibility + '; path=/; max-age=31536000';

                // Set both buttons to the same state.
                if (canToggleVisibility) {
                    tempToggle.removeClass('disabled').addClass(showZoneClass);
                } else {
                    tempToggle.addClass('disabled').removeClass(showZoneClass);
                }
            }
            else
            {
                if (canToggleVisibility) {
                    wzToggleButton.toggleClass(showZoneClass);
                }
            }

            this.setVisibilityForAllZones(tempToggle.hasClass(showZoneClass));
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
            .append('<div class="wz-zone-pointer-container d-flex gap-2 py-1"><a href="#" class="wz-zone-pointer flex-grow-1 text-decoration-none text-truncate" title="' + zone.name + '">' + zone.name + '</a>' +
                '<a href="#" class="copy-to-clipboard text-secondary" data-value="' + zone.name + '" title="" data-toggle="tooltip" data-placement="top" data-original-title="' +
                this.Res['Common.CopyToClipboard'] + '"><i class="far fa-copy"></i><a></div>')
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
            $('#wz-toolbar .wz-visible').addClass('d-none');
            $('#wz-toolbar .wz-invisible').removeClass('d-none');
        }
        else
        {
            zonePreviews.addClass('d-none');
            $('#wz-toolbar .wz-visible').removeClass('d-none');
            $('#wz-toolbar .wz-invisible').addClass('d-none');
        }
    }
}
