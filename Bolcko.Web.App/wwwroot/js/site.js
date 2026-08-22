/**
 * BLOCKO Shared UI Initialize scripts
 * Contains ONLY global lightweight UI initializations (Sidebar toggle, Dropdown toggles, Toast notifications).
 * Decoupled from core business services.
 */
document.addEventListener("DOMContentLoaded", function () {
    // 1. Sidebar Toggle & Mobile Backdrop Logic
    const sidebarToggleBtn = document.getElementById('sidebar-toggle');
    const closeSidebarBtn = document.getElementById('close-sidebar');
    const sidebarOverlay = document.getElementById('sidebar-overlay');
    const adminSidebar = document.getElementById('admin-sidebar');

    // Restore saved sidebar collapsed state on desktop
    const isCollapsed = localStorage.getItem('blocko_sidebar_collapsed') === 'true';
    if (isCollapsed && window.innerWidth >= 1024) {
        document.body.classList.add('sidebar-collapsed');
    }

    if (sidebarToggleBtn) {
        sidebarToggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            if (window.innerWidth < 1024) {
                // Mobile View: Toggle overlay drawer
                if (adminSidebar) {
                    const isHidden = adminSidebar.classList.contains('-translate-x-full') || adminSidebar.classList.contains('rtl:translate-x-full');
                    if (isHidden) {
                        openMobileSidebar();
                    } else {
                        closeMobileSidebar();
                    }
                }
            } else {
                // Desktop View: Toggle collapsed state
                document.body.classList.toggle('sidebar-collapsed');
                const nowCollapsed = document.body.classList.contains('sidebar-collapsed');
                localStorage.setItem('blocko_sidebar_collapsed', nowCollapsed);
            }
        });
    }

    if (closeSidebarBtn) {
        closeSidebarBtn.addEventListener('click', closeMobileSidebar);
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', closeMobileSidebar);
    }

    function openMobileSidebar() {
        if (!adminSidebar || !sidebarOverlay) return;
        sidebarOverlay.classList.remove('hidden');
        setTimeout(() => sidebarOverlay.classList.remove('opacity-0'), 10);
        adminSidebar.classList.remove('-translate-x-full', 'rtl:translate-x-full');
        adminSidebar.classList.add('translate-x-0');
    }

    function closeMobileSidebar() {
        if (!adminSidebar || !sidebarOverlay) return;
        adminSidebar.classList.remove('translate-x-0');
        adminSidebar.classList.add('-translate-x-full', 'rtl:translate-x-full');
        sidebarOverlay.classList.add('opacity-0');
        setTimeout(() => sidebarOverlay.classList.add('hidden'), 300);
    }

    // 2. Notification Dropdown toggle
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

/**
 * Global Tailwind Toast Notification helper
 */
window.showToast = function (message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'fixed bottom-6 start-6 z-[99999] flex flex-col gap-3 pointer-events-none';
        document.body.appendChild(container);
    }

    const icon = type === 'success' ? 'check_circle' : type === 'error' ? 'error' : 'info';
    const borderColor = type === 'success' ? 'border-s-emerald-500' : type === 'error' ? 'border-s-rose-500' : 'border-s-[#E8A020]';
    const iconColor = type === 'success' ? 'text-emerald-500' : type === 'error' ? 'text-rose-500' : 'text-[#E8A020]';

    const toast = document.createElement('div');
    toast.className = `pointer-events-auto bg-[#0d111a]/90 backdrop-blur-md border border-slate-700 text-white px-5 py-4 rounded-2xl shadow-2xl flex items-center gap-3.5 transform translate-y-4 opacity-0 transition-all duration-300 ease-out select-none border-s-4 ${borderColor}`;
    toast.style.minWidth = '300px';
    toast.style.maxWidth = '420px';

    toast.innerHTML = `
        <div class="w-9 h-9 rounded-xl bg-white/10 flex items-center justify-center ${iconColor} shrink-0">
            <span class="material-symbols-outlined text-[22px]">${icon}</span>
        </div>
        <div class="flex-grow text-start">
            <p class="text-xs text-slate-100 font-bold leading-relaxed">${message}</p>
        </div>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.remove('translate-y-4', 'opacity-0');
        toast.classList.add('translate-y-0', 'opacity-100');
    }, 10);

    setTimeout(() => {
        toast.classList.remove('translate-y-0', 'opacity-100');
        toast.classList.add('translate-y-4', 'opacity-0');
        setTimeout(() => toast.remove(), 400);
    }, 4500);
};