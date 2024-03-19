/*
*  Project: Smartstore entity picker
*  Author: Marcus Gesing, SmartStore AG
*/

; (function ($, window, document, undefined) {

    var methods = {
        loadDialog: function (options) {
            options = normalizeOptions(options, this);

            return this.each(function () {
                loadDialog(this, options);
            });
        },

        initDialog: function () {
            return this.each(function () {
                initDialog(this);
            });
        },

        fillList: function (options) {
            return this.each(function () {
                fillList(this, options);
            });
        },

        itemClick: function () {
            return this.each(function () {
                itemClick(this);
            });
        }
    };

    $.fn.entityPicker = function (method) {
        return main.apply(this, arguments);
    };

    $.entityPicker = function () {
        return main.apply($('.entpicker:first'), arguments);
    };


    function main(method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }

        if (typeof method === 'object' || !method) {
            var btn = this;
            $(btn).on('click', function (e) {
                if (!btn.is('.disabled')) {
                    loadDialog(btn, normalizeOptions(method, btn));
                }
            });

            return null;
        }

        EventBroker.publish("message", { title: 'Method "' + method + '" does not exist on jQuery.entityPicker', type: "error" });
        return null;
    }

    function normalizeOptions(options, context) {
        var self = $(context);

        var defaults = {
            url: '',
            entityType: 'product',
            caption: '&nbsp;',
            disableIf: '',
            disableIds: '',
            thumbZoomer: false,
            highligtSearchTerm: true,
            returnField: 'id',
            delim: ',',
            targetInput: null,
            selected: null,
            appendMode: true,
            maxItems: 0,
            onDialogLoading: null,
            onDialogLoaded: null,
            onSelectionCompleted: null
        };

        // Use self.attr, not self.data!
        options = $.extend({}, defaults, options);
        options.entityType = self.attr('data-entitytype') || options.entityType;
        options.caption = self.attr('data-caption') || options.caption;

        if (_.isEmpty(options.url)) {
            options.url = self.attr('data-url');
        }

        if (options.maxItems == 0 && !_.isEmpty(self.data('maxitems'))) {
            options.maxItems = self.data('maxitems');
        }

        if (options.appendMode == true && !_.isEmpty(self.data('appendmode'))) {
            options.appendMode = self.data('appendmode');
        }

        if (_.isEmpty(options.url)) {
            console.error('EntityPicker cannot find the url for entity picker!');
        }

        if (!options.targetInput && self.data('target')) {
            options.targetInput = $(self.data('target')).first();
        }

        if (options.targetInput) {
            var val = $(options.targetInput).val();
            if (val && val.length > 0) {
                var returnId = options.returnField.toLowerCase() === 'id';
                options.selected = _.map(val.split(options.delim), function (x) {
                    return returnId ? toInt(x.trim()) : x.trim();
                });
            }
        }

        if (!_.isArray(options.selected)) {
            options.selected = [];
        }

        return options;
    }

    function ajaxErrorHandler(objXml) {
        try {
            if (objXml != null && objXml.responseText != null && objXml.responseText !== '') {
                EventBroker.publish("message", { title: objXml.responseText, type: "error" });
            }
        }
        catch (e) { }
    }

    function showStatus(dialog, noteClass, condition) {
        var footerNote = $(dialog).find('.footer-note');
        footerNote.find('span').hide();
        footerNote.find('.' + (noteClass || 'default')).show();
    }

    function loadDialog(context /* button */, opt) {
        var btn = $(context);
        var dialog = $('#entpicker-' + opt.entityType + '-dialog');

        function showAndFocusDialog() {
            dialog = $('#entpicker-' + opt.entityType + '-dialog');
            dialog.find('.modal-title').html(opt.caption || '&nbsp;');
            dialog.data('entitypicker', opt);
            dialog.modal('show');

            fillList(dialog, { append: false });

            setTimeout(function () {
                dialog.find('.modal-header :input:visible:enabled:first')[0].focus();
            }, 800);
        }

        if (dialog.length) {
            showAndFocusDialog();
        }
        else {
            $.ajax({
                cache: false,
                type: 'GET',
                data: {
                    "EntityType": opt.entityType,
                    "HighligtSearchTerm": opt.highligtSearchTerm,
                    "ReturnField": opt.returnField,
                    "MaxItems": opt.maxItems,
                    "Selected": opt.selected.join(),
                    "DisableIf": opt.disableIf,
                    "DisableIds": opt.disableIds
                },
                url: opt.url,
                beforeSend: function () {
                    btn.addClass('disabled').prop('disabled', true);
                    if (_.isFunction(window[opt.onDialogLoading])) {
                        return window[opt.onDialogLoading](dialog);
                    }
                },
                success: function (response) {
                    $('body').append(response);
                    showAndFocusDialog();
                },
                complete: function () {
                    btn.prop('disabled', false).removeClass('disabled');
                    if (_.isFunction(window[opt.onDialogLoaded])) {
                        window[opt.onDialogLoaded](dialog);
                    }
                },
                error: ajaxErrorHandler
            });
        }
    }

    function initDialog(context) {
        var dialog = $(context);
        var keyUpTimer = null;
        var currentValue = '';

        // search entities
        dialog.find('button[name=SearchEntities]').on('click', function (e) {
            e.preventDefault();
            fillList(this, { append: false });
            return false;
        });

        // toggle filters
        dialog.find('button[name=FilterEntities]').on('click', function () {
            dialog.find('.entpicker-filter').slideToggle(200);
        });

        // hit enter or key up starts searching
        dialog.find('input.entpicker-searchterm').on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                return false;
            }
        }).bind('keyup change paste', function (e) {
            try {
                var val = $(this).val();

                if (val !== currentValue) {
                    if (keyUpTimer) {
                        keyUpTimer = clearTimeout(keyUpTimer);
                    }

                    keyUpTimer = setTimeout(function () {
                        fillList(dialog, {
                            append: false,
                            onSuccess: function () {
                                currentValue = val;
                            }
                        });
                    }, 500);
                }
            }
            catch (err) { }
        });

        // filter change starts searching
        dialog.find('.entpicker-filter .form-control').on('change', function () {
            fillList(this, { append: false });
        });

        // lazy loading
        dialog.find('.modal-body').on('scroll', function (e) {
            if (dialog.find('.load-more:not(.loading)').visible(true, false, 'vertical')) {
                fillList(this, { append: true });
            }
        });

        // item select and item hover
        dialog.find('.entpicker-list').on('click', '.entpicker-item', function (e) {
            var item = $(this);

            if (item.hasClass('disabled'))
                return false;

            var dialog = item.closest('.entpicker'),
                list = item.closest('.entpicker-list'),
                data = dialog.data('entitypicker');

            if (data.maxItems === 1) {
                list.find('.entpicker-item').removeClass('selected');
                item.addClass('selected');
            }
            else if (item.hasClass('selected')) {
                item.removeClass('selected');
            }
            else if (data.maxItems === 0 || list.find('.selected').length < data.maxItems) {
                item.addClass('selected');
            }

            dialog.find('.modal-footer .btn-primary').prop('disabled', list.find('.selected').length <= 0);
        }).on({
            mouseenter: function () {
                if ($(this).hasClass('disabled'))
                    showStatus($(this).closest('.entpicker'), 'not-selectable');
            },
            mouseleave: function () {
                if ($(this).hasClass('disabled'))
                    showStatus($(this).closest('.entpicker'));
            }
        }, '.entpicker-item');

        // return value(s)
        dialog.find('.modal-footer .btn-primary').on('click', function () {
            var dialog = $(this).closest('.entpicker');
            var items = dialog.find('.entpicker-list .selected');
            var opts = dialog.data('entitypicker');
            var returnId = opts.returnField.toLowerCase() === 'id';

            var selectedItems = _.map(items, function (val) {
                return {
                    id: $(val).data('returnvalue'),
                    name: $(val).find('.title').attr('title')
                };
            });

            var selectedValues = _.uniq(_.map(selectedItems, function (x) {
                return returnId && !_.isNumber(x.id) ? toInt(x.id) : x.id;
            }));

            if (opts.appendMode && _.isArray(opts.selected)) {
                selectedValues = _.union(opts.selected, selectedValues);
            }

            var selectedValuesStr = selectedValues.join(opts.delim);

            if (opts.targetInput) {
                $(opts.targetInput).val(selectedValuesStr).trigger('change');
            }

            dialog.find('input[name=Selected]').val(selectedValuesStr);

            if (_.isFunction(window[opts.onSelectionCompleted])) {
                if (window[opts.onSelectionCompleted](selectedValues, selectedItems, dialog)) {
                    dialog.modal('hide');
                }
            }
            else {
                dialog.modal('hide');
            }
        });

        // cancel
        dialog.find('button.entpicker-cancel').on('click', function () {
            dialog.find('.entpicker-list').empty();
            dialog.find('.footer-note span').hide();
            dialog.find('.modal-footer .btn-primary').prop('disabled', true);
        });
    }

    function fillList(context, opt) {
        var dialog = $(context).closest('.entpicker');

        if (opt.append) {
            var pageElement = dialog.find('input[name=PageIndex]');
            var pageIndex = parseInt(pageElement.val());

            pageElement.val(pageIndex + 1);
        }
        else {
            dialog.find('input[name=PageIndex]').val('0');
        }

        $.ajax({
            global: false,
            cache: false,
            type: 'POST',
            data: dialog.find('form:first').serialize(),
            url: dialog.find('form:first').attr('action'),
            beforeSend: function () {
                if (opt.append) {
                    dialog.find('.load-more').addClass('loading');
                }
                else {
                    dialog.find('.entpicker-list').empty();
                    dialog.find('.modal-footer .btn-primary').prop('disabled', true);
                }

                dialog.find('button[name=SearchEntities]').button('loading').prop('disabled', true);
                dialog.find('.load-more').append(createCircularSpinner(20, true));
            },
            success: function (response) {
                var list = dialog.find('.entpicker-list');
                var data = dialog.data('entitypicker');

                list.stop().append(response);

                if (!opt.append) {
                    showStatus(dialog);
                }

                if (list.thumbZoomer && _.isTrue(data.thumbZoomer)) {
                    list.find('.entpicker-thumb > img:not(.zoomable-thumb)').addClass('zoomable-thumb');
                    list.thumbZoomer();
                }

                if (_.isFunction(opt.onSuccess)) {
                    opt.onSuccess();
                }
            },
            complete: function () {
                dialog.find('button[name=SearchEntities]').prop('disabled', false).button('reset');
                dialog.find('.load-more.loading').parent().remove();
            },
            error: ajaxErrorHandler
        });
    }

})(jQuery, window, document);