// ============================================================
// NOTIFICATION SOUND ENGINE
// Uses AudioContext to bypass browser autoplay restrictions.
// Audio is unlocked on the first user interaction with the page.
// ============================================================
(function () {
    var _audioCtx = null;
    var _soundBuffer = null;
    var _soundUrl = null;
    var _unlocked = false;

    // Detect sound URL from the <audio> element injected by Razor
    function detectSoundUrl() {
        var el = document.getElementById('notification-sound') || document.getElementById('shop-notification-sound');
        return el ? el.getAttribute('src') : '/sounds/default-notification.wav';
    }

    // Preload the audio buffer via Web Audio API
    function preloadSound(url) {
        if (!url || _soundBuffer) return;
        _soundUrl = url;
        _getAudioContext().then(function (ctx) {
            if (!ctx) return;
            fetch(url)
                .then(function (res) { return res.arrayBuffer(); })
                .then(function (buf) { return ctx.decodeAudioData(buf); })
                .then(function (decoded) {
                    _soundBuffer = decoded;
                    console.log('[Sound] Preloaded:', url);
                })
                .catch(function (e) { console.warn('[Sound] Preload failed:', e); });
        });
    }

    function _getAudioContext() {
        return new Promise(function (resolve) {
            if (!window.AudioContext && !window.webkitAudioContext) { resolve(null); return; }
            if (!_audioCtx) {
                _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            }
            if (_audioCtx.state === 'suspended') {
                _audioCtx.resume().then(function () { resolve(_audioCtx); });
            } else {
                resolve(_audioCtx);
            }
        });
    }

    // Unlock audio on first click/touch anywhere on page
    function unlockAudio() {
        if (_unlocked) return;
        _unlocked = true;
        _getAudioContext().then(function (ctx) {
            if (!ctx) return;
            // Play a silent buffer to unlock
            var buf = ctx.createBuffer(1, 1, 22050);
            var src = ctx.createBufferSource();
            src.buffer = buf;
            src.connect(ctx.destination);
            src.start(0);
            console.log('[Sound] AudioContext unlocked.');
            // Now load the real sound
            preloadSound(detectSoundUrl());
        });
    }

    document.addEventListener('click', unlockAudio, { once: false, passive: true });
    document.addEventListener('keydown', unlockAudio, { once: false, passive: true });
    document.addEventListener('touchstart', unlockAudio, { once: false, passive: true });

    // Also try to preload immediately (works if page was already interacted with)
    window.addEventListener('load', function () {
        preloadSound(detectSoundUrl());
    });

    // PUBLIC: play notification sound
    window.playNotificationSound = function () {
        _getAudioContext().then(function (ctx) {
            if (!ctx) return;
            if (_soundBuffer) {
                var src = ctx.createBufferSource();
                src.buffer = _soundBuffer;
                src.connect(ctx.destination);
                src.start(0);
                return;
            }
            // Buffer not ready yet — try <audio> fallback
            var el = document.getElementById('notification-sound') || document.getElementById('shop-notification-sound');
            if (el) {
                el.currentTime = 0;
                el.play().catch(function () {});
            }
        });
    };
})();


// ============================================================
// NOTIFICATION CENTER
// ============================================================
$(document).ready(function () {
    // Quantity controls
    $(document).on('click', '[data-quantity-btn]', function () {
        const btn = $(this);
        const input = btn.parent().find('input');
        let val = parseInt(input.val()) || 1;
        if (btn.data('quantity-btn') === 'add') input.val(val + 1);
        else if (btn.data('quantity-btn') === 'remove' && val > 1) input.val(val - 1);
    });

    // Mobile Menu
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const mobileMenu = document.getElementById('mobileMenu');
    const menuIcon = document.getElementById('menuIcon');
    let isMenuOpen = false;
    mobileMenuBtn?.addEventListener('click', function () {
        isMenuOpen = !isMenuOpen;
        if (mobileMenu) mobileMenu.style.maxHeight = isMenuOpen ? mobileMenu.scrollHeight + 'px' : '0px';
        if (menuIcon) menuIcon.textContent = isMenuOpen ? 'close' : 'menu';
    });

    // Load notifications on page load
    fetchNotifications();

    // Close dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('#notification-center-dropdown').length) {
            const menu = $('#notification-menu');
            if (!menu.hasClass('hidden')) {
                menu.addClass('opacity-0 translate-y-1').removeClass('opacity-100 translate-y-0');
                setTimeout(() => menu.addClass('hidden'), 200);
            }
        }
    });
});

window.toggleNotificationDropdown = function (e) {
    e?.preventDefault();
    e?.stopPropagation();
    const menu = $('#notification-menu');
    if (menu.hasClass('hidden')) {
        menu.removeClass('hidden');
        setTimeout(() => menu.removeClass('opacity-0 translate-y-1').addClass('opacity-100 translate-y-0'), 10);
        // Mark as read after opening
        setTimeout(fetchNotifications, 300);
    } else {
        menu.addClass('opacity-0 translate-y-1').removeClass('opacity-100 translate-y-0');
        setTimeout(() => menu.addClass('hidden'), 200);
    }
};

window.fetchNotifications = function () {
    $.get('/Notification/GetRecent', function (res) {
        if (res && res.success) {
            renderNotifications(res.notifications, res.unreadCount);
        }
    }).fail(function () {
        // User not logged in — silently ignore
    });
};

window.renderNotifications = function (notifications, unreadCount) {
    const badge = $('#notification-badge');
    if (unreadCount > 0) {
        badge.removeClass('hidden');
    } else {
        badge.addClass('hidden');
    }

    const list = $('#notification-list');
    list.empty();

    if (!notifications || notifications.length === 0) {
        list.append('<div class="p-8 text-center text-slate-500 text-xs font-bold leading-relaxed">لا يوجد إشعارات جديدة</div>');
        return;
    }

    const isAdmin = document.body.classList.contains('bg-[#F8FAFC]');

    notifications.forEach(n => {
        const isRead = (n.isRead === true || n.IsRead === true);
        
        // Premium CSS variables & classes for light/dark modes
        const unreadBg     = isAdmin ? 'bg-indigo-50/40 border-s-4 border-indigo-600' : 'bg-[#E8A020]/10 border-s-4 border-[#E8A020]';
        const readBg       = isAdmin ? 'bg-white hover:bg-slate-50' : 'bg-[#161d2d]/30 hover:bg-white/5';
        const textClass    = isAdmin ? 'text-slate-800' : 'text-slate-100';
        const subText      = isAdmin ? 'text-slate-500' : 'text-slate-400';
        const titleHover   = isAdmin ? 'group-hover:text-indigo-600' : 'group-hover:text-[#E8A020]';
        
        const iconClass    = isAdmin
            ? (isRead ? 'bg-slate-100 text-slate-500' : 'bg-indigo-100 text-indigo-600')
            : (isRead ? 'bg-white/5 text-slate-400' : 'bg-[#E8A020]/20 text-[#E8A020]');
            
        const dotColor     = isAdmin ? 'bg-indigo-600' : 'bg-[#E8A020]';

        const bgClass  = isRead ? readBg : unreadBg;
        const dot      = isRead ? '' : `<span class="w-2.5 h-2.5 rounded-full ${dotColor} shadow-lg shrink-0"></span>`;
        const href     = n.actionUrl ? `href="${n.actionUrl}"` : `href="#"`;
        const cursor   = n.actionUrl ? 'cursor-pointer' : '';

        let icon = 'notifications';
        const tl = (n.title || '').toLowerCase();
        const ml = (n.message || '').toLowerCase();
        if (tl.includes('طلب') || tl.includes('order') || ml.includes('طلب') || ml.includes('order')) icon = 'shopping_cart';
        else if (tl.includes('توصيل') || tl.includes('سائق') || tl.includes('مندوب') || tl.includes('delivery')) icon = 'local_shipping';

        list.append(`
            <a ${href} onclick="markAsRead(${n.id}, event, '${n.actionUrl || ''}')"
               class="block p-4.5 ${bgClass} ${cursor} transition-all duration-300 group border-b ${isAdmin ? 'border-slate-100' : 'border-white/5'}">
                <div class="flex items-start gap-4">
                    <div class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0 transition-all duration-300 ${iconClass} group-hover:scale-105">
                        <span class="material-symbols-outlined text-[20px]">${icon}</span>
                    </div>
                    <div class="flex-grow text-start min-w-0">
                        <h4 class="text-xs font-black ${textClass} mb-1 ${titleHover} transition-colors truncate">${n.title || ''}</h4>
                        <p class="text-[11px] ${subText} leading-relaxed font-medium break-words">${n.message || ''}</p>
                        <span class="text-[9px] font-bold text-slate-500 mt-2 block">${n.createdAt || ''}</span>
                    </div>
                    <div class="self-center">
                        ${dot}
                    </div>
                </div>
            </a>`);
    });

    // Add "See All" button at the bottom of dropdown
    const seeAllBg = isAdmin ? 'bg-slate-50 border-t border-slate-100 hover:bg-slate-100' : 'bg-[#161d2d]/90 border-t border-white/5 hover:bg-[#161d2d]';
    const seeAllText = isAdmin ? 'text-indigo-600' : 'text-[#E8A020]';
    list.append(`
        <div class="p-3 text-center ${seeAllBg} transition-colors">
            <a href="/Notification/Index" class="text-xs font-black ${seeAllText} tracking-wider hover:underline block w-full">رؤية الكل</a>
        </div>
    `);
};

window.markAsRead = function (id, event, actionUrl) {
    if (!actionUrl || actionUrl === 'null' || actionUrl === '') {
        event?.preventDefault();
    }
    
    // Optimistically hide the dropdown immediately on click
    const menu = $('#notification-menu');
    menu.addClass('opacity-0 translate-y-1').removeClass('opacity-100 translate-y-0');
    setTimeout(() => menu.addClass('hidden'), 200);

    $.post('/Notification/MarkAsRead?id=' + id, function () {
        fetchNotifications();
        if (actionUrl && actionUrl !== 'null' && actionUrl !== '') {
            window.location.href = actionUrl;
        }
    });
};

window.markAllNotificationsAsRead = function () {
    $.post('/Notification/MarkAllAsRead', function () {
        fetchNotifications();
        const menu = $('#notification-menu');
        menu.addClass('opacity-0 translate-y-1').removeClass('opacity-100 translate-y-0');
        setTimeout(() => menu.addClass('hidden'), 200);
    });
};

// ============================================================
// PREMIUM TAILWIND CSS MODAL SYSTEM (window.BlockoModal)
// ============================================================
(function (window) {
    'use strict';

    class BlockoModal {
        /**
         * Renders a premium dynamic Modal matching the project branding
         * @param {object} params - { title, message, icon, confirmText, cancelText, onConfirm, onCancel }
         */
        show(params = {}) {
            const title = params.title || 'تنبيه';
            const message = params.message || '';
            const icon = params.icon || 'info'; // 'info', 'warning', 'error', 'success'
            const confirmText = params.confirmText || 'موافق';
            const cancelText = params.cancelText || '';
            
            // Icon visual colors mapping
            let iconColor = 'text-[#E8A020] bg-[#E8A020]/10 border-[#E8A020]/20';
            if (icon === 'error') iconColor = 'text-red-500 bg-red-500/10 border-red-500/20';
            if (icon === 'success') iconColor = 'text-emerald-500 bg-emerald-500/10 border-emerald-500/20';
            
            // Icon material symbols mapping
            let iconSymbol = 'info';
            if (icon === 'error') iconSymbol = 'cancel';
            if (icon === 'warning') iconSymbol = 'warning';
            if (icon === 'success') iconSymbol = 'check_circle';

            const modalId = 'blocko-modal-' + Date.now();
            const isRtl = document.documentElement.dir === 'rtl' || document.documentElement.lang === 'ar';

            const modalHtml = `
                <div id="${modalId}" class="fixed inset-0 z-[999999] flex items-center justify-center p-4 opacity-0 transition-opacity duration-300 pointer-events-auto" dir="${isRtl ? 'rtl' : 'ltr'}">
                    <!-- Glassmorphic dark backdrop -->
                    <div class="absolute inset-0 bg-[#0D111A]/60 backdrop-blur-sm"></div>
                    
                    <!-- Content Card -->
                    <div class="relative bg-white border border-slate-100 rounded-2xl p-6 shadow-2xl max-w-sm w-full transform scale-95 opacity-0 transition-all duration-300 flex flex-col gap-5 text-start">
                        <div class="flex items-start gap-4">
                            <div class="w-12 h-12 rounded-xl flex items-center justify-center border shrink-0 ${iconColor}">
                                <span class="material-symbols-outlined text-[26px]">${iconSymbol}</span>
                            </div>
                            <div class="flex-grow">
                                <h3 class="text-base font-black text-slate-900 leading-snug">${title}</h3>
                                <p class="text-xs text-slate-500 mt-2 font-medium leading-relaxed">${message}</p>
                            </div>
                        </div>
                        
                        <div class="flex items-center justify-end gap-3 mt-2">
                            ${cancelText ? `
                                <button id="${modalId}-cancel" class="px-4 py-2 text-xs font-bold text-slate-600 hover:text-slate-900 border border-slate-200 hover:bg-slate-50 rounded-xl transition-colors">
                                    ${cancelText}
                                </button>
                            ` : ''}
                            <button id="${modalId}-confirm" class="px-5 py-2 text-xs font-bold text-white bg-[#0D111A] hover:bg-[#1E293B] rounded-xl transition-colors shadow-sm">
                                ${confirmText}
                            </button>
                        </div>
                    </div>
                </div>
            `;

            // Append to DOM
            const container = document.createElement('div');
            container.innerHTML = modalHtml.trim();
            const modalNode = container.firstChild;
            document.body.appendChild(modalNode);

            // Access inner card
            const card = modalNode.querySelector('.relative');

            // Trigger enter animations
            setTimeout(() => {
                modalNode.classList.remove('opacity-0');
                modalNode.classList.add('opacity-100');
                card.classList.remove('scale-95', 'opacity-0');
                card.classList.add('scale-100', 'opacity-100');
            }, 10);

            // Dismiss function
            const dismiss = () => {
                modalNode.classList.remove('opacity-100');
                modalNode.classList.add('opacity-0');
                card.classList.remove('scale-100', 'opacity-100');
                card.classList.add('scale-95', 'opacity-0');
                setTimeout(() => modalNode.remove(), 300);
            };

            // Event Listeners
            modalNode.querySelector(`#${modalId}-confirm`).addEventListener('click', () => {
                dismiss();
                if (typeof params.onConfirm === 'function') params.onConfirm();
            });

            if (cancelText) {
                modalNode.querySelector(`#${modalId}-cancel`).addEventListener('click', () => {
                    dismiss();
                    if (typeof params.onCancel === 'function') params.onCancel();
                });
            }
        }

        /**
         * Replaces standard confirm() box using promises
         */
        confirm(message, title = 'تأكيد العملية') {
            return new Promise((resolve) => {
                this.show({
                    title: title,
                    message: message,
                    icon: 'warning',
                    confirmText: 'تأكيد',
                    cancelText: 'إلغاء',
                    onConfirm: () => resolve(true),
                    onCancel: () => resolve(false)
                });
            });
        }
    }

    window.BlockoModal = Object.freeze(new BlockoModal());

})(window);