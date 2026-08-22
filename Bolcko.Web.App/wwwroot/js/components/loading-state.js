/**
 * Block-O UI Loading State Component
 * Handles AJAX loading states, button spinners, and prevents duplicate submissions seamlessly.
 */

(function () {
    'use strict';

    window.BlockoUI = window.BlockoUI || {};

    /**
     * Set a button into loading state
     * @param {HTMLElement|string} button - Button element or selector
     * @param {string} [loadingText] - Optional temporary loading text
     */
    window.BlockoUI.setButtonLoading = function (button, loadingText) {
        var btn = typeof button === 'string' ? document.querySelector(button) : button;
        if (!btn || btn.dataset.isLoading === 'true') return;

        btn.dataset.isLoading = 'true';
        btn.dataset.originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.classList.add('opacity-75', 'cursor-not-allowed', 'pointer-events-none');

        var spinnerHtml = '<svg class="animate-spin -ms-1 me-2 h-4 w-4 text-current inline-block" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">' +
            '<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>' +
            '<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>' +
            '</svg>';

        if (loadingText) {
            btn.innerHTML = spinnerHtml + '<span>' + loadingText + '</span>';
        } else {
            btn.innerHTML = spinnerHtml + btn.innerHTML;
        }
    };

    /**
     * Restore button from loading state
     * @param {HTMLElement|string} button - Button element or selector
     */
    window.BlockoUI.resetButtonLoading = function (button) {
        var btn = typeof button === 'string' ? document.querySelector(button) : button;
        if (!btn || btn.dataset.isLoading !== 'true') return;

        if (btn.dataset.originalHtml) {
            btn.innerHTML = btn.dataset.originalHtml;
            delete btn.dataset.originalHtml;
        }
        btn.dataset.isLoading = 'false';
        btn.disabled = false;
        btn.classList.remove('opacity-75', 'cursor-not-allowed', 'pointer-events-none');
    };

    // Auto-attach listeners for forms with attribute data-ajax-form="true"
    document.addEventListener('DOMContentLoaded', function () {
        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (form && form.dataset && form.dataset.ajaxForm === 'true') {
                var submitBtn = form.querySelector('button[type="submit"]');
                if (submitBtn) {
                    window.BlockoUI.setButtonLoading(submitBtn);
                }
            }
        });
    });
})();
