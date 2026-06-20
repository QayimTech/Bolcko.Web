$(document).ready(function () {
    const tenderSessionKey = "TenderSessionCart";

    // Load initial tender cart items count on header
    updateTenderBadgeCount();

    // Event handler for "Add to Tender Request" button
    $(document).on('click', '.add-to-tender-btn', function (e) {
        e.preventDefault();
        const btn = $(this);
        const productId = btn.data('product-id');
        const productName = btn.data('product-name');
        const unit = btn.data('unit') || 'كيس';

        let quantity = 1;
        // Try to find the quantity input on the same product card or the page
        const qtyInput = $(`#quantity-input-${productId}`);
        if (qtyInput.length) {
            quantity = parseInt(qtyInput.val()) || 1;
        } else {
            const generalQtyInput = $('#quantity-input');
            if (generalQtyInput.length) {
                quantity = parseInt(generalQtyInput.val()) || 1;
            }
        }

        addToTenderSession(productId, productName, unit, quantity);

        // Animate the button to confirm the action
        btn.addClass('scale-90');
        setTimeout(() => btn.removeClass('scale-90'), 200);
    });

    function addToTenderSession(productId, productName, unit, quantity) {
        let tenderCart = JSON.parse(sessionStorage.getItem(tenderSessionKey)) || [];
        const existingItemIndex = tenderCart.findIndex(item => item.productId === productId);

        if (existingItemIndex > -1) {
            tenderCart[existingItemIndex].quantity += quantity;
        } else {
            tenderCart.push({
                productId: productId,
                productName: productName,
                unit: unit,
                quantity: quantity
            });
        }

        sessionStorage.setItem(tenderSessionKey, JSON.stringify(tenderCart));
        updateTenderBadgeCount();
        showTenderNotification(productName);
    }

    function updateTenderBadgeCount() {
        const tenderCart = JSON.parse(sessionStorage.getItem(tenderSessionKey)) || [];
        const itemCount = tenderCart.length; // Count of unique products, not total quantities

        let badge = $('#tender-badge');
        if (badge.length) {
            if (itemCount > 0) {
                badge.text(itemCount).removeClass('hidden');
            } else {
                badge.addClass('hidden');
            }
        }
    }

    function showTenderNotification(productName) {
        // Remove any existing toast
        $('.tender-toast').remove();

        const isAr = $('html').attr('lang') === 'ar' || $('html').attr('dir') === 'rtl';
        const msg = isAr
            ? `تمت إضافة <strong>${productName}</strong> إلى سلة العطاء`
            : `<strong>${productName}</strong> added to tender basket`;
        const linkText = isAr ? 'اكمل طلب العطاء ←' : 'Complete Tender Request →';
        const tenderUrl = '/Shop/Quote/Request';

        const toast = $(`
            <div class="tender-toast fixed bottom-6 end-6 z-[9999] bg-[#151B26] border border-[#E8A020]/30 text-white rounded-2xl shadow-[0_8px_30px_rgba(0,0,0,0.4)] overflow-hidden flex flex-col" style="min-width:280px;max-width:340px;animation:slideUpFade 0.35s ease-out;">
                <div class="flex items-start gap-3 p-4">
                    <span class="material-symbols-outlined text-[#E8A020] text-xl mt-0.5 shrink-0">request_quote</span>
                    <div class="flex-1 text-sm leading-relaxed">${msg}</div>
                    <button class="tender-toast-close text-slate-400 hover:text-white transition-colors shrink-0 mt-0.5">
                        <span class="material-symbols-outlined text-[16px]">close</span>
                    </button>
                </div>
                <div class="border-t border-white/10 px-4 py-3 bg-white/5 flex items-center justify-between">
                    <span class="text-[10px] text-slate-400">${isAr ? 'سلة العطاءات والتسعير' : 'Tender & Quote Basket'}</span>
                    <a href="${tenderUrl}" class="text-[#E8A020] hover:text-white font-bold text-xs transition-colors">${linkText}</a>
                </div>
            </div>
        `);

        // Style injection for animation if not already present
        if (!$('#tender-toast-style').length) {
            $('head').append(`<style id="tender-toast-style">
                @keyframes slideUpFade {
                    from { opacity:0; transform: translateY(16px); }
                    to   { opacity:1; transform: translateY(0); }
                }
            </style>`);
        }

        $('body').append(toast);

        // Close button
        toast.find('.tender-toast-close').on('click', function () {
            toast.fadeOut(300, function () { $(this).remove(); });
        });

        // Auto-dismiss after 4.5s
        setTimeout(function () {
            toast.fadeOut(500, function () { $(this).remove(); });
        }, 4500);
    }
});
