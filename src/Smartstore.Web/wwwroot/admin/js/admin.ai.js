(function ($, window, document, undefined) {
    $(function () {
        // Images
        $(document).on('click', '.ai-provider-tool .ai-image-composer', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");

            // Set the title used for the modal dialog title. 
            // For direct openers the title is set else we take the text of the dropdown item.
            let title = this.getAttribute("title") || el.html();
            
            let params = {
                providerSystemname: tool.data('provider-systemname'),
                targetProperty: tool.data('target-property'),
                entityName: tool.data('entity-name'),
                type: tool.data('entity-type'),
                modalTitle: title,
                format: tool.data('format'),
                mediaFolder: tool.data('media-folder')
            };

            openDialog(tool, params, false);
        });

        // Text creation
        $(document).on('click', '.ai-text-composer', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");
            let isRichText = tool.data('is-rich-text');

            if (!isRichText) {
                // Get chosen provider tool.
                tool = el.closest(".ai-dialog-opener-root").find("button.active");
            }

            let params = {
                providerSystemName: tool.data('provider-systemname'),
                entityName: tool.data('entity-name'),
                Type: tool.data('entity-type'),
                targetProperty: tool.data('target-property')
            };

            if (!isRichText) {
                Object.assign(params, {
                    // INFO: This is the optimization command of the clicked item.
                    optimizationCommand: el.data('command'),
                    // INFO: This is important for change style and tone items. We must know how to change the present text. 
                    // For command "change-style" e.g.professional, casual, friendly, etc.
                    changeParameter: el.text(),
                    displayWordLimit: tool.data('display-word-limit'),
                    displayStyle: tool.data('display-style'),
                    displayTone: tool.data('display-tone'),
                    displayOptimizationOptions: tool.data('display-optimization-options')
                });
            } else {
                Object.assign(params, {
                    entityId: tool.data('entity-id'),
                    displayAdditionalContentOptions: tool.data('display-additional-content-options'),
                    displayLinkOptions: tool.data('display-link-options'),
                    displayImageOptions: tool.data('display-image-options'),
                    displayStructureOptions: tool.data('display-structure-options')
                });
            }

            openDialog(tool, params, isRichText);
        });

        // Prevent dropdown from closing when a provider is choosen.
        $(document).on("click", ".btn-ai-provider-chooser", function (e) {
            e.stopPropagation();

            let el = $(this);

            // Swap active class of button group
            let btnGroup = el.closest('.btn-group');
            btnGroup.find('button').removeClass('active');
            el.addClass('active');

            return false;
        });

        // Translation
        $(document).on('click', '.ai-provider-tool .ai-translator', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");

            let params = {
                providerSystemname: tool.data('provider-systemname'),
                targetProperty: tool.data('target-property'),
                ModalTitle: tool.data('modal-title')
            };

            openDialog(tool, params, true);
        });

        // Suggestion
        $(document).on('click', '.ai-provider-tool .ai-suggestion', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");

            let params = {
                providerSystemname: tool.data('provider-systemname'),
                targetProperty: tool.data('target-property'),
                type: tool.data('entity-type'),
                mandatoryEntityFields: tool.data('mandatory-entity-fields')
            };

            openDialog(tool, params, false);
        });

        // Set a class to apply margin if the dialog opener contains a textarea with scrollbar.
        $('.ai-dialog-opener-root').each(function () {
            const root = $(this);
            const localeEditor = root.parent();

            if (localeEditor.hasClass('locale-editor')) {
                // Removing translator menu items that have no according input element in the localized editor.
                root.find('.ai-translator-menu .ai-provider-tool').each(function () {
                    const tool = $(this);
                    const propName = tool.data('target-property');
                    if (!_.isEmpty(propName) && localeEditor.find('#' + propName).length == 0) {
                        tool[0].remove();
                    }
                });
                return;
            }

            let textarea = root.find('> textarea');
            let innerHeight = textarea.innerHeight();
            if (textarea.length && innerHeight && textarea[0].scrollHeight > innerHeight) {
                root.addClass('has-scrollbar');
            }

            let summernote = root.find('.note-editor-preview');
            innerHeight = summernote.innerHeight();
            if (summernote.length && innerHeight && summernote[0].scrollHeight > innerHeight) {
                root.addClass('has-scrollbar');
            }

            // TODO: On summernote init shift ai-opener below toolbar.
        });
    });

    function openDialog(opener, params, large) {
        openPopup({
            url: getDialogUrl(opener.data('modal-url'), params),
            large: large,
            flex: true,
            backdrop: 'static',
            scrollable: false
        });
    }

    function getDialogUrl(baseUrl, params) {
        let queryString = _.map(params, (value, key) => {
            return encodeURIComponent(key) + "=" + encodeURIComponent(value);
        }).join("&");

        return baseUrl + (baseUrl.includes('?') ? '&' : '?') + queryString;
    }
})(jQuery, this, document);