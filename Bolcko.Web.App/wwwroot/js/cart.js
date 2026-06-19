/**
 * Shopping Cart – client-side logic
 * Handles: quantity increment/decrement, direct input, AJAX update, remove-item modal.
 */
(function ($) {
    'use strict';

    // ── State ────────────────────────────────────────────────────────────────
    var activeInput  = null;
    var activeItemId = null;
    var timers       = {};
    var csrfToken    = null; // resolved on DOM ready

    // ── DOM refs (resolved once on ready) ────────────────────────────────────
    var $modal    = null;
    var $backdrop = null;
    var $panel    = null;

    $(function () {
        // Resolve token & DOM refs now that the page is fully loaded
        csrfToken = $('input[name="__RequestVerificationToken"]').val();
        $modal    = $('#removeModal');
        $backdrop = $('#removeModalBackdrop');
        $panel    = $('#removeModalPanel');
    });

    // ── Modal helpers ─────────────────────────────────────────────────────────

    function showModal() {
        $modal.removeClass('hidden');
        void $modal[0].offsetWidth; // force reflow for transition
        $backdrop.removeClass('opacity-0').addClass('opacity-100');
        $panel.removeClass('opacity-0 translate-y-4 scale-95')
              .addClass('opacity-100 translate-y-0 scale-100');
    }

    function hideModal() {
        $backdrop.removeClass('opacity-100').addClass('opacity-0');
        $panel.removeClass('opacity-100 translate-y-0 scale-100')
              .addClass('opacity-0 translate-y-4 scale-95');
        setTimeout(function () { $modal.addClass('hidden'); }, 300);
    }

    function promptRemove(input, itemId) {
        activeInput  = input;
        activeItemId = itemId;
        showModal();
    }

    // ── AJAX cart update ──────────────────────────────────────────────────────

    function updateCart(itemId, quantity) {
        $('#spinner-' + itemId).removeClass('hidden');

        $.ajax({
            url:  window.CartConfig.updateUrl,
            type: 'POST',
            data: {
                itemId:                   itemId,
                quantity:                 quantity,
                __RequestVerificationToken: csrfToken
            },
            success: function (res) {
                $('#spinner-' + itemId).addClass('hidden');
                if (!res.success) return;

                // Update item-level subtotal
                $('#subtotal-' + itemId).html(
                    res.itemSubtotal.toFixed(2) + ' <span class="text-sm">د.أ</span>'
                );

                // Update order-summary totals
                $('#summary-subtotal').text(res.cartSubtotal.toFixed(2) + ' د.أ');
                $('#summary-tax').text(res.cartTax.toFixed(2) + ' د.أ');
                $('#summary-shipping').text(res.cartShipping.toFixed(2) + ' د.أ');
                $('#summary-total').text(res.cartTotal.toFixed(2) + ' د.أ');

                // Remove card from DOM if item was deleted
                if (res.removed) {
                    $('#item-card-' + itemId).fadeOut(300, function () {
                        $(this).remove();
                        if (res.totalItems === 0) location.reload();
                    });
                }
            },
            error: function () {
                $('#spinner-' + itemId).addClass('hidden');
            }
        });
    }

    // Debounce: wait 500 ms after last change before sending request
    function scheduleUpdate(itemId, quantity) {
        clearTimeout(timers[itemId]);
        timers[itemId] = setTimeout(function () {
            updateCart(itemId, quantity);
        }, 500);
    }

    // ── Event bindings ────────────────────────────────────────────────────────

    $(document)

        // Increment button
        .on('click', '[data-increment]', function () {
            var id    = $(this).data('increment');
            var $inp  = $('#quantity-' + id);
            var value = parseInt($inp.val(), 10) + 1;
            $inp.val(value);
            scheduleUpdate(id, value);
        })

        // Decrement button
        .on('click', '[data-decrement]', function () {
            var id    = $(this).data('decrement');
            var $inp  = $('#quantity-' + id);
            var value = parseInt($inp.val(), 10);

            if (value > 1) {
                $inp.val(value - 1);
                scheduleUpdate(id, value - 1);
            } else if (value === 1) {
                promptRemove($inp, id);
            }
        })

        // Direct keyboard input
        .on('input change', '[data-item-id]', function () {
            var id    = $(this).data('item-id');
            var value = parseInt($(this).val(), 10);

            if (isNaN(value) || value < 0) {
                $(this).val(1);
                scheduleUpdate(id, 1);
            } else if (value === 0) {
                promptRemove($(this), id);
            } else {
                scheduleUpdate(id, value);
            }
        })

        // Modal – cancel
        .on('click', '#btnCancelRemove', function () {
            if (activeInput) activeInput.val(1);
            hideModal();
        })

        // Modal – confirm remove
        .on('click', '#btnConfirmRemove', function () {
            hideModal();
            if (activeItemId) updateCart(activeItemId, 0);
        });

}(jQuery));
