(function ($, window, document, undefined) {
    $(function () {
        // Images
        $(document).on('click', '.ai-provider-cnt .ai-image-creation', function (e) {
            e.preventDefault();

            var el = $(this);
            var cnt = el.closest(".ai-provider-cnt");

            // Set the title used for the modal dialog title. 
            // For direct openers the title is set else we take the text of the dropdown item.
            var title = this.hasAttribute("title") ? el.attr('title') : el.html();
            
            var params = {
                providerSystemname: cnt.data('provider-systemname'),
                targetProperty: cnt.data('target-property'),
                entityName: cnt.data('entity-name'),
                type: cnt.data('entity-type'),
                modalTitle: title,
                format: cnt.data('format'),
                mediaFolder: cnt.data('media-folder')
            };

            var url = getAIDialogUrl(cnt.data('modal-url'), params);

            openPopup({ large: false, flex: true, url: url });
        });

        // Text creation
        $(document).on('click', '.ai-text-creation', function (e) {
            e.preventDefault();

            var el = $(this);
            var cnt = el.closest(".ai-provider-cnt");
            var isRichtext = cnt.data('is-rich-text');

            if (!isRichtext) {
                // Get choosen provider cnt.
                cnt = el.closest(".cnt-ai-dialog-opener").find("button.active");
            }

            var params = {
                providerSystemName: cnt.data('provider-systemname'),
                entityName: cnt.data('entity-name'),
                Type: cnt.data('entity-type'),
                targetProperty: cnt.data('target-property')
            };

            if (!isRichtext) {
                Object.assign(params, {
                    optimizationCommand: el.data('command'),    // INFO: This is the optimization command of the clicked item.
                    changeParameter: el.text(),                 // INFO: This is important for change style and tone items. We must know how to change the present text. For command "change-style" e.g. professional, casual, friendly, etc.
                    displayWordLimit: cnt.data('display-word-limit'),
                    displayStyle: cnt.data('display-style'),
                    displayTone: cnt.data('display-tone'),
                    displayOptimizationOptions: cnt.data('display-optimization-options')
                });
            } else {
                Object.assign(params, {
                    entityId: cnt.data('entity-id'),
                    displayAdditionalContentOptions: cnt.data('display-additional-content-options'),
                    displayLinkOptions: cnt.data('display-link-options'),
                    displayImageOptions: cnt.data('display-image-options'),
                    displayStructureOptions: cnt.data('display-structure-options')
                });
            }

            var url = getAIDialogUrl(cnt.data('modal-url'), params);

            openPopup({ large: isRichtext, flex: true, url: url });
        });

        // Prevent dropdown from closing when a provider is choosen.
        $(document).on("click", ".btn-ai-provider-chooser", function (e) {
            e.stopPropagation();

            var el = $(this);

            // Swap active class of button group
            var btnGroup = el.closest('.btn-group');
            btnGroup.find('button').removeClass('active');
            el.addClass('active');

            return false;
        });

        // Translation
        $(document).on('click', '.ai-provider-cnt .ai-translation', function (e) {
            e.preventDefault();

            var el = $(this);
            var cnt = el.closest(".ai-provider-cnt");

            var params = {
                providerSystemname: cnt.data('provider-systemname'),
                targetProperty: cnt.data('target-property'),
                ModalTitle: cnt.data('modal-title')
            };

            var url = getAIDialogUrl(cnt.data('modal-url'), params);

            openPopup({ large: true, flex: true, url: url });
        });

        // Suggestion
        $(document).on('click', '.ai-provider-cnt .ai-suggestion', function (e) {
            e.preventDefault();

            var el = $(this);
            var cnt = el.closest(".ai-provider-cnt");

            var params = {
                providerSystemname: cnt.data('provider-systemname'),
                targetProperty: cnt.data('target-property'),
                type: cnt.data('entity-type'),
                mandatoryEntityFields: cnt.data('mandatory-entity-fields')
            };

            var url = getAIDialogUrl(cnt.data('modal-url'), params);

            openPopup({ large: false, flex: true, url: url });
        });

        // Set a class to apply margin if the dialog opener contains a textarea with scrollbar.
        $('.cnt-ai-dialog-opener').each(function () {
            var textarea = $(this).find('textarea');
            if (textarea.length > 0 && textarea[0].scrollHeight > textarea.innerHeight() && textarea.innerHeight() > 0) {
                $(this).addClass('has-scrollbar');
            }

            var summernote = $(this).find('.note-editor-preview');
            if (summernote.length > 0 && summernote[0].scrollHeight > summernote.innerHeight() && summernote.innerHeight() > 0) {
                $(this).addClass('has-scrollbar');
            }

            // TODO: On summernote init shift ai-opener below toolbar.
        });
    });

    function getAIDialogUrl(baseUrl, params) {
        var queryString = Object.entries(params).map(([key, value]) => {
            return encodeURIComponent(key) + "=" + encodeURIComponent(value);
        }).join("&");

        return baseUrl + (baseUrl.includes('?') ? '&' : '?') + queryString;
    }
})(jQuery, this, document);