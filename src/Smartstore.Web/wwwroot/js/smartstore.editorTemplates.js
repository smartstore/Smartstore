var _editorTemplateFactories = [

    // TODO: (mh) (core) Move this file to appropriate place after bundling is available.

    // Select2: AccessPermissions, CustomerRoles, DeliveryTimes, Discounts, Stores
    function (ctx) {
        if ($.fn.select2 === undefined || $.fn.selectWrapper === undefined)
            return;

        ctx.find("select:not(.noskin), input:hidden[data-select]").selectWrapper();
    },

    // Datetime & Time
    function (ctx) {
        var cntDatetime = ctx.find(".datetimepicker-group");
        if (!cntDatetime)
            return;

        cntDatetime.each(function (i, el) {
            var $el = $(this);
            var ctrl = $el.find(".datetimepicker-input");
            if (!ctrl)
                return;

            $el.datetimepicker({ format: ctrl.data("format"), useCurrent: ctrl.data("use-current"), locale: moment.locale() });
        });
    },

    // Html
    function (ctx) {
        var cntEditor = ctx.find(".html-editor-root");
        if (!cntEditor)
            return;
        // TODO: (mh) (core) lazy check is missing
        summernote_image_upload_url = cntEditor.data("summernote-image-upload-url");

        cntEditor.each(function (i, el) {
            var $el = $(this);
            var ctrl = $el.find(".summernote-editor");
            if (!ctrl)
                return;

            ctrl.summernote($.extend(true, {}, summernote_global_config, { lang: $el.data("lang") }));
        });
    },
    
    // Link
    function (ctx) {
        var cntLinkBuilder = ctx.find(".link-builder");
        if (!cntLinkBuilder)
            return;

        cntEditor.each(function (i, el) {
            var $el = $(this);
            $el.linkBuilder();

            var productPickerCallback = $el.data("product-picker-callback");
            if (productPickerCallback) {

                // TODO: (mh) (core) Test this.
                // TODO: (mh) (core) This method needs to be centralized
                window[productPickerCallback] = function (ids, selectedItems) {
                    // Cnt
                    var cnt = $el;
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
            }
        });
    },

    // Download
    function (ctx) {
        var cntDownload = ctx.find(".download-editor");
        if (!cntDownload)
            return;

        cntDownload.each(function (i, el) {
            var $el = $(this);
            $el.downloadEditor();

            var onMediaSelected = $el.data("media-selected-callback");
            if (onMediaSelected) {

                // TODO: (mh) (core) This method needs to be centralized
                window[onMediaSelected] = function (file) {
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
            }
        });
    },

    // RuleSets
    function (ctx) {
        var cntRuleSets = ctx.find(".rule-sets");
        if (!cntRuleSets)
            return;

        cntRuleSets.each(function (i, el) {
            $(this)
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
        });
    }

    // INFO: (mh) (core) Liquid & ModelTree were spared as they will probably never be used in data grid & also ModelTree must be reimplemented first.
];

Smartstore.EditorTemplates = {
    Apply: (function (context) {
        $.each(_editorTemplateFactories, function (i, val) {
            val.call(this, $(context));
        });
    })
};