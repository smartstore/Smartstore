Smartstore.DevTools = (function () {

    let _widgetZoneMenuId = '#devtools-widget-zone-menu';
    let _widgetzones;
    let _zIndex = 1000;
    
    return {
        // Initialize the DevTools widget functionality: Create the widget zone menu and set up event listeners.
        init: function () {
            _widgetzones = [];
            _duplicateCounter = [];

            $(function () {
                createSideMenu(_widgetZoneMenuId, _zIndex);

                // Jump to the zone.
                $(_widgetZoneMenuId).on("click", ".zone-pointer", function (e) {
                    e.preventDefault();

                    let wzName = $(this).text();
                    let widetzones = document.querySelectorAll('span[title="' + wzName + '"]');

                    if (widetzones) {
                        widetzones.forEach((wz, index) => {
                            let wzPreview = $(wz);
                            wzPreview.addClass('widget-zone-highlight');

                            // If multiple widget zones with the same name exist, we scroll to the first one.
                            if (index == 0) {
                                let wZMenu = $(_widgetZoneMenuId);

                                // Make menu see-through, so the user can see covered widget zones.
                                wZMenu.addClass('see-through');

                                // Scroll to widget zone.
                                wz.scrollIntoView({ behavior: "smooth", block: "center", inline: "center" });
                                
                                setTimeout(function () {
                                    wZMenu.removeClass('see-through');
                                }, 2000);
                            }

                            setTimeout(function () {
                                wzPreview.removeClass('widget-zone-highlight');
                            }, 2000);
                        });
                    }
                });
                
                // Add toggle buttons to widget zone menu.
                $(_widgetZoneMenuId).on("click", ".persistent-toggle", function (e) {
                    Smartstore.DevTools.toggleAllZones(true);
                });
                $(_widgetZoneMenuId).on("click", ".temporary-toggle", function (e) {
                    Smartstore.DevTools.toggleAllZones();
                });

                // Add event listener to copy widget zone name to clipboard.
                $(_widgetZoneMenuId).on("click", ".copy-to-clipboard", function (e) {
                    e.preventDefault();
                    window.copyTextToClipboard($(e.currentTarget).data('value'));
                    return false;
                });

                // When the user presses "Strg + Alt + B" the widget zones will be toggled.
                $(document).on("keydown", function (e) {
                    if (e.ctrlKey && e.altKey && e.key === 'b') {
                        Smartstore.DevTools.toggleAllZones();
                    }
                });
            });
        },
        // Register a widget zone and add it to the zone list.
        registerWidgetZone: function (widgetZone) {
            let duplicate = false;
            _widgetzones.forEach(wz => {
                if (wz.name === widgetZone.name) {
                    duplicate = true;
                    return false;
                }
            });

            // If the widget zone is already registered, we skip it.
            if (duplicate) { return; }
            
            _widgetzones.push(widgetZone);

            let areaName = Smartstore.DevTools.getWidgetZoneArea(widgetZone.name);
            
            // Place the widget zone in the correct area and make sure the area is visible.
            $(_widgetZoneMenuId + ' .zone-area[data-area="' + areaName + '"]')
                .append('<div class="d-flex p-2 gap-2"><a href="#" class="zone-pointer flex-grow-1 text-light text-decoration-none text-break">' + widgetZone.name
                + '</a><a href="#" class="copy-to-clipboard text-secondary" data-value="' + widgetZone.name + '"><i class="fa-solid fa-copy"></i><a></div>')
                .removeClass('d-none');
        },
        // Returns the area the widget zone should be placed in, using .zone-area[data-zones="..."].
        getWidgetZoneArea: function (widgetZoneName) {
            let areas = $(_widgetZoneMenuId + ' .zone-area');
            let area = areas.filter(function () {
                return $(this).data('zones').split(' ').includes(widgetZoneName);
            });

            // If no area was found, use the last one (custom).
            if (area.length === 0) {
                // Check if the widget zone has a meta preview tag.
                let wz = _widgetzones.find(wz => wz.name === widgetZoneName);
                if (wz.previewTagName == 'meta') {
                    return 'Meta';
                }

                area = areas.last();
            }

            return area.data('area');
        },
        // Toggles the visibility of all widget zones. If saveInCookie is true, the state will be saved in a cookie.
        toggleAllZones: function (saveInCookie = false) {
            $(document).find('.wz-preview').toggleClass('d-none');

            // Save state in a cookie if requested.
            if (saveInCookie) {
                let wzState = $(document).find('.wz-preview').hasClass('d-none') ? 'hidden' : 'visible';
                document.cookie = '.Smart.WZVisibility=' + wzState + '; path=/; max-age=31536000';
            }
        }
    };
})();

// This function turns an element into a sidebar menu.
window.createSideMenu = function (selector, zIndex) {
    let menuWidth = '250'; // in px

    let menu = $(selector);

    menu.addClass("sidebar-menu position-fixed top-0 p-1 d-flex flex-column justify-content-center gap-1 bg-dark overflow-hidden")
        .removeClass("d-none")
        .width(menuWidth)
        .css({
            'height': '100%',
            'left': '-' + menuWidth * 1.2 + 'px',
            'z-index': zIndex + 1,
            'transition': '0.5s'
        });
    
    // Add close button to menu.
    menu.prepend('<button class="sidebar-close btn btn-dark btn-outline-light"><i class="fa fa-times"></i></button>')
        .find(".sidebar-close").on("click", function (e) {
            menu.css('left', '-' + menuWidth * 1.2 + 'px');
        });

    // Add toggle button outside the menu.
    menu.after('<button class="sidebar-menu-toggle position-fixed top-0 start-0 m-2 btn btn-dark text-white"><i class="fa fa-bars"></i></button>')
        .next().on("click", function (e) {
            menu.css('left', 0);
        }).css('z-index', zIndex);
};

Smartstore.DevTools.init();