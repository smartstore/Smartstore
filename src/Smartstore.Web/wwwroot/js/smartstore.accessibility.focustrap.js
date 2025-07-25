﻿/* Focus trap
 * * TODO: (wcag) (mh) 
        - Docs  
        - alert2
        - What else must be trapped?
    */
(() => {
    let onRelease = null;
    let lastActiveElement = null;

    // TODO: (wcag) (mh) We also have classes .disabled
    // Determine visible & focusable nodes in the container 
    const selectors = [
        'a[href]',
        'button:not([disabled])',
        'input:not([disabled]):not([type="hidden"])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        '[tabindex]:not([tabindex="-1"])'
     ].join(',');

    const focusables = (c, excludeSel) =>
        [...c.querySelectorAll(selectors)].filter((el) =>
            el.offsetParent !== null && getComputedStyle(el).visibility !== 'hidden' && (!excludeSel || !el.closest(excludeSel))
        );

    function initOffCanvasTrap() {
        console.log('initOffCanvasTrap');
        // INFO: Jquery must be used here, because original event is namespaced & triggered via Jquery.
        $(document).on('shown.sm.offcanvas', (e) => {
            const offcanvas = $(e.target).aria("hidden", false);

            // Set attribute aria-expanded for opening element.
            $(`[aria-controls="${offcanvas.attr('id')}"]`).aria("expanded", true);

            activate(offcanvas[0]);
        });

        $(document).on('hidden.sm.offcanvas', (e) => {
            const offcanvas = $(e.target).aria("hidden", true);

            // Set attribute aria-expanded for the element that has opened offcanvas.
            $(`[aria-controls="${offcanvas.attr('id')}"]`).aria("expanded", false);

            deactivate();
        });

        // Offcanvas layers must maintain focus after they are loaded and displayed via AJAX.
        $(document).on('shown.sm.offcanvaslayer', (e) => {
            activate(e.target);
            // INFO: Deactivation will be handled automatically on hidden.sm.offcanvas
        });
    }

    function initDialogTrap() {
        $(document).on('shown.bs.modal', (e) => {
            activate(e.target);
        });

        $(document).on('hidden.bs.modal', () => {
            deactivate();
        });
    }

    function activate(container, { initial, exclude } = {}) {
        // TODO: (wcag) (mh) What about modals with embedded iframes?
        // There can only be one active focus trap at a time.
        if (onRelease)
            onRelease();               

        lastActiveElement = document.activeElement;

        const first = initial ? container.querySelector(initial) : focusables(container, exclude)[0] || container;

        /** TAB-Wrap / Shift+TAB-Wrap */
        const onKey = (e) => {
            if (e.key !== 'Tab') return;
            const list = focusables(container, exclude);
            if (!list.length) {
                e.preventDefault();
                return;
            }

            const firstEl = list[0];
            const lastEl  = list[list.length - 1];

            if (e.shiftKey && document.activeElement === firstEl)
            {
                e.preventDefault();
                lastEl.focus();
            }
            else if (!e.shiftKey && document.activeElement === lastEl)
            {
                e.preventDefault();
                firstEl.focus();
            }
        };

        /** Focus must not leave container */
        const onFocusIn = (e) => {
            if (!container.contains(e.target))
            {
                (focusables(container, exclude)[0] || container).focus();
            }
        };

        document.addEventListener('keydown', onKey, true);
        document.addEventListener('focusin', onFocusIn, true);

        // Set initial focus
        first?.focus();

        onRelease = () => {
            document.removeEventListener('keydown', onKey, true);
            document.removeEventListener('focusin', onFocusIn, true);
            onRelease = null;
            // Return focus to opener
            lastActiveElement?.focus();            
        };
    }

    function deactivate()
    {
        if (onRelease) onRelease();
    }

    document.addEventListener('DOMContentLoaded', () => {
        initOffCanvasTrap();
        initDialogTrap();
    });

    window.AccessKitFocusTrap = { activate, deactivate };
})();