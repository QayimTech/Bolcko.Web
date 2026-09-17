/**
 * BLOCKO UI Modal Component
 * Compatibility layer delegating to global BlockoModal engine.
 */
(function (window) {
    'use strict';
    // If blocko-modal.js is already loaded, do not overwrite with legacy class
    if (window.BlockoModal && typeof window.BlockoModal.alert === 'function') {
        return;
    }
})(window);
