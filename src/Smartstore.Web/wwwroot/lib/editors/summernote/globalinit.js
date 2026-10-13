let summernote_global_config;
let summernote_image_upload_url;

(function () {
    // Summernote accepts trusted HTML in its icon slots, including custom elements.
    const iconConcepts = {
        align: 'text-align-left',
        alignCenter: 'text-align-center',
        alignJustify: 'text-align-justify',
        alignLeft: 'text-align-left',
        alignRight: 'text-align-right',
        floatLeft: 'image-float-left',
        floatRight: 'image-float-right',
        floatNone: 'image-float-none',
        flipV: 'flip-v',
        flipH: 'flip-h',
        rowBelow: 'table-row-below',
        colBefore: 'table-column-before',
        colAfter: 'table-column-after',
        rowAbove: 'table-row-above',
        rowRemove: 'table-row-remove',
        colRemove: 'table-column-remove',
        indent: 'text-indent',
        outdent: 'text-outdent',
        arrowsAlt: 'maximize',
        arrowsAltCollapse: 'minimize',
        bold: 'bold',
        caret: 'chevron-down',
        circle: 'circle-outline',
        close: 'close',
        code: 'code',
        inlineCode: 'code',
        eraser: 'eraser',
        paintbrush: 'paint-brush',
        colorReset: 'droplet-off',
        colorPicker: 'dropper',
        font: 'font',
        frame: 'editor-frame',
        italic: 'italic',
        link: 'link',
        unlink: 'unlink',
        magic: 'text-style',
        paragraph: 'paragraph',
        menuCheck: 'check',
        minus: 'minus',
        orderedlist: 'list-ordered',
        pencil: 'pen',
        picture: 'image',
        question: 'question-circle',
        redo: 'redo',
        square: 'square-outline',
        strikethrough: 'strikethrough',
        subscript: 'subscript',
        superscript: 'superscript',
        table: 'table',
        textHeight: 'line-height',
        trash: 'trash',
        underline: 'underline',
        undo: 'undo',
        rollback: 'image-actual-size',
        unorderedlist: 'list-unordered',
        video: 'video',
        css: 'css-3',
        ellipsis: 'ellipsis',
        spellCheck: 'spell-check',
        gear: 'gears',
        fill: 'paint-bucket',
        face: 'smile',
        omega: 'special-character',
        layout: 'editor-layout',
        smiley: 'smile',
        ai: 'stars-tricolor',
    };
    const editorIcons = Object.fromEntries(Object.entries(iconConcepts).map(([key, concept]) =>
        [key, '<sm-icon name="' + concept + '" aria-hidden="true"></sm-icon>']));
    // Preserve the editor's original save icon.
    editorIcons.disk = '<svg viewBox="0 0 448 512" style="width: 18px; height: 18px;"><path class="note-duotone-secondary" d="M48 96l0 320c0 8.8 7.2 16 16 16l320 0c8.8 0 16-7.2 16-16l0-245.5c0-4.2-1.7-8.3-4.7-11.3L320.8 84.7c-.3-.3-.5-.5-.8-.8L320 184c0 13.3-10.7 24-24 24l-192 0c-13.3 0-24-10.7-24-24L80 80 64 80c-8.8 0-16 7.2-16 16zM288 320a64 64 0 1 1 -128 0 64 64 0 1 1 128 0z"/><path class="note-duotone-primary" d="M48 96l0 320c0 8.8 7.2 16 16 16l320 0c8.8 0 16-7.2 16-16l0-245.5c0-4.2-1.7-8.3-4.7-11.3l33.9-33.9c12 12 18.7 28.3 18.7 45.3L448 416c0 35.3-28.7 64-64 64L64 480c-35.3 0-64-28.7-64-64L0 96C0 60.7 28.7 32 64 32l245.5 0c17 0 33.3 6.7 45.3 18.7l74.5 74.5-33.9 33.9L320.8 84.7c-.3-.3-.5-.5-.8-.8L320 184c0 13.3-10.7 24-24 24l-192 0c-13.3 0-24-10.7-24-24L80 80 64 80c-8.8 0-16 7.2-16 16zm80-16l0 80 144 0 0-80L128 80zm32 240a64 64 0 1 1 128 0 64 64 0 1 1 -128 0z"/></svg>';

    const beautifyOpts = {
        indent_size: 2,
        indent_with_tabs: true,
        indent_char: " ",
        max_preserve_newlines: "2",
        preserve_newlines: true,
        keep_array_indentation: false,
        break_chained_methods: false,
        indent_scripts: "normal",
        brace_style: "collapse",
        space_before_conditional: true,
        unescape_strings: false,
        jslint_happy: false,
        end_with_newline: false,
        wrap_line_length: "140",
        indent_inner_html: true,
        comma_first: false,
        e4x: false,
        indent_empty_lines: false
    };

    summernote_global_config = {
        disableDragAndDrop: false,
        dialogsInBody: true,
        container: 'body',
        dialogsFade: true,
        height: 350,
        prettifyHtml: true,
        popatmouse: true,
        hideArrow: false,
        recordEveryKeystroke: false,
        followingToolbar: true,
        linkTargetBlank: false,
        // TODO: Turn on spellCheck again
        spellCheck: false,
        colorButton: {
            foreColor: '#424242',
            backColor: '#CEE7F7',
        },
        callbacks: {
            onBlurCodeview(code, e) {
                // Summernote does not update WYSIWYG content on codable blur,
                // only when switched back to editor
                $(this).val(code);
            },
            onFileBrowse(e, mediaType, deferred) {
                Smartstore.media.openFileManager({
                    el: e.target,
                    type: mediaType,
                    backdrop: false,
                    onSelect: (files) => {
                        if (!files.length) {
                            deferred.reject();
                        }
                        else {
                            deferred.resolve(files[0].url);
                        }
                    }
                });
            },
            onImageUpload(files) {
                if (summernote_image_upload_url) {
                    sendFile(files[0], this);
                }
            },
            onPrettifyHtml(html) {
                if (window.html_beautify) {
                    return window.html_beautify(html, beautifyOpts);
                }
                return html;
            }
        },
        icons: editorIcons,
        toolbar: [
            ['edit', ['undo', 'redo']],
            ['text', ['bold', 'italic', 'underline', 'color', 'moreFontStyles']],
            //['color', ['forecolor', 'backcolor']],
            //['font', ['fontname', 'fontsize']],
            ['para', ['ai', 'style', 'cssclass', 'ul', 'ol', 'paragraph', 'clear', 'cleaner']],
            ['insert', ['link', 'image', 'video', 'emoji', 'table', 'hr']],
            ['view', ['codeview', 'fullscreen', 'help']]
        ],
        popover: {
            image: [
                ['custom', ['imageAttributes', 'link', 'unlink', 'imageShapes']],
                ['imagesize', ['resizeFull', 'resizeHalf', 'resizeQuarter', 'resizeNone']],
                ['float', ['floatLeft', 'floatRight', 'floatNone']],
                ['remove', ['removeMedia']]
            ],
            link: [
                ['link', ['linkDialogShow', 'unlink']]
            ],
            table: [
                ['add', ['addRowDown', 'addRowUp', 'addColLeft', 'addColRight']],
                ['delete', ['deleteRow', 'deleteCol', 'deleteTable']],
                ['custom', ['tableStyles']]
            ],
             air: [
               ['color', ['color']],
               ['font', ['bold', 'underline', 'clear']],
               ['para', ['ul', 'paragraph']],
               ['table', ['table']],
               ['insert', ['link', 'picture']]
             ]
        },
        styleTags: [
            'p',
            'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
            'pre',
            { title: 'Blockquote', tag: 'blockquote', className: 'blockquote', value: 'blockquote' }
        ],
        imageAttributes: {
            icon: editorIcons.pencil,
            removeEmpty: true, // true = remove attributes | false = leave empty if present
            disableUpload: true // true = don't display Upload Options | Display Upload Options
        }
    };

    if (CodeMirror?.htmlSchema) {
        // Extend CodeMirror's HTML schema without replacing its HTML/Liquid hint helpers.
        let manifestPromise;
        function iconManifest() {
            const meta = document.querySelector('meta[property="sm:icons"]');
            if (!meta) return Promise.resolve(null);
            if (!manifestPromise) {
                const root = new URL(document.querySelector('meta[property="sm:root"]')?.content || './', document.baseURI);
                manifestPromise = fetch(new URL(meta.content, root), { credentials: 'same-origin' })
                    .then(response => response.ok ? response.json() : Promise.reject())
                    .catch(() => { manifestPromise = null; return null; });
            }
            return manifestPromise;
        }

        function iconContext(cm) {
            // The opening tag may span multiple lines. Content targets the storefront unless the tag explicitly selects another area.
            const text = cm.getValue(), cursor = cm.indexFromPos(cm.getCursor());
            const start = text.lastIndexOf('<', cursor), end = text.indexOf('>', cursor);
            const tag = text.slice(start, end < 0 ? text.length : end);
            const area = /\sarea\s*=\s*(["'])(.*?)\1/i.exec(tag)?.[2]
                || 'public';
            return { area, beforeCursor: text.slice(start, cursor) };
        }

        function iconNames(cm) {
            const { area } = iconContext(cm);
            return iconManifest().then(data => Object.keys(data?.areas?.[area] || {}).sort());
        }

        const booleans = ['true', 'false'];
        const presentation = {
            class: null,
            style: null,
            title: null,
            size: ['2xs', 'xs', 'sm', 'lg', 'xl', '2xl', ...Array.from({ length: 10 }, (_, i) => `${i + 1}x`)],
            'font-scale': null,
            fw: booleans,
            color: null,
            inverse: booleans,
            animation: ['spin', 'pulse', 'spin-pulse', 'beat', 'fade', 'throb', 'cylon', 'cylon-vertical', 'bounce', 'beat-fade', 'flip', 'shake'],
            'animation-duration': null,
            'animation-reverse': booleans,
            rotate: ['0', '90', '180', '270'],
            'flip-h': booleans,
            'flip-v': booleans,
            scale: null,
            'shift-x': null,
            'shift-y': null,
            'aria-label': null,
            'aria-labelledby': null,
            'aria-hidden': booleans,
            role: ['img', 'presentation', 'none']
        };
        CodeMirror.htmlSchema['sm-icon'] = {
            attrs: {
                ...presentation,
                name: iconNames,
                area: () => iconManifest().then(data => Object.keys(data?.areas || {})),
                lib: null,
                variant: null,
                'stroke-scale': null
            },
            children: []
        };
        CodeMirror.htmlSchema['sm-icon-stack'] = { attrs: { ...presentation }, children: ['sm-icon'] };

        const htmlHint = CodeMirror.hint.html;
        CodeMirror.registerHelper('hint', 'html', function (cm, options) {
            const result = htmlHint(cm, options);
            const { area, beforeCursor } = iconContext(cm);
            if (/^<sm-icon(?:-stack)?\s/i.test(beforeCursor) && result?.list) {
                result.list = result.list.map(text => {
                    if (typeof text !== 'string' || !Object.hasOwn(CodeMirror.htmlSchema['sm-icon'].attrs, text)
                        || ['class', 'style', 'title', 'role'].includes(text) || text.startsWith('aria-')) {
                        return text;
                    }

                    return {
                        text,
                        render(element) {
                            const label = document.createElement('strong');
                            label.textContent = text;
                            element.append(label);
                        }
                    };
                });
            }

            if (!/^<sm-icon\s/i.test(beforeCursor) || !/\sname\s*=\s*(?:["'][^"']*)?$/i.test(beforeCursor)) {
                return result;
            }

            return Promise.resolve(result).then(hints => {
                if (hints) {
                    hints.list = hints.list.map(text => ({
                        text,
                        render(element) {
                            const name = text.slice(1, -1);
                            const icon = document.createElement('sm-icon');
                            icon.setAttribute('name', name);
                            icon.setAttribute('area', area);
                            icon.setAttribute('aria-hidden', 'true');
                            icon.className = 'mr-2';
                            icon.style.cssText = 'width: 16px; height: 16px; flex-shrink: 0;';
                            element.classList.add('d-flex', 'align-items-center');
                            element.append(icon, document.createTextNode(name));
                        }
                    }));
                }
                return hints;
            });
        });
    }

    if (CodeMirror?.hint) {
        summernote_global_config.codemirror = {
            mode: "htmlmixed",
            theme: "eclipse",
            lineNumbers: true,
            lineWrapping: false,
            tabSize: 2,
            indentWithTabs: true,
            smartIndent: true,
            matchTags: true,
            matchBrackets: true,
            autoCloseTags: true,
            autoCloseBrackets: true,
            styleActiveLine: true,
            hintOptions: {
                closeCharacters: /[\s()\[\]{};:>,.|%]/,
                completeSingle: false
            },
            extraKeys: {
                "'.'": CodeMirror.hint.completeAfter,
                "'<'": CodeMirror.hint.completeAfter,
                "'/'": CodeMirror.hint.completeIfAfterLt,
                "' '": CodeMirror.hint.completeIfAfterSpace,
                "'='": CodeMirror.hint.completeIfInTag,
                "Ctrl-Space": "autocomplete",
                "F11": function (cm) { cm.setOption("fullScreen", !cm.getOption("fullScreen")); },
                "Esc": function (cm) { if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false); }
            }
        };
    }

    function sendFile(file, editor, welEditable) {
        data = new FormData();
        data.append("file", file);
        data.append("a", "UPLOAD");
        data.append("d", "file");
        data.append("ext", true);

        $.ajax({
            data: data,
            type: "POST",
            url: summernote_image_upload_url,
            cache: false,
            contentType: false,
            processData: false,
            success: function (result) {
                if (result.Success) {
                    $(editor).summernote('insertImage', result.Url);
                }
                else {
                    EventBroker.publish("message", {
                        title: 'Image upload error',
                        text: result.Message,
                        type: 'error',
                        hide: false
                    });
                }
            }
        });
    }
})();

// Initialize summernote
$(function () {

    function saveHtml(url, html, deferred) {
        $.ajax({
            cache: false,
            method: "POST",
            contentType: 'application/json',
            url,
            data: JSON.stringify(html),
            success: (data) => {
                if (data.success)
                    deferred.resolve(html);
            }
        });     
    }

    // Custom events
    // Editor toggling
    $(document).on('click', '.note-editor-preview', function (e) {
        let div = $(this);
        let textarea = $(div.data("target"));
        let lang = div.data("lang");
        let root = div.parent();

        if (root.parent().is('.ai-provider-tool')) {
            // Remove button and dropdown menu from DOM
            root.nextAll().remove();
        }

        // Prepare options
        const options = $.extend(true, {}, summernote_global_config, { lang: lang, focus: true });

        // Handle save options
        const saveUrl = textarea.data('save-url');
        if (saveUrl) {
            // Prepend save button to the toolbar
            options.toolbar.unshift(['save', ['save']]);

            // Assign saveHtml function as Summernote onSave callback
            options.callbacks.onSave = (html, deferred) => {
                saveHtml(saveUrl, html, deferred);
            };
        }

        // Remove preview element
        div.remove();

        // Initialize Summernote
        textarea.removeClass('d-none').summernote(options);
    });

    // Fix "CodeMirror too wide" issue
    $(document).on('click', '.note-toolbar .btn-codeview', function (e) {
        var wrapper = $(this).closest('.adminData');
        if (wrapper.length) {
            wrapper.css('overflow-x', $(this).is('.active') ? 'auto' : '');
        }
    });
});