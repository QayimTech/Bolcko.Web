/**
 * Block-O Global Modal & Toast Component
 * Unified, enterprise-grade modal dialogs and toasts using Tailwind CSS.
 */
(function (window) {
    'use strict';

    function ensureContainers() {
        if (!document.getElementById('blocko-modal-container')) {
            const modalContainer = document.createElement('div');
            modalContainer.id = 'blocko-modal-container';
            modalContainer.className = 'fixed inset-0 z-[999999] hidden items-center justify-center p-4 bg-slate-900/70 backdrop-blur-sm transition-all duration-300';
            modalContainer.setAttribute('dir', 'rtl');
            document.body.appendChild(modalContainer);
        }

        if (!document.getElementById('blocko-toast-container')) {
            const toastContainer = document.createElement('div');
            toastContainer.id = 'blocko-toast-container';
            toastContainer.className = 'fixed top-5 left-1/2 -translate-x-1/2 sm:left-6 sm:translate-x-0 z-[1000000] flex flex-col gap-3 pointer-events-none max-w-md w-full px-4';
            toastContainer.setAttribute('dir', 'rtl');
            document.body.appendChild(toastContainer);
        }
    }

    const icons = {
        success: `<div class="w-14 h-14 rounded-2xl bg-emerald-500/10 text-emerald-500 flex items-center justify-center mx-auto mb-4 ring-8 ring-emerald-500/5">
            <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"></path></svg>
        </div>`,
        error: `<div class="w-14 h-14 rounded-2xl bg-rose-500/10 text-rose-500 flex items-center justify-center mx-auto mb-4 ring-8 ring-rose-500/5">
            <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"></path></svg>
        </div>`,
        warning: `<div class="w-14 h-14 rounded-2xl bg-amber-500/10 text-amber-500 flex items-center justify-center mx-auto mb-4 ring-8 ring-amber-500/5">
            <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
        </div>`,
        info: `<div class="w-14 h-14 rounded-2xl bg-blue-500/10 text-blue-500 flex items-center justify-center mx-auto mb-4 ring-8 ring-blue-500/5">
            <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
        </div>`
    };

    class BlockoModalService {
        show(options = {}) {
            ensureContainers();
            const container = document.getElementById('blocko-modal-container');
            
            const type = options.type || options.icon || 'info';
            const title = options.title || (type === 'success' ? 'عملية ناجحة' : type === 'error' ? 'تنبيه' : type === 'warning' ? 'تحذير' : 'إشعار');
            const message = options.message || '';
            const confirmText = options.confirmText || 'حسناً';
            const cancelText = options.cancelText || 'إلغاء';
            const showCancel = options.showCancel || Boolean(options.cancelText);
            const isPrompt = options.isPrompt || false;
            const defaultValue = options.defaultValue || '';
            const placeholder = options.placeholder || 'أدخل القيمة هنا...';

            return new Promise((resolve) => {
                const iconHtml = icons[type] || icons.info;
                const promptInputHtml = isPrompt ? `
                    <div class="mt-4 mb-2">
                        <input type="text" id="blocko-modal-prompt-input" value="${defaultValue}" placeholder="${placeholder}"
                               class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 outline-none transition-all text-sm text-right" />
                    </div>
                ` : '';

                const cancelBtnHtml = (showCancel || isPrompt) ? `
                    <button type="button" id="blocko-modal-cancel-btn"
                            class="flex-1 px-5 py-2.5 text-sm font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition-all border border-slate-200 dark:border-slate-700">
                        ${cancelText}
                    </button>
                ` : '';

                const confirmBtnClass = type === 'error' 
                    ? 'bg-rose-600 hover:bg-rose-700 text-white shadow-lg shadow-rose-600/20' 
                    : type === 'warning'
                    ? 'bg-amber-600 hover:bg-amber-700 text-white shadow-lg shadow-amber-600/20'
                    : 'bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-slate-900 font-black shadow-lg shadow-amber-500/20';

                container.innerHTML = `
                    <div class="relative w-full max-w-md bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl shadow-2xl p-6 transform transition-all scale-95 opacity-0 text-center" style="animation: modalEnter 0.25s ease-out forwards;">
                        ${iconHtml}
                        <h3 class="text-lg font-black text-slate-900 dark:text-white mb-2">${title}</h3>
                        <div class="text-sm font-medium text-slate-600 dark:text-slate-300 leading-relaxed max-h-60 overflow-y-auto px-1">
                            ${message}
                        </div>
                        ${promptInputHtml}
                        <div class="mt-6 flex items-center gap-3 pt-2">
                            ${cancelBtnHtml}
                            <button type="button" id="blocko-modal-confirm-btn"
                                    class="flex-1 px-5 py-2.5 text-sm font-bold rounded-xl transition-all cursor-pointer ${confirmBtnClass}">
                                ${confirmText}
                            </button>
                        </div>
                    </div>
                `;

                container.classList.remove('hidden');
                container.classList.add('flex');

                const inputEl = document.getElementById('blocko-modal-prompt-input');
                if (inputEl) {
                    setTimeout(() => inputEl.focus(), 50);
                    inputEl.addEventListener('keydown', (e) => {
                        if (e.key === 'Enter') confirmAction();
                        if (e.key === 'Escape') cancelAction();
                    });
                }

                function closeModal() {
                    container.classList.add('hidden');
                    container.classList.remove('flex');
                    container.innerHTML = '';
                }

                function confirmAction() {
                    const val = inputEl ? inputEl.value : true;
                    closeModal();
                    if (typeof options.onConfirm === 'function') options.onConfirm(val);
                    resolve(val);
                }

                function cancelAction() {
                    closeModal();
                    if (typeof options.onCancel === 'function') options.onCancel(null);
                    resolve(isPrompt ? null : false);
                }

                document.getElementById('blocko-modal-confirm-btn')?.addEventListener('click', confirmAction);
                document.getElementById('blocko-modal-cancel-btn')?.addEventListener('click', cancelAction);
            });
        }

        alert(message, title = 'تنبيه', type = 'info') {
            if (typeof message === 'object' && message !== null) {
                return this.show(message);
            }
            return this.show({ title, message, type });
        }

        confirm(message, title = 'تأكيد الإجراء') {
            if (typeof message === 'object' && message !== null) {
                return this.show({ showCancel: true, type: 'warning', ...message });
            }
            return this.show({ title, message, showCancel: true, type: 'warning' });
        }

        prompt(message, defaultValue = '', title = 'إدخال بيانات') {
            return this.show({ title, message, isPrompt: true, defaultValue, type: 'info' });
        }

        toast(message, type = 'info', duration = 3500) {
            ensureContainers();
            const container = document.getElementById('blocko-toast-container');

            const toast = document.createElement('div');
            toast.className = 'pointer-events-auto flex items-center gap-3 px-4 py-3 rounded-2xl shadow-xl border bg-white dark:bg-slate-900 transition-all duration-300 transform translate-y-2 opacity-0 text-sm font-bold';
            
            let borderClass = 'border-slate-200 dark:border-slate-800 text-slate-800 dark:text-white';
            let icon = `<svg class="w-5 h-5 text-blue-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`;

            if (type === 'success') {
                borderClass = 'border-emerald-500/20 text-emerald-700 dark:text-emerald-400 bg-emerald-50/90 dark:bg-emerald-950/40';
                icon = `<svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`;
            } else if (type === 'error') {
                borderClass = 'border-rose-500/20 text-rose-700 dark:text-rose-400 bg-rose-50/90 dark:bg-rose-950/40';
                icon = `<svg class="w-5 h-5 text-rose-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>`;
            } else if (type === 'warning') {
                borderClass = 'border-amber-500/20 text-amber-700 dark:text-amber-400 bg-amber-50/90 dark:bg-amber-950/40';
                icon = `<svg class="w-5 h-5 text-amber-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>`;
            }

            toast.className += ` ${borderClass}`;
            toast.innerHTML = `
                ${icon}
                <span class="flex-1">${message}</span>
                <button type="button" class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
                </button>
            `;

            toast.querySelector('button').addEventListener('click', () => removeToast(toast));
            container.appendChild(toast);

            requestAnimationFrame(() => {
                toast.classList.remove('translate-y-2', 'opacity-0');
                toast.classList.add('translate-y-0', 'opacity-100');
            });

            const timer = setTimeout(() => {
                removeToast(toast);
            }, duration);

            function removeToast(el) {
                clearTimeout(timer);
                el.classList.add('opacity-0', '-translate-y-2');
                setTimeout(() => el.remove(), 300);
            }
        }
    }

    const instance = new BlockoModalService();
    window.BlockoModal = instance;
    window.showBlockoModal = (opts) => instance.show(opts);
    window.showBlockoToast = (msg, type, duration) => instance.toast(msg, type, duration);

    window._nativeAlert = window.alert;
    window.alert = function (message) {
        instance.alert(message, 'تنبيه النظام', 'info');
    };

    if (typeof document !== 'undefined') {
        const style = document.createElement('style');
        style.innerHTML = `
            @keyframes modalEnter {
                from { opacity: 0; transform: scale(0.95); }
                to { opacity: 1; transform: scale(1); }
            }
        `;
        document.head.appendChild(style);
    }
})(window);
