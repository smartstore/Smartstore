/*
*  Project: Smartstore ajax wrapper
*  Author: Marcus Gesing, SmartStore AG
*/

(function ($, window, document, undefined) {

    const defaults = {
        cache: false,
        type: "POST"
    };

    // Global XMLHttpRequest send interceptor
    let open = XMLHttpRequest.prototype.open;

    // Overwrite the native "open" method
    XMLHttpRequest.prototype.open = function (method, url) {
        // Call the stored reference to the native method
        open.apply(this, arguments);

        if (method?.toLowerCase() == 'post' && !isExternalUrl(url)) {
            var token = getAntiforgeryToken();
            if (token) {
                // INFO: must be called after .open()
                this.setRequestHeader("X-XSRF-Token", encodeURIComponent(token));
            }
        }

        function isExternalUrl(url) {
            const mergedUrl = new URL(url, window.location.href);
            return mergedUrl.host !== window.location.host || mergedUrl.protocol !== window.location.protocol;
        }
    };

    $.fn.ajax = function (options) {
        this.each(function () {
            let opts = createOptions(this, options);

            if (!opts.url) {
                console.warn('AJAX cannot find any URL to call.');
            }
            else if (!_.isFalse(opts.valid)) {
                executeConfirmed(opts);
            }
        });

        return this;
    };

    $.fn.postData = function (options) {
        function createAndSubmitForm(opts) {
            var id = 'DynamicForm_' + Math.random().toString().substring(2);
            var form = '<form id="' + id + '" action="' + opts.url + '" method="' + opts.type + '">';

            if (!_.isUndefined(opts.data)) {
                $.each(opts.data, function (key, val) {
                    var value = $('<div/>').text(val).html() || '';
                    form += '<input type="hidden" name="' + key + '" value="' + value.replace(/"/g, '&quot;') + '" />';
                });
            }

            form += '<input type="hidden" name="__RequestVerificationToken" value="' + getAntiforgeryToken() + '" />';
            form += '</form>';

            $('body').append(form);
            $('#' + id).trigger('submit');
        }

        this.each(function () {
            let opts = createOptions(this, options);

            if (!opts.url) {
                console.warn('postData cannot find any URL to call.');
            }
            else if (_.isEmpty(opts.ask)) {
                createAndSubmitForm(opts);
            }
            else {
                confirm2({
                    message: opts.ask,
                    icon: { type: opts.iconType || 'question' },
                    callback: accepted => {
                        if (accepted) {
                            createAndSubmitForm(opts);
                        }
                    }
                });
            }
        });

        return this;
    }

    $.fn.ajax.defaults = defaults;

    function createOptions(el, options) {
        var $el = $(el);
        var opts = $.extend({}, defaults, options);

        if ($el.is('form')) {
            opts.data = opts.data || $el.serialize();
            opts.url = opts.url || $el.attr('action');
        }

        opts.ask = opts.ask || $el.attr('data-ask');
        opts.url = opts.url || findUrl($el);

        return opts;
    }

    function findUrl($el) {
        var url = $el.attr('href');

        if (typeof url === 'string' && url.substr(0, 11) === 'javascript:')
            url = '';

        if (_.isEmpty(url) || url.length <= 1)
            url = $el.attr('data-url');

        if (_.isEmpty(url) || url.length <= 1)
            url = $el.attr('data-button');

        return url;
    }

    function showAnimation(opts) {
        if (opts.curtainTitle) {
            $.throbber.show(opts.curtainTitle);
        }
        else if (opts.throbber) {
            $(opts.throbber).removeData('throbber').throbber({ white: true, small: true, message: '' });
        }
        else if (opts.smallIcon) {
            $(opts.smallIcon).append(window.createCircularSpinner(16, true));
        }
        else if (opts.buttonIcon) {
            $(opts.buttonIcon).prop('disabled', true).prepend('<i class="fa fa-fw fa-spinner fa-spin"></i>');
        }
    }

    function hideAnimation(opts) {
        if (opts.curtainTitle) {
            $.throbber.hide(true);
        }
        if (opts.throbber) {
            $(opts.throbber).data('throbber').hide(true);
        }
        if (opts.smallIcon) {
            $(opts.smallIcon).find('.spinner').remove();
        }
        if (opts.buttonIcon) {
            $(opts.buttonIcon).prop('disabled', false).find('.fa-spinner').remove();
        }
    }

    function execute(opts) {
        var ajaxOptions = $.extend({}, opts);

        if (opts.appendToUrl) {
            ajaxOptions.url += opts.appendToUrl;
        }

        // OnError
        if (!ajaxOptions.error) {
            ajaxOptions.error = function (xml) {
                try {
                    if (!_.isEmpty(xml?.responseText)) {
                        if (_.isTrue(ajaxOptions.consoleError))
                            console.error(xml.responseText);
                        else
                            EventBroker.publish("message", { title: xml.responseText, type: "error" });
                    }
                }
                catch (e)
                {
                }
            };
        }

        // OnComplete
        ajaxOptions.complete = function (response) {
            hideAnimation(opts);
            _.call(opts.complete);
        }

        // Execute now
        $.ajax(ajaxOptions);

        showAnimation(opts);
    }

    function executeConfirmed(opts) {
        if (_.isEmpty(opts.ask)) {
            execute(opts);
        }
        else {
            confirm2({
                message: opts.ask,
                icon: { type: opts.iconType || 'question' },
                callback: accepted => {
                    if (accepted) {
                        execute(opts);
                    }
                }
            });
        }
    }

})(jQuery, window, document);
