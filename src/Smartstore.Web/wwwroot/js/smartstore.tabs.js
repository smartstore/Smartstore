; (function ($, window, document, undefined) {

    $(function () {

        // Tab strip smart auto selection
        // ------------------------------------------------------

        $(document).on('shown.bs.tab', '.tabs-autoselect ul.nav a[data-toggle=tab]', function (e) {
            let tab = $(e.target),
                strip = tab.closest('.tabbable'),
                href = strip.data("tabselector-href"),
                hash = tab.attr("href");

            if (hash)
                hash = hash.replace(/#/, "");

            if (href) {
                $.ajax({
                    type: "POST",
                    url: href,
                    async: true,
                    data: { navId: strip.attr('id'), tabId: hash, path: location.pathname + location.search },
                    global: false
                });
            }
        });


        // AJAX tabs
        // ------------------------------------------------------

        $(document).on('show.bs.tab', '.nav a[data-ajax-url]', function (e) {
            let newTab = $(e.target),
                tabbable = newTab.closest('.tabbable'),
                pane = tabbable.find(newTab.attr("href")),
                url = newTab.data('ajax-url');

            if (newTab.data("loaded") || !url)
                return;

            $.ajax({
                cache: false,
                type: "GET",
                async: true,
                global: false,
                url: url,
                beforeSend: function (xhr) {
                    pane.html($("<div class='text-center mt-6'></div>").append(createCircularSpinner(48, true, 2)));
                    getFunction(tabbable.data("ajax-onbegin"), ["tab", "pane", "xhr"]).apply(this, [newTab, pane, xhr]);
                },
                success: function (data, status, xhr) {
                    pane.html(data);
                    getFunction(tabbable.data("ajax-onsuccess"), ["tab", "pane", "data", "status", "xhr"]).apply(this, [newTab, pane, data, status, xhr]);
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    pane.html('<div class="text-danger">Error while loading resource: ' + thrownError + '</div>');
                    getFunction(tabbable.data("ajax-onfailure"), ["tab", "pane", "xhr", "ajaxOptions", "thrownError"]).apply(this, [newTab, pane, xhr, ajaxOptions, thrownError]);
                },
                complete: function (xhr, status) {
                    newTab.data("loaded", true);
                    var tabName = newTab.data('tab-name') || newTab.attr("href").replace(/#/, "");
                    tabbable.append('<input type="hidden" class="loaded-tab-name" name="LoadedTabs" value="' + tabName + '" />');

                    getFunction(tabbable.data("ajax-oncomplete"), ["tab", "pane", "xhr", "status"]).apply(this, [newTab, pane, xhr, status]);
                }
            });
        });


        // Adaptive tabs
        // ------------------------------------------------------

        $('.tab-adaptive > a.nav-link').each(function (i, el) {
            let $el = $(el);
            if ($el.is('.active')) {
                initAdaptivePane(el);
            }

            $el.on('shown.bs.tab', function (e) {
                initAdaptivePane(e.target);
            });

            $el.on('hidden.bs.tab', function (e) {
                let currentTab = $(e.target);
                let newTab = $(e.relatedTarget);

                // Restore current tab height to what it was before
                currentTab.css('height', '');

                if (!newTab.is('.tab-adaptive')) {
                    let tabContent = newTab.closest('.tabbable').find('.tab-content');

                    // Restore .tab-content margin-bottom to what it was before
                    // if the new tab is NOT adaptive.
                    tabContent.css('margin-bottom', '');
                }
            });
        });

        function initAdaptivePane(el) {
            let activeTab = $(el),
                tabbable = activeTab.closest('.tabbable'),
                pane = tabbable.find(activeTab.attr("href")),
                contentPaddingBottom = parseFloat($('#content').css('padding-bottom'));

            // Set .tab-content wrapper's margin-bottom to 0.
            pane.parent().css('margin-bottom', '0');

            function adaptPaneHeight() {
                var rect = pane[0].getBoundingClientRect();
                var viewportHeight = document.documentElement.clientHeight;
                pane.css('height', (viewportHeight - rect.top - contentPaddingBottom) + 'px');
            }

            if (!pane.data('is-height-adapted')) {
                $(window).on('resize', function () {
                    if (pane.is(':visible')) {
                        adaptPaneHeight();
                    }
                });
            }

            pane.data('is-height-adapted', true);

            adaptPaneHeight();
        }

        function getFunction(code, argNames) {
            var fn = window, parts = (code || "").split(".");
            while (fn && parts.length) {
                fn = fn[parts.shift()];
            }
            if (typeof (fn) === "function") {
                return fn;
            }
            argNames.push(code);
            return Function.constructor.apply(null, argNames);
        }

    });
})(jQuery, this, document);

