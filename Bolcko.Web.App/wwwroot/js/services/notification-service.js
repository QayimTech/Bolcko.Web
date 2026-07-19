/**
 * BLOCKO Notification Center Service
 * Decoupled static file to support browser caching.
 */
(function (window) {
    'use strict';

    class NotificationService {
        constructor() {}

        async fetchNotifications() {
            try {
                const response = await fetch('/Notification/GetRecent');
                const data = await response.json();
                if (data.success) {
                    this.updateUI(data);
                }
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
            
            const menu = document.getElementById('notification-menu');
            if (menu) {
                menu.classList.add('opacity-0', 'translate-y-1');
                menu.classList.remove('opacity-100', 'translate-y-0');
                setTimeout(() => menu.classList.add('hidden'), 200);
            }

            try {
                await fetch(`/Notification/MarkAsRead?id=${id}`, { method: 'POST' });
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
                await fetch('/Notification/MarkAllAsRead', { method: 'POST' });
                await this.fetchNotifications();
            } catch (err) {
                console.error('[NotificationService] MarkAllRead failed:', err);
            }
        }

        updateUI(data) {
            const list = document.getElementById('notification-list');
            const countBadge = document.getElementById('notification-badge');

            const isArabic = document.documentElement.dir === 'rtl' || document.documentElement.lang === 'ar';
            const notifications = data?.notifications || [];
            const count = data?.unreadCount || 0;
            const isAdmin = window.location.pathname.toLowerCase().includes('/admin');

            if (countBadge) {
                if (count > 0) {
                    countBadge.textContent = count;
                    countBadge.classList.remove('hidden');
                } else {
                    countBadge.textContent = '';
                    countBadge.classList.add('hidden');
                }
            }

            if (list) {
                list.innerHTML = '';

                if (notifications.length === 0) {
                    const emptyBg = isAdmin ? 'text-slate-400' : 'text-slate-500';
                    list.innerHTML = `
                        <div class="p-6 text-center flex flex-col items-center justify-center gap-2">
                            <span class="material-symbols-outlined text-[32px] opacity-40">notifications_off</span>
                            <p class="text-xs font-bold ${emptyBg}">${isArabic ? 'لا توجد إشعارات غير مقروءة' : 'No unread notifications'}</p>
                        </div>
                    `;
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

                    const textAlignment = isArabic ? 'text-right' : 'text-start';
                    const flexDir = isArabic ? 'flex-row-reverse' : 'flex-row';
                    const listPadding = isArabic ? 'pl-4' : 'pr-4';

                    list.innerHTML += `
                        <a ${href} onclick="window.NotificationService.markAsRead(${n.id}, event, '${n.actionUrl || ''}')"
                           class="block p-4.5 ${bgClass} ${cursor} transition-all duration-300 group border-b ${isAdmin ? 'border-slate-100' : 'border-white/5'}">
                            <div class="flex items-start gap-4 ${flexDir}">
                                <div class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0 transition-all duration-300 ${iconClass} group-hover:scale-105">
                                    <span class="material-symbols-outlined text-[20px]">${icon}</span>
                                </div>
                                <div class="flex-grow min-w-0 ${textAlignment}">
                                    <h4 class="text-xs font-black ${textClass} mb-1 ${titleHover} transition-colors truncate">${n.title || ''}</h4>
                                    <p class="text-[11px] ${subText} leading-relaxed font-medium break-words">${n.message || ''}</p>
                                    <span class="text-[9px] font-bold text-slate-500 mt-2 block">${n.createdAt || ''}</span>
                                </div>
                                <div class="self-center">
                                    ${dot}
                                </div>
                            </div>
                        </a>`;
                });

                const seeAllBg = isAdmin ? 'bg-slate-50 border-t border-slate-100 hover:bg-slate-100' : 'bg-[#161d2d]/90 border-t border-white/5 hover:bg-[#161d2d]';
                const seeAllText = isAdmin ? 'text-indigo-600' : 'text-[#E8A020]';
                list.innerHTML += `
                    <div class="p-3 text-center ${seeAllBg} transition-colors">
                        <a href="/Notification/Index" class="text-xs font-black ${seeAllText} tracking-wider hover:underline block w-full">${isArabic ? 'رؤية الكل' : 'See All'}</a>
                    </div>
                `;
            }
        }
        injectNotification(notification) {
            const list = document.getElementById('notification-list');
            const countBadge = document.getElementById('notification-badge');
            const isArabic = document.documentElement.dir === 'rtl' || document.documentElement.lang === 'ar';
            const isAdmin = window.location.pathname.toLowerCase().includes('/admin');

            // 1. Update Badge Count (+1)
            if (countBadge) {
                let currentCount = parseInt(countBadge.textContent || '0', 10);
                currentCount = isNaN(currentCount) ? 0 : currentCount;
                currentCount += 1;
                countBadge.textContent = currentCount;
                countBadge.classList.remove('hidden');
            }

            // 2. Format & Inject dynamic notification HTML directly into the list (without REST request)
            if (list) {
                // Remove empty notifications state message if it exists
                const emptyState = list.querySelector('.p-6');
                if (emptyState) {
                    list.innerHTML = '';
                }

                const bgClass = isAdmin ? 'bg-indigo-50/40' : 'bg-white/5';
                const textClass = isAdmin ? 'text-slate-800' : 'text-slate-100';
                const subText = isAdmin ? 'text-slate-500 font-medium' : 'text-slate-400 font-normal';
                const titleHover = isAdmin ? 'group-hover:text-indigo-600' : 'group-hover:text-[#E8A020]';
                const dot = `<span class="w-2 h-2 rounded-full bg-[#E8A020] block shrink-0"></span>`;
                const iconClass = isAdmin ? 'bg-indigo-50 border border-indigo-100 text-indigo-600' : 'bg-white/5 border border-white/10 text-[#E8A020]';
                
                const href = notification.ActionUrl || notification.actionUrl ? `href="${notification.ActionUrl || notification.actionUrl}"` : 'href="javascript:void(0)"';
                const cursor = notification.ActionUrl || notification.actionUrl ? 'cursor-pointer' : 'cursor-default';

                let icon = 'notifications';
                const tl = (notification.Title || notification.title || '').toLowerCase();
                const ml = (notification.Message || notification.message || '').toLowerCase();
                if (tl.includes('طلب') || tl.includes('order') || ml.includes('طلب') || ml.includes('order')) icon = 'shopping_cart';
                else if (tl.includes('توصيل') || tl.includes('delivery')) icon = 'local_shipping';

                const textAlignment = isArabic ? 'text-right' : 'text-start';
                const flexDir = isArabic ? 'flex-row-reverse' : 'flex-row';

                // Format time dynamically for fresh notification
                const timeString = isArabic ? 'الآن' : 'Just now';

                const itemHtml = `
                    <a ${href} onclick="window.NotificationService.markAsRead(${notification.Id || notification.id}, event, '${notification.ActionUrl || notification.actionUrl || ''}')"
                       class="block p-4.5 ${bgClass} ${cursor} transition-all duration-300 group border-b ${isAdmin ? 'border-slate-100' : 'border-white/5'}">
                        <div class="flex items-start gap-4 ${flexDir}">
                            <div class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0 transition-all duration-300 ${iconClass} group-hover:scale-105">
                                <span class="material-symbols-outlined text-[20px]">${icon}</span>
                            </div>
                            <div class="flex-grow min-w-0 ${textAlignment}">
                                <h4 class="text-xs font-black ${textClass} mb-1 ${titleHover} transition-colors truncate">${notification.Title || notification.title || ''}</h4>
                                <p class="text-[11px] ${subText} leading-relaxed font-medium break-words">${notification.Message || notification.message || ''}</p>
                                <span class="text-[9px] font-bold text-slate-500 mt-2 block">${timeString}</span>
                            </div>
                            <div class="self-center">
                                ${dot}
                            </div>
                        </div>
                    </a>`;

                // Prepend new item to the list
                list.insertAdjacentHTML('afterbegin', itemHtml);
            }
        }
    }

    window.NotificationService = new NotificationService();

    // Helper functions for header
    window.toggleNotificationDropdown = function(e) {
        const menu = document.getElementById('notification-menu');
        if (menu) {
            if (menu.classList.contains('hidden')) {
                menu.classList.remove('hidden');
                setTimeout(() => {
                    menu.classList.remove('opacity-0', 'translate-y-1');
                    menu.classList.add('opacity-100', 'translate-y-0');
                }, 50);
                
                if (window.NotificationService && typeof window.NotificationService.fetchNotifications === 'function') {
                    window.NotificationService.fetchNotifications();
                }
            } else {
                menu.classList.remove('opacity-100', 'translate-y-0');
                menu.classList.add('opacity-0', 'translate-y-1');
                setTimeout(() => menu.classList.add('hidden'), 200);
            }
        }
    };

    window.markAllNotificationsAsRead = function() {
        if (window.NotificationService && typeof window.NotificationService.markAllAsRead === 'function') {
            window.NotificationService.markAllAsRead();
        }
    };

})(window);
