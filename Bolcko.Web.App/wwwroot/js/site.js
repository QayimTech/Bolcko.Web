$(document).ready(function () {
    // Quantity increment/decrement
    $(document).on('click', '[data-quantity-btn]', function () {
        const btn = $(this);
        const container = btn.parent();
        const input = container.find('input');
        let val = parseInt(input.val());
        
        if (btn.data('quantity-btn') === 'add') {
            input.val(val + 1);
        } else if (btn.data('quantity-btn') === 'remove' && val > 1) {
            input.val(val - 1);
        }
    });

    // Simple mobile menu toggle (if needed in future)
    // Add logic here
});