/**
 * BLOCKO Shared UI Initialize scripts
 * Contains ONLY global lightweight UI initializations (Dropdown toggles, event listeners).
 * Decoupled from core business services.
 */
document.addEventListener("DOMContentLoaded", function () {
    // 1. Notification Dropdown toggle
    const bellBtn = document.getElementById('notification-bell-btn');
    const menu = document.getElementById('notification-menu');

    if (bellBtn && menu) {
        bellBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            if (menu.classList.contains('hidden')) {
                menu.classList.remove('hidden');
                setTimeout(() => {
                    menu.classList.remove('opacity-0', 'translate-y-1');
                    menu.classList.add('opacity-100', 'translate-y-0');
                }, 50);
                
                // Fetch notifications on opening
                if (window.NotificationService && typeof window.NotificationService.fetchNotifications === 'function') {
                    window.NotificationService.fetchNotifications();
                }
            } else {
                menu.classList.remove('opacity-100', 'translate-y-0');
                menu.classList.add('opacity-0', 'translate-y-1');
                setTimeout(() => menu.classList.add('hidden'), 200);
            }
        });

        // Close on clicking outside
        document.addEventListener('click', function (e) {
            if (!menu.contains(e.target) && !bellBtn.contains(e.target)) {
                menu.classList.remove('opacity-100', 'translate-y-0');
                menu.classList.add('opacity-0', 'translate-y-1');
                setTimeout(() => menu.classList.add('hidden'), 200);
            }
        });
    }

    // Trigger initial notification fetch silently
    if (window.NotificationService && typeof window.NotificationService.fetchNotifications === 'function') {
        window.NotificationService.fetchNotifications();
    }
});