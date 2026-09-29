; (function ($, window, document, undefined) {

    const swatchLabelRestoreDelay = 150;

    function updateSwatchLabel(swatch) {
        const selection = swatch.closest('.choice').find('.choice-label-value').first();

        if (!selection.length) {
            return;
        }

        const valueName = swatch.data('swatch-value') || '';
        if (valueName) {
            selection.removeClass('text-danger text-muted')
                .text(valueName);
        }
    }

    function supportsSwatchLabelPreview(swatch) {
        // Non-card swatches use the dynamic label as their visible replacement for the former tooltip.
        return !swatch.find('.swatch-card').length;
    }

    function clearSwatchLabelRestore(choice) {
        const timer = choice.data('swatch-label-restore-timer');

        if (timer) {
            window.clearTimeout(timer);
            choice.removeData('swatch-label-restore-timer');
        }
    }

    function restoreSwatchLabel(choice) {
        const selectedSwatch = choice.find('.swatch-input:checked').closest('.swatch');

        if (selectedSwatch.length) {
            updateSwatchLabel(selectedSwatch);
            return;
        }

        const selection = choice.find('.choice-label-value').first();
        const emptyClass = selection.attr('data-swatch-empty-class');

        selection
            .removeClass('text-danger text-muted')
            .addClass(emptyClass || '')
            .text(selection.attr('data-swatch-empty-value') || '');
    }

    function scheduleSwatchLabelRestore(choice) {
        clearSwatchLabelRestore(choice);

        const timer = window.setTimeout(() => {
            choice.removeData('swatch-label-restore-timer');
            restoreSwatchLabel(choice);
        }, swatchLabelRestoreDelay);

        choice.data('swatch-label-restore-timer', timer);
    }

    $(document)
        .on('mouseenter focusin', '.swatch', (e) => {
            const swatch = $(e.currentTarget);
            const choice = swatch.closest('.choice');
            clearSwatchLabelRestore(choice);

            if (supportsSwatchLabelPreview(swatch)) {
                updateSwatchLabel(swatch);
            }
            else {
                restoreSwatchLabel(choice);
            }
        })
        .on('mouseleave focusout', '.swatch', (e) => {
            if (e.type === 'focusout' && e.relatedTarget && $.contains(e.currentTarget, e.relatedTarget)) {
                return;
            }

            const swatch = $(e.currentTarget);
            if (supportsSwatchLabelPreview(swatch)) {
                const choice = swatch.closest('.choice');
                if (e.type === 'mouseleave') {
                    scheduleSwatchLabelRestore(choice);
                }
                else {
                    restoreSwatchLabel(choice);
                }
            }
        })
        .on('change', '.swatch-input', (e) => {
            updateSwatchLabel($(e.currentTarget).closest('.swatch'));
        });

})(jQuery, this, document);
