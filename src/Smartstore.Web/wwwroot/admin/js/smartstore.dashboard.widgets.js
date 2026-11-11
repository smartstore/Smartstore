// Loads one dashboard widget without changing its grid placement.
(function ($) {
    const pending = new WeakMap();

    Smartstore.Admin = Smartstore.Admin || {};
    Smartstore.Admin.Dashboard = {
        loadWidget: function (element, refresh = false, parameters = {}, draft = null) {
            const widget = $(element).closest('.dashboard-widget')[0];
            if (!widget || !widget.isConnected) return;
            if (pending.has(widget)) {
                const current = pending.get(widget);
                return draft ? current.then(
                    () => this.loadWidget(element, refresh, parameters, draft),
                    () => this.loadWidget(element, refresh, parameters, draft)) : current;
            }

            const $widget = $(widget);
            const $grid = $widget.closest('[data-dashboard-load-url]');
            const $content = $widget.find('.dashboard-widget-content').first();
            const $buttons = $widget.find('.dashboard-refresh');
            // Widget-specific controls contribute only transient display parameters.
            const values = Object.assign({}, $widget.data('dashboardParameters'), parameters);
            const selectedTab = $content.find('[data-toggle="tab"].active').attr('href');
            const periods = $content.find('input[data-period]');
            const selectedPeriod = periods.index(periods.filter(':checked'));
            const $controls = (draft ? $widget : $content).find('input, select, button').filter(':enabled');
            $widget.attr('aria-busy', 'true');
            $controls.prop('disabled', true);
            $buttons.prop('disabled', true);

            const throbber = draft ? null : $widget.children('.dashboard-widget-body')
                .throbber({ white: true, small: true, message: '', show: false, speed: 0 }).data('throbber');
            throbber?.show();
            const startedAt = performance.now();

            const completion = $.Deferred();
            let succeeded = false;
            const request = $.ajax({
                url: $grid.attr('data-dashboard-load-url'),
                type: 'POST',
                dataType: 'html',
                data: { instanceId: $widget.attr('data-instance-id'), refresh, parameters: values, ...draft }
            });
            pending.set(widget, completion.promise());
            request.done(function (html) {
                succeeded = true;
                if (!widget.isConnected) return;
                $widget.data('dashboardParameters', values);
                $widget.triggerHandler('dashboard:unloading');
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
                    $widget.triggerHandler('dashboard:loaded');
                });
            }).fail(function () {
                if (!widget.isConnected) return;
                displayNotification(window.Res.Dashboard.refreshFailed, 'error');
            }).always(function () {
                // Keep even fast refreshes visible without delaying the data request or rendering.
                window.setTimeout(function () {
                    throbber?.hide();
                    pending.delete(widget);
                    $widget.removeAttr('aria-busy');
                    $buttons.prop('disabled', false);
                    $controls.prop('disabled', false);
                    if (succeeded) completion.resolve();
                    else completion.reject(request);
                }, draft ? 0 : Math.max(0, 400 - (performance.now() - startedAt)));
            });
            return completion.promise();
        }
    };

    $(document).on('click', '.dashboard-refresh', function () {
        Smartstore.Admin.Dashboard.loadWidget(this, true);
    });
})(jQuery);
