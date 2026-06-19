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

    // Mobile Menu Toggle
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const mobileMenu = document.getElementById('mobileMenu');
    const menuIcon = document.getElementById('menuIcon');
    let isMenuOpen = false;

    mobileMenuBtn?.addEventListener('click', function () {
        isMenuOpen = !isMenuOpen;
        
        if (isMenuOpen) {
            mobileMenu.style.maxHeight = mobileMenu.scrollHeight + 'px';
            if (menuIcon) menuIcon.textContent = 'close';
        } else {
            mobileMenu.style.maxHeight = '0px';
            if (menuIcon) menuIcon.textContent = 'menu';
        }
    });
});