/**
 * BLOCKO Sound Service (SOLID - Single Responsibility)
 * Handles notifications audio buffering and Web Audio API playback mechanisms.
 */
(function (window) {
    'use strict';

    var _audioCtx = null;
    var _soundBuffer = null;
    var _soundUrl = null;
    var _unlocked = false;

    function detectSoundUrl() {
        var el = document.getElementById('notification-sound') || document.getElementById('shop-notification-sound');
        return el ? el.getAttribute('src') : '/sounds/default-notification.wav';
    }

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
                    console.log('[SoundService] Sound preloaded:', url);
                })
                .catch(function (e) { console.warn('[SoundService] Preload failed:', e); });
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

    function unlockAudio() {
        if (_unlocked) return;
        _unlocked = true;
        _getAudioContext().then(function (ctx) {
            if (!ctx) return;
            var buf = ctx.createBuffer(1, 1, 22050);
            var src = ctx.createBufferSource();
            src.buffer = buf;
            src.connect(ctx.destination);
            src.start(0);
            preloadSound(detectSoundUrl());
        });
    }

    // Register audio unlock triggers
    document.addEventListener('click', unlockAudio, { once: false, passive: true });
    document.addEventListener('keydown', unlockAudio, { once: false, passive: true });
    document.addEventListener('touchstart', unlockAudio, { once: false, passive: true });

    window.addEventListener('load', function () {
        preloadSound(detectSoundUrl());
    });

    // Public API
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
            var el = document.getElementById('notification-sound') || document.getElementById('shop-notification-sound');
            if (el) {
                el.currentTime = 0;
                el.play().catch(function () {});
            }
        });
    };

})(window);
