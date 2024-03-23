(function ($, window, document, undefined) {

    let editorMap = {
        // LinkBuilder
        "link": function (el) { $(el).linkBuilder() },
        // Download
        "download": function (el) { $(el).downloadEditor() },
        // NumberInput
        "number": function (el) { $(el).numberInput() },
        // RangeSlider
        "range": function (el) { $(el).rangeSlider() },
        // ColorBox
        "color": function (el) { $(el).colorpickerWrapper() },
        // Select2: AccessPermissions, CustomerRoles, DeliveryTimes, Discounts, Stores
        "select": function (el) {
            if ($.fn.select2 === undefined || $.fn.selectWrapper === undefined)
                return;

            if (!el.classList.contains("noskin") && !$(el).data("select2")) {
                $(el).selectWrapper();
            }
        },
        // Datetime & Time
        "date-time": function (el) {
            var $el = $(el);
            $el.parent().datetimepicker({
                format: $el.data("format"),
                useCurrent: $el.data("use-current"),
                locale: moment.locale(),
                keepOpen: false
            });
        },
        // Html
        "html": function (el) {
            var $el = $(el);

            summernote_image_upload_url = $el.data("summernote-image-upload-url");

            if (!$el.data("lazy")) {
                $el.find("> .summernote-editor").summernote($.extend(true, {}, summernote_global_config, { lang: $el.data("lang") }));
            }
        },

        // RuleSets
        "rule-sets": function (el) {
            $(el)
                .selectWrapper()
                .on('select2:selecting select2:unselecting', function (e) {
                    try {
                        // Prevent selection when a link has been clicked.
                        if ($(e.params.args.originalEvent.target).hasClass('prevent-selection')) {
                            var data = e.params.args.data;

                            if (data.id === '-1' && !_.isEmpty(data.url)) {
                                window.location = data.url;
                            }

                            e.preventDefault();
                            return false;
                        }
                    }
                    catch (e) {
                        console.error(e);
                    }
                });
        }
    };
    editorMap["time"] = editorMap["date-time"];

    // Replace datetimepicker internal _place() method with our own, because the original is really badly fucked up!
    // TODO: (core) Move $.fn.datetimepicker.Constructor.prototype._place() to applicable place when bundling is available.
    $.fn.datetimepicker.Constructor.prototype._place = function (e) {
        const self = e && e.data && e.data.picker || this,
            parent = document.body,
            component = (self.component && self.component.length ? self.component : self._element).get(0),
            position = component.getBoundingClientRect(),
            widget = self.widget.get(0),
            scrollTop = document.documentElement.scrollTop;
        let vpos = "bottom",
            hpos = "right";

        parent.append(widget);

        const widgetWidth = widget.offsetWidth;
        const widgetHeight = widget.offsetHeight;

        if (position.bottom + widgetHeight > window.innerHeight && position.top - widgetHeight > -2) {
            vpos = "top";
        }

        if (position.right - widgetWidth < 0) {
            hpos = "left";
        }

        if (vpos === "top") {
            widget.classList.add("top");
            widget.classList.remove("bottom");
        }
        else {
            widget.classList.add("bottom");
            widget.classList.remove("top");
        }

        if (hpos === "left") {
            widget.classList.add("float-left");
            widget.classList.remove("float-right");
        }
        else {
            widget.classList.add("float-right");
            widget.classList.remove("float-left");
        }

        // Default pos --> right/bottom
        var pos = {
            top: (vpos === "top" ? Math.max(0, scrollTop + position.top - widgetHeight - 8) : (scrollTop + position.bottom)) + "px",
            left: (hpos == "left" ? position.left : position.left + (position.width - widgetWidth)) + "px",
            bottom: "auto",
            right: "auto"
        };

        $.extend(widget.style, pos);
    };

    function initConfirmationDialogs(context) {
        // TODO: (mh) (core) Move initConfirmationDialogs() to applicable place later (e.g. globalinit, plugin factories or alike)
        var confirmations = (context || document).getElementsByClassName("confirmation-dialog");

        Array.from(confirmations).forEach(el => {
            var dialog = $(el);
            var submitButton = $("#" + dialog.data("submit-button-id"));
            var acceptButton = dialog.find(".btn-accept");

            submitButton.on("click", e => {
                e.preventDefault();
                dialog.modal("show");
            });

            acceptButton.on("click", e => {
                e.preventDefault();

                const url = dialog.data("action-url");

                if (acceptButton.data("commit-action") != 'delete') {
                    var form = submitButton.closest("form");

                    if (!_.isEmpty(url)) {
                        form.attr("action", url);
                    }

                    if (submitButton.val()) {
                        $("<input />")
                            .attr("type", "hidden")
                            .attr("name", submitButton.attr("name"))
                            .attr("value", submitButton.val())
                            .appendTo(form);
                    }

                    form.trigger("submit");
                }
                else {
                    $({}).postData({
                        url,
                        data: { id: acceptButton.data("commit-id") }
                    });
                }

                dialog.hide();
            });
        });
    }

    window.productPickerCallback = function (ids, selectedItems) {
        var $el = $(this);

        if ($el.length == 0)
            return;

        var cnt = $el.closest(".link-builder");
        var val = 'product:' + ids[0];
        var qs = cnt.find(".query-string").val() || '';

        while (qs.startsWith('?')) {
            qs = qs.substring(1);
        }

        if (!_.isEmpty(qs)) {
            val = val + '?' + qs;
        }

        // Ctrl
        $('#' + $el.data("field-id")).val(val).trigger("change");
        cnt.find(".product-picker-input").val(selectedItems[0].name);

        return true;
    }

    // TODO: (mh) (core) This method must be used in FileUploader script as a callback function.
    // > window.onMediaSelected.apply($("#control-id").get(0), [file]);
    window.onMediaSelected = function (file) {
        var $el = $(this);

        if ($el.length == 0)
            return;

        // Create download for selected MediaFile.
        $.ajax({
            async: true,
            cache: false,
            type: 'POST',
            url: $el.data("create-url"),
            data: {
                mediaFileId: file.id,
                entityId: $el.data("entity-id"),
                entityName: $el.data("entity-name")
            },
            success: function (response) {
                if (!response.success)
                    displayNotification("Error while trying to create a download for the selected file.", "error");
            }
        });
    }

    window.initializeEditControls = function (context) {
        context = context?.length ? context.get(0) : context;
        var editControls = (context || document).getElementsByClassName("edit-control");

        Array.from(editControls).forEach(el => {
            var template = el.getAttribute("data-editor");
            if (template) {
                var initializer = editorMap[template];
                if (_.isFunction(initializer)) {
                    initializer(el);
                }
                else {
                    EventBroker.publish("editcontrol.initializing", el);
                }
            }
        });
    };

    // TODO: (mh) (core) Move to globalinit later.
    $(function () {
        initializeEditControls();
        initConfirmationDialogs();
    });
})(jQuery, this, document);

