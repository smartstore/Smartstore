// Loads one dashboard widget without changing its grid placement.
(function ($) {
    const pending = new WeakMap();

    Smartstore.Admin = Smartstore.Admin || {};
    Smartstore.Admin.Dashboard = {
        loadWidget: function (element, refresh = false, parameters = {}) {
            const widget = $(element).closest('.dashboard-widget')[0];

            if (!widget || !widget.isConnected) return;

            if (pending.has(widget)) {
                return pending.get(widget);
            }

            const $widget = $(widget);
            const $grid = $widget.closest('[data-dashboard-load-url]');
            const $content = $widget.find('.dashboard-widget-content').first();
            const $buttons = $widget.find('.dashboard-refresh');
            // Widget-specific controls contribute only transient display parameters.
            const values = Object.assign({}, $widget.data('dashboardParameters'), parameters);
            const $controls = $content.find('input, select, button').filter(':enabled');
            $widget.attr('aria-busy', 'true');
            $controls.prop('disabled', true);
            $buttons.prop('disabled', true);

            const throbber = $widget.children('.dashboard-widget-body')
                .throbber({ white: true, small: true, message: '', show: false, speed: 0 }).data('throbber');
            throbber?.show();
            const startedAt = performance.now();

            const completion = $.Deferred();
            let succeeded = false;
            let response;
            const request = $.ajax({
                url: $grid.attr('data-dashboard-load-url'),
                type: 'POST',
                dataType: 'html',
                data: { instanceId: $widget.attr('data-instance-id'), refresh, parameters: values }
            });
            pending.set(widget, completion.promise());
            request.always(function (result) {
                try {
                    if (request.state() === 'resolved') {
                        response = result;

                        if (widget.isConnected) {
                            $widget.data('dashboardParameters', values);
                            Smartstore.Admin.Dashboard.updateWidget(widget, result);
                        }

                        succeeded = true;
                    }
                    else if (widget.isConnected) {
                        displayNotification(window.Res.Dashboard.refreshFailed, 'error');
                    }
                }
                finally {
                    // Keep even fast refreshes visible without delaying the data request or rendering.
                    window.setTimeout(function () {
                        throbber?.hide();
                        pending.delete(widget);
                        $widget.removeAttr('aria-busy');
                        $buttons.prop('disabled', false);
                        $controls.prop('disabled', false);

                        if (succeeded) completion.resolve(response);
                        else completion.reject(request);
                    }, Math.max(0, 400 - (performance.now() - startedAt)));
                }
            });
            return completion.promise();
        },
        updateWidget: function (widget, html) {
            if (!widget.isConnected) return;

            const $widget = $(widget);
            const $content = $widget.find('.dashboard-widget-content').first();
            const selectedTab = $content.find('[data-toggle="tab"].active').attr('href');
            const periods = $content.find('input[data-period]');
            const selectedPeriod = periods.index(periods.filter(':checked'));
            // Chart.js keeps instances outside the DOM; release only this widget's charts.
            if (window.Chart) {
                Object.values(Chart.instances || {}).forEach(chart => {
                    if (chart.canvas && $content[0].contains(chart.canvas)) chart.destroy();
                });
            }

            $content.html(html);
            // Existing Razor initializers use document-ready callbacks.
            $(function () {
                if (!widget.isConnected) return;

                if (selectedPeriod >= 0) {
                    $content.find('input[data-period]').eq(selectedPeriod).closest('label').trigger('click');
                }

                if (selectedTab) {
                    $content.find('[data-toggle="tab"]').filter(function () {
                        return this.getAttribute('href') === selectedTab;
                    }).tab('show');
                }
            });
        }
    };

    $(document).on('click', '.dashboard-refresh', function () {
        Smartstore.Admin.Dashboard.loadWidget(this, true);
    });
})(jQuery);
