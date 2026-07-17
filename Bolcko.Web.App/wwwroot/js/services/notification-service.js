/**
 * BLOCKO Notification Center Service (SOLID - Single Responsibility)
 * Manages fetching, marking read, and DOM updates of notifications in header dropdowns.
 */
(function (window) {
    'use strict';

    class NotificationService {
        constructor(apiClient) {
            this.apiClient = apiClient || window.ApiClient;
        }

        async fetchNotifications() {
            if (!this.apiClient) return;
            try {
                const notifications = await this.apiClient.get('/Notification/GetUnreadNotifications');
                this.updateUI(notifications);
            } catch (err) {
                if (err.name !== 'AbortError') {
                    console.warn('[NotificationService] Fetch failed:', err);
                }
            }
        }

        async markAsRead(id, event, actionUrl) {
            if (!actionUrl || actionUrl === 'null' || actionUrl === '') {
                event?.preventDefault();
            }
            
            // Hide menu dropdown immediately
            const menu = document.getElementById('notification-menu');
            if (menu) {
                menu.classList.add('opacity-0', 'translate-y-1');
                menu.classList.remove('opacity-100', 'translate-y-0');
                setTimeout(() => menu.classList.add('hidden'), 200);
            }

            try {
                await this.apiClient.post(`/Notification/MarkAsRead?id=${id}`);
                await this.fetchNotifications();
                if (actionUrl && actionUrl !== 'null' && actionUrl !== '') {
                    window.location.href = actionUrl;
                }
            } catch (err) {
                console.error('[NotificationService] MarkRead failed:', err);
            }
        }

        async markAllAsRead() {
            const menu = document.getElementById('notification-menu');
            if (menu) {
                menu.classList.add('opacity-0', 'translate-y-1');
                menu.classList.remove('opacity-100', 'translate-y-0');
                setTimeout(() => menu.classList.add('hidden'), 200);
            }

            try {
                await this.apiClient.post('/Notification/MarkAllAsRead');
                await this.fetchNotifications();
            } catch (err) {
                console.error('[NotificationService] MarkAllRead failed:', err);
            }
        }

        updateUI(notifications) {
            const list = $('#notification-list');
            const countBadge = $('#notification-count');
            const bell = $('#notification-bell');

            if (!list.length) return;
            list.empty();

            const isArabic = document.documentElement.dir === 'rtl' || document.documentElement.lang === 'ar';
            const count = notifications ? notifications.length : 0;
            const isAdmin = window.location.pathname.toLowerCase().includes('/admin');

            // Toggle indicators
            if (count > 0) {
                countBadge.text(count).removeClass('hidden');
                bell.addClass('animate-swing text-[#E8A020]');
            } else {
                countBadge.text('0').addClass('hidden');
                bell.removeClass('animate-swing text-[#E8A020]');
            }

            if (count === 0) {
                const emptyBg = isAdmin ? 'text-slate-400' : 'text-slate-500';
                list.append(`
                    <div class="p-6 text-center flex flex-col items-center justify-center gap-2">
                        <span class="material-symbols-outlined text-[32px] opacity-40">notifications_off</span>
                        <p class="text-xs font-bold ${emptyBg}">${isArabic ? 'لا توجد إشعارات غير مقروءة' : 'No unread notifications'}</p>
                    </div>
                `);
                return;
            }

            notifications.forEach(n => {
                const bgClass = n.isRead ? (isAdmin ? 'bg-white' : 'bg-transparent') : (isAdmin ? 'bg-indigo-50/40' : 'bg-white/5');
                const textClass = isAdmin ? 'text-slate-800' : 'text-slate-100';
                const subText = isAdmin ? 'text-slate-500 font-medium' : 'text-slate-400 font-normal';
                const titleHover = isAdmin ? 'group-hover:text-indigo-600' : 'group-hover:text-[#E8A020]';
                const dot = n.isRead ? '' : `<span class="w-2 h-2 rounded-full bg-[#E8A020] block shrink-0"></span>`;
                const iconClass = isAdmin ? 'bg-indigo-50 border border-indigo-100 text-indigo-600' : 'bg-white/5 border border-white/10 text-[#E8A020]';
                
                const href = n.actionUrl ? `href="${n.actionUrl}"` : 'href="javascript:void(0)"';
                const cursor = n.actionUrl ? 'cursor-pointer' : 'cursor-default';

                let icon = 'notifications';
                const tl = (n.title || '').toLowerCase();
                const ml = (n.message || '').toLowerCase();
                if (tl.includes('طلب') || tl.includes('order') || ml.includes('طلب') || ml.includes('order')) icon = 'shopping_cart';
                else if (tl.includes('توصيل') || tl.includes('delivery')) icon = 'local_shipping';

                list.append(`
                    <a ${href} onclick="window.NotificationService.markAsRead(${n.id}, event, '${n.actionUrl || ''}')"
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

            const seeAllBg = isAdmin ? 'bg-slate-50 border-t border-slate-100 hover:bg-slate-100' : 'bg-[#161d2d]/90 border-t border-white/5 hover:bg-[#161d2d]';
            const seeAllText = isAdmin ? 'text-indigo-600' : 'text-[#E8A020]';
            list.append(`
                <div class="p-3 text-center ${seeAllBg} transition-colors">
                    <a href="/Notification/Index" class="text-xs font-black ${seeAllText} tracking-wider hover:underline block w-full">${isArabic ? 'رؤية الكل' : 'See All'}</a>
                </div>
            `);
        }
    }

    // Export globally
    window.NotificationService = new NotificationService();

})(window);
