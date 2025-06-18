/* Focus trap
 * * TODO: (wcag) (mh) 
        - Docs  
        - alert2
        - MediaGallery
        - What else must be trapped?
    */
(() => {
    let releaseFn = null;
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

    function activate(container, { initial, exclude } = { }) {
        // There can only be one active focus trap at a time.
        if (releaseFn)
            releaseFn();               

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
                e.preventDefault(); lastEl.focus();
            }
            else if (!e.shiftKey && document.activeElement === lastEl)
            {
                e.preventDefault(); firstEl.focus();
            }
        };

        /** Fokus darf Container nicht verlassen */
        const onFocusIn = (e) => {
            if (!container.contains(e.target))
            {
                (focusables(container, exclude)[0] || container).focus();
            }
        };

        document.addEventListener('keydown', onKey, true);
        document.addEventListener('focusin', onFocusIn, true);
        first?.focus();

        releaseFn = () => {
            document.removeEventListener('keydown', onKey, true);
            document.removeEventListener('focusin', onFocusIn, true);
            releaseFn = null;
            lastActiveElement?.focus();            // Return focus to opener
        };
    }

    function deactivate()
    {
        if (releaseFn) releaseFn();
    }

    window.AccessKitFocusTrap = { activate, deactivate };
})();