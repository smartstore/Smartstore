(function ($, window, document, undefined) {

    // Select2: AccessPermissions, CustomerRoles, DeliveryTimes, Discounts, Stores
    function initSelect(el) {
        if ($.fn.select2 === undefined || $.fn.selectWrapper === undefined)
            return;

        if (!el.classList.contains("noskin")) {
            $(el).selectWrapper();
        }
    }

    // Datetime & Time
    function initDateTime(el) {
        var $el = $(el);
        $el.parent().datetimepicker({
            format: $el.data("format"),
            useCurrent: $el.data("use-current"),
            locale: moment.locale()
        });
    }

    // Html
    function initHtml(el) {
        var $el = $(el);

        summernote_image_upload_url = $el.data("summernote-image-upload-url");

        if (!$el.data("lazy")) {
            $el.summernote($.extend(true, {}, summernote_global_config, { lang: $el.data("lang") }));
        }
    }

    // Link
    function initLinkBuilder(el) {
        $(el).linkBuilder();
    }

    // Download
    function initDownload(el) {
        $(el).downloadEditor();
    }

    // RuleSets
    function initRuleSets(el) {
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

    // TODO: (mh) (core) This method must be used in EntityPicker script as a callback function.
    // > window.productPickerCallback.apply($("#control-id").get(0), [ids, selectedItems]);
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

        // TODO: (mh) (core) implement generic solution for this. Tokens can also be sent with the header in an Actionfilter or something like this.
        var token = $('input[name="__RequestVerificationToken"]').val();
        // Create download for selected MediaFile.
        $.ajax({
            async: true,
            cache: false,
            type: 'POST',
            url: $el.data("create-url"),
            data: {
                mediaFileId: file.id,
                entityId: $el.data("entity-id"),
                entityName: $el.data("entity-name"),
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (!response.success)
                    displayNotification("Error while trying to create a download for the selected file.", "error");
            }
        });
    }

    window.initializeEditControls = function (context) {
        var editControls = (context || document).getElementsByClassName("edit-control");

        Array.from(editControls).forEach(el => {
            var template = el.getAttribute("data-editor");
            switch (template) {
                case "select":
                    initSelect(el);
                    break;
                case "date-time":
                case "time":
                    initDateTime(el);
                    break;
                case "html":
                    initHtml(el);
                    break;
                case "link":
                    initLinkBuilder(el);
                    break;
                case "download":
                    initDownload(el);
                    break;
                case "rule-sets":
                    initRuleSets(el);
                    break;
            }
        });
    }
})(jQuery, this, document);

