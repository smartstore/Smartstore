/**
 * Focus Trap Module
 * 
 * Manages keyboard focus within modal dialogs and offcanvas elements to ensure WCAG 2.1 compliance. 
 * Prevents focus from escaping trapped containers and provides proper keyboard navigation (Tab/Shift+Tab wrapping).
 * 
 * Automatically activates for:
 * - Bootstrap modals (shown.bs.modal / hidden.bs.modal)
 * - Offcanvas elements (shown.sm.offcanvas / hidden.sm.offcanvas)
 * - Offcanvas layers (shown.sm.offcanvaslayer)
 */
(() => {
    let onRelease = null;
    let lastActiveElement = null;

    /**
     * Selector for potentially focusable elements.
     * Actual focusability is determined by the focusables() function.
     */
    const selectors = [
        'a[href]',
        'button',
        'input',
        'select',
        'textarea',
        '[tabindex]'
    ].join(',');

    /**
     * Gets all actually focusable elements within a container.
     * Filters out elements that are:
     * - Hidden (display: none, visibility: hidden, hidden attribute)
     * - Disabled (disabled attribute, .disabled class, aria-disabled)
     * - Not tabbable (tabindex="-1", type="hidden")
     * - iFrames (to prevent focus escaping into iframe context)
     * - Matching the exclude selector
     * 
     * @param {HTMLElement} c - Container element to search within
     * @param {string} [excludeSel] - Optional CSS selector for elements to exclude
     * @returns {HTMLElement[]} Array of focusable elements
     */
    const focusables = (c, excludeSel) =>
        [...c.querySelectorAll(selectors)].filter((el) => {
            // Visibility checks
            if (el.offsetParent === null) return false;
            if (getComputedStyle(el).visibility === 'hidden') return false;
            if (el.hasAttribute('hidden')) return false;
            if (el.getAttribute('aria-hidden') === 'true') return false;

            // Disabled checks
            if (el.disabled) return false;
            if (el.classList.contains('disabled')) return false;
            if (el.hasAttribute('aria-disabled')) return false;

            // Special exclusions
            if (el.type === 'hidden') return false;
            if (el.getAttribute('tabindex') === '-1') return false;
            if (el.tagName === 'IFRAME') return false;

            // Exclude selector
            if (excludeSel && el.closest(excludeSel)) return false;

            return true;
        });

    /**
    * Initializes focus trap for offcanvas elements.
    * Listens to jQuery-triggered events and manages ARIA attributes.
    */
    function initOffCanvasTrap() {
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

    /**
     * Activates a focus trap within the specified container.
     * 
     * Features:
     * - Traps keyboard focus within the container
     * - Enables Tab/Shift+Tab wrapping (cycles through focusable elements)
     * - Prevents focus from leaving the container
     * - Removes iframes from tab order to prevent focus escape
     * - Restores focus to the previously active element on release
     * 
     * Only one focus trap can be active at a time. Activating a new trap automatically releases the previous one.
     * 
     * @param {HTMLElement} container - The container element to trap focus within
     * @param {Object} [options] - Configuration options
     * @param {string} [options.initial] - CSS selector for element to receive initial focus
     * @param {string} [options.exclude] - CSS selector for elements to exclude from focus trap
     */
    function activate(container, { initial, exclude } = {}) {
        // There can only be one active focus trap at a time.
        if (onRelease)
            onRelease();               

        lastActiveElement = document.activeElement;

        // Disable jumping into iframes. They are considered as focus sinks.
        const iframes = container.querySelectorAll('iframe');
        const iframeTabindices = new Map();

        iframes.forEach(iframe => {
            const originalTabindex = iframe.getAttribute('tabindex');
            iframeTabindices.set(iframe, originalTabindex);
            iframe.setAttribute('tabindex', '-1');
        });

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