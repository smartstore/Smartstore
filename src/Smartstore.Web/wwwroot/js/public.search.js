(function ($, window, document, undefined) {

    //
    // Instant Search
    // ==========================================================

    $(function () {
        $('.instasearch-form').each(function () {
            let form = $(this),
                box = form.find('.instasearch-term'),
                addon = form.find('.instasearch-addon'),
                spinner = form.find('.instasearch-progress'),
                clearer = form.find('.instasearch-clear');

            if (box.length == 0 || box.data('instasearch') === false)
                return;

            // INFO: if 'data-target' is specified, then JSON response is expected
            // and the 'content' property contains the HTML (convention).
            const target = $(box.data('target'));

            let drop = form.find('.instasearch-drop'),
                logo = $('.shop-logo'),
                dropBody = drop.find('.instasearch-drop-body'),
                minLength = box.data("minlength"),
                url = box.data("url"),
                keyNav = null;

            clearer.on('click', function (e) {
                box[0].value = '';
                doSearch('');
                box[0].focus();
            });

            box.parent().on('click', function (e) {
                e.stopPropagation();
            });

            box.on('focus', function (e) {
                expandBox();
            });

            box.on('keydown', function (e) {
                if (e.which === 13 /* Enter */) {
                    if (target.length || (keyNav && dropBody.find('.key-hover').length > 0)) {
                        // Do not post form when key navigation is in progress
                        e.preventDefault();
                    }
                }
            });

            box.on('keyup', function (e) {
                if (e.which === 27 /* ESC */) {
                    closeDrop();
                }
            });

            $(document).on('mousedown', function (e) {
                // Close drop on outside click
                if ($(e.target).closest('.instasearch-form').length > 0)
                    return;

                shrinkBox();
                closeDrop();
            });

            var debouncedInput = _.debounce(function (e) {
                doSearch(box.val());
            }, 180, false);
            box.on('input propertychange paste', debouncedInput);

            // Sometimes a previous search term finishes request AFTER
            // a subsequent one. We need to skip rendering in this case.
            var lastTerm;

            function doSearch(term) {
                if (term.length < minLength) {
                    closeDrop();
                    dropBody.html('');
                    return;
                }

                if (spinner.length === 0) {
                    spinner = createCircularSpinner(20)
                        .addClass('instasearch-progress')
                        .prependTo(addon);
                }
                // Don't show spinner when result is coming fast (< 100 ms.)
                var spinnerTimeout = setTimeout(function () {
                    spinner.addClass('active');
                    box.addClass('busy');
                }, 100)

                // Save last entered term in a global variable.
                lastTerm = term;

                $.ajax({
                    dataType: target.length ? 'json' : 'html',
                    url: url,
                    data: { q: term },
                    type: 'POST',
                    //cache: true,
                    success: function (response, status, req) {
                        if (lastTerm !== term) {
                            // This is the result of a previous term. Get out!
                            return;
                        }

                        if (target.length) {
                            target.html(response.content);
                            target.trigger('updated');
                            applyCommonPlugins(target);
                        }
                        else if (!response || response.length === 0) {
                            closeDrop();
                            dropBody.html('');
                        }
                        else {
                            var markup = $(response);
                            var isMultiCol = markup.hasClass('instasearch-row');
                            drop.toggleClass('w-100', !isMultiCol);
                            dropBody.html(markup);
                            openDrop();
                        }
                    },
                    error: function () {
                        closeDrop();
                        dropBody.html('');
                    },
                    complete: function () {
                        clearTimeout(spinnerTimeout); 
                        spinner.removeClass('active');
                        box.removeClass('busy');
                    }
                });
            }

            function expandBox() {
                box.addClass('active');
                if (box.data('origin') === 'Search/Search') {
                    var logoWidth = logo.width();
                    $('body').addClass('search-focused');
                    logo.css('margin-inline-start', (logoWidth * -1) + 'px');

                    if (dropBody.text().length > 0) {
                        logo.one(Prefixer.event.transitionEnd, function () {
                            openDrop();
                        });
                    }
                }
            }

            function shrinkBox() {
                box.removeClass('active');
                if (box.data('origin') === 'Search/Search') {
                    $('body').removeClass('search-focused');
                    logo.css('margin-inline-start', '');
                }
            }

            function openDrop() {
                form.addClass('open');
                if (!drop.hasClass('open')) {
                    drop.addClass('open');
                    beginKeyEvents();
                }
            }

            function closeDrop() {
                form.removeClass('open');
                drop.removeClass('open');
                endKeyEvents();
            }

            function beginKeyEvents() {
                if (keyNav)
                    return;

                // start listening to Down, Up and Enter keys

                dropBody.keyNav({
                    exclusiveKeyListener: false,
                    scrollToKeyHoverItem: false,
                    selectionItemSelector: ".instasearch-hit",
                    selectedItemHoverClass: "key-hover",
                    keyActions: [
                        { keyCode: 13, action: "select" }, //enter
                        { keyCode: 38, action: "up" }, //up
                        { keyCode: 40, action: "down" }, //down
                    ]
                });

                keyNav = dropBody.data("keyNav");

                dropBody.on("keyNav.selected", function (e) {
                    // Triggered when user presses Enter after navigating to a hit with keyboard
                    var el = $(e.selectedElement);
                    var href = el.attr('href') || el.data('href');
                    if (href) {
                        closeDrop();
                        location.replace(href);
                    }
                });
            }

            function endKeyEvents() {
                if (keyNav) {
                    dropBody.off("keyNav.selected");
                    keyNav.destroy();
                    keyNav = null;
                }
            }

            form.on("submit", function (e) {
                if (!box.val()) {
                    // Shake the form on submit but no term has been entered
                    var frm = $(this);
                    var shakeOpts = { direction: "right", distance: 4, times: 2 };
                    frm.stop(true, true).effect("shake", shakeOpts, 400, function () {
                        box.trigger("focus").removeClass("placeholder")
                    });
                    return false;
                }

                return true;
            });
        });
    });


    //
    // Facets
    // ==========================================================

    $(function () {
        var widget = $('#faceted-search');
        if (widget.length === 0)
            return;

        //
        //	Handle facet widget filter events
        // =============================================
        (function () {
            // Handle checkboxes
            widget.on('change', ':input[type=checkbox].facet-control-native', facetControlClickHandler);

            // Handle radio buttons
            widget.on('click', ':input[type=radio].facet-control-native', facetControlClickHandler);

            function facetControlClickHandler(e) {
                let href = $(this).closest('[data-href]').data('href');
                if (href) {
                    setLocation(href);
                }
            }

            // Custom ranges (prices, custom numeric attributes etc.)
            widget.on('click', '.btn-custom-range', function (e) {
                var btn = $(this),
                    cnt = btn.closest('.facet-range-container'),
                    minVal = cnt.find('.facet-range-from').val(),
                    maxVal = cnt.find('.facet-range-to').val();

                let expr = minVal.replace(/[^\d\.\-]/g, '') + '~' + maxVal.replace(/[^\d\.\-]/g, '');

                let url = modifyUrl(null, btn.data('qname'), expr.length > 1 ? expr : null);
                setLocation(url);
            });

            // Validate custom range selection
            widget.on('change', 'select.facet-range-from, select.facet-range-to', function (e, recursive) {
                if (recursive)
                    return;

                let select = $(this),
                    isMin = select.hasClass('facet-range-from'),
                    otherSelect = select.closest('.facet-range-container').find('select.facet-range-' + (isMin ? 'to' : 'from')),
                    idx = select.find('option:selected').index(),
                    otherIdx = otherSelect.find('option:selected').index();

                function validateRangeControls() {
                    var newIdx = Math.min($('option', otherSelect).length - 1, Math.max(0, isMin ? idx + 1 : idx - 1));
                    if (newIdx == idx) {
                        newIdx = 0;
                    }

                    $('option:eq(' + newIdx + ')', otherSelect).prop('selected', true);
                    otherSelect.trigger('change', [true]);
                }

                if (idx > 0 && otherIdx > 0 && ((isMin && idx > otherIdx) || (!isMin && idx < otherIdx))) {
                    validateRangeControls();
                }
            });

            // Switch range value to upper or back.
            widget.on('change', 'select.facet-switch-range', function (e, recursive) {
                if (recursive)
                    return;

                let select = $(this),
                    selectedUrl = null,
                    toUpper = select.val() === 'upper',
                    qname = select.data('qname');

                // Update all url and input values.
                select.closest('.facet-group').find('.facet-item').each(function (index) {
                    var item = $(this),
                        url = item.attr('data-href'),
                        input = item.find('input.facet-control-native'),
                        val = input.val().replace('~', '');

                    if (toUpper) {
                        val = '~' + val;
                    }
                    url = modifyUrl(url, qname, val);

                    input.val(val);
                    item.attr('data-href', url);

                    if (input.is(':checked')) {
                        selectedUrl = url;
                    }
                });

                // Update location for selected filter.
                if (selectedUrl) {
                    setLocation(selectedUrl);
                }
            });
        })();


        //
        //	Handle local search
        // =============================================
        (function () {
            widget.on('input propertychange paste', '.facet-local-search-input', function (e) {
                let el = $(this);

                // Retrieve the input field text and reset the count to zero
                let filter = el.val(),
                    rg = new RegExp(filter, "i");

                // Loop through the facet items
                el.closest('.facet-body').find('.facet-item').each(function () {
                    let item = $(this);

                    // If the facet item does not contain the text phrase hide it
                    if (filter.length > 0 && item.text().search(rg) < 0) {
                        item.hide();
                    }
                    // Show the facet item if the phrase matches
                    else {
                        item.show();
                    }
                });
            });
        })();


        //
        //	Handle widget responsiveness (offcanvas)
        // =============================================
        (function () {
            var btn = $('.btn-toggle-filter-widget');
            if (btn.length === 0)
                return;

            var viewport = ResponsiveBootstrapToolkit;

            function collapseWidget(afterResize) {
                if (btn.data('offcanvas')) return;

                // create offcanvas wrapper
                let placement = viewport.is('>=md') ? 'start' : 'bottom';
                let offcanvas =
                    $(`<aside class="offcanvas offcanvas-${placement} offcanvas-shadow offcanvas-lg offcanvas-rounded" data-overlay="true">
                            <div class="offcanvas-header">
                                <h5 class="offcanvas-title"><i class="fa fa-sliders-h mr-2"></i><span>${btn.data("title")}</span></h5>
                                <button type="button" class="btn-close" data-dismiss="offcanvas"></button>
                            </div>
                            <div class="offcanvas-content offcanvas-scrollable"></div>
                       </aside>`).appendTo('body');

                // handle .offcanvas-closer click
                offcanvas.one('click', '.btn-close', function (e) {
                    offcanvas.offcanvas('hide');
                });

                // put widget into offcanvas wrapper
                widget.appendTo(offcanvas.find('> .offcanvas-content'));

                btn.data('offcanvas', offcanvas)
                    .attr('data-toggle', 'offcanvas')
                    .attr('data-disablescrolling', 'true')
                    .data('placement', { xs: "bottom", md: "start" })
                    .data('target', offcanvas);

                if (!afterResize) {
                    //// Collapse all groups on initial page load
                    //// TODO: (mc) Why did we do this? I don't like it anymore, so no auto-collapsing on mobile anymore (for now).
                    //widget.find('.facet-toggle:not(.collapsed)').addClass('collapsed');
                    //widget.find('.facet-body.show').removeClass('show');
                }
            }

            function restoreWidget() {
                if (!btn.data('offcanvas')) return;

                // move widget back to its origin
                let offcanvas = btn.data('offcanvas');
                widget.appendTo($('.faceted-search-container'));
                offcanvas.remove();

                btn.removeData('offcanvas')
                    .removeAttr('data-toggle')
                    .removeAttr('data-placement')
                    .removeAttr('data-disablescrolling')
                    .removeData('target');
            }

            function toggleOffCanvas(afterResize) {
                let breakpoint = '<lg';
                if (viewport.is(breakpoint)) {
                    collapseWidget(afterResize);
                }
                else {
                    restoreWidget();
                }
            }

            EventBroker.subscribe("page.resized", function (msg, viewport) {
                toggleOffCanvas(true);
            });

            _.delay(toggleOffCanvas, 10);
        })();
    });

})(jQuery, this, document);

