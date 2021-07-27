/*
*  Project: Smartstore tree view.
*  Author: Marcus Gesing, SmartStore AG.
*/
; (function ($, window, document, undefined) {

    var methods = {
        init: function (options) {
            return this.each(function () {
                initialize(this, options);
            });
        },
    };

    $.fn.treeview = function (method) {
        return main.apply(this, arguments);
    };

    $.treeview = function () {
        return main.apply($('.treeview:first'), arguments);
    };

    $.treeview.defaults = {
        expandedClass: 'fas fa-angle-down',
        collapsedClass: 'fas fa-angle-right',
    };

    function main(method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }

        if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }

        EventBroker.publish('message', { title: 'Treeview method "' + method + '" does not exist.', type: 'error' });
        return null;
    }

    function initialize(context, opt) {
        var root = $(context);

        opt = $.extend({}, $.treeview.defaults, opt);
        root.data('treeview-options', opt);

        loadData({
            root: root,
            global: this._global,
            callback: function (data) {
                console.log(data.nodes);
            }
        });
    }

    function loadData(opt) {
        $.ajax({
            type: 'GET',
            url: opt.root.data('url'),
            global: opt.global,
            dataType: 'json',
            cache: false,
            timeout: 5000,
            //data: { page: params.page, term: params.term },
            success: function (data, status, jqXHR) {
                opt.callback(data);
            }
        });
    }



})(jQuery, this, document);