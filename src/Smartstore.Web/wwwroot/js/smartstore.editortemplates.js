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
        "color": function (el) { $(el).colorpicker() },
        // ColorPalette
        "color-palette": function (el) { $(el).colorPalette() },
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
            const group = el.parentElement;
            if (Smartstore.DateTimePicker.getInstance(group)) return;
            const $el = $(el);
            const picker = new Smartstore.DateTimePicker(group, {
                format: $el.data("format"),
                useCurrent: $el.data("use-current") ?? true,
                locale: moment.locale(),
                keepOpen: false
            });
            group.addEventListener('smartdatetimepicker:change', event => {
                $(group).trigger({ type: 'change.datetimepicker', ...event.detail });
            }, { signal: picker.events.signal });
        },
        // Html
        "html": function (el) {
            var $el = $(el);

            summernote_image_upload_url = $el.data("summernote-image-upload-url");

            if (!$el.data("lazy")) {
                $el.find("> .summernote-editor").summernote($.extend(true, {}, summernote_global_config, { lang: $el.data("lang") }));
            }
        }
    };
    editorMap["time"] = editorMap["date-time"];

    $.fn.colorPalette = function () {
        return this.each(function () {
            const $editor = $(this);

            if ($editor.data("colorPalette")) {
                return;
            }

            const maxColors = Math.max(1, parseInt($editor.data("max-colors")) || 1);
            const additionalName = $editor.data("additional-name");
            const $addContainer = $editor.children(".color-palette-add-container");
            const $addButton = $addContainer.children(".color-palette-add");
            const template = $editor.children(".color-palette-template").html();

            function updateInputTitle(input) {
                input.title = input.value;
            }

            function update() {
                const $items = $editor.children(".color-palette-item");
                const $inputs = $editor.find('.colorpicker-component > input');

                $items.each((index, item) => {
                    const $input = $(item).find("input").first();
                    $input.attr("name", `${additionalName}[${index}]`);
                    $input.attr("id", `${$editor.attr("id")}_AdditionalColors_${index}_`);
                });

                // Avoid having the color value appear cut off.
                $inputs.addClass("p-2");
                $inputs.each((_, input) => updateInputTitle(input));
                $addContainer.toggleClass("d-none", $items.length >= maxColors - 1);
            }

            $addButton.on("click", () => {
                const index = $editor.children(".color-palette-item").length;
                if (index >= maxColors - 1) {
                    return;
                }

                const $item = $(template.replace(/__index__/g, index));
                $addContainer.before($item);
                initializeEditControls($item);
                update();
                $item.find("input").trigger("focus");
            });

            $editor.on("click", ".color-palette-remove", (e) => {
                const $item = $(e.currentTarget).closest(".color-palette-item");
                const colorpicker = $item.find('[data-editor="color"]').data("colorpicker");

                colorpicker?.destroy();
                $item.remove();
                update();
            });

            $editor.on("input change", '.colorpicker-component > input', (e) => updateInputTitle(e.currentTarget));

            $editor.data("colorPalette", true);
            update();
        });
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
        if (context instanceof jQuery) {
            context = context[0];
        }
        else if (!(context instanceof HTMLElement)) {
            context = null;
        }

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

