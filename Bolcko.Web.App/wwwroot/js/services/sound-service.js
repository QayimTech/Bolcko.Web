/**
 * BLOCKO Sound Service (SOLID - Single Responsibility)
 * Handles notifications audio buffering and Web Audio API playback mechanisms.
 * Features built-in synthesizer fallback to ensure reliable sounds without network requests.
 */
(function (window) {
    'use strict';

    var _audioCtx = null;
    var _soundBuffer = null;
    var _soundUrl = null;
    var _unlocked = false;

    function detectSoundUrl() {
        var el = document.getElementById('notification-sound') || document.getElementById('shop-notification-sound');
        return el ? el.getAttribute('src') : null;
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
                    console.log('[SoundService] Custom sound preloaded:', url);
                })
                .catch(function (e) {
                    console.warn('[SoundService] Custom sound preload failed, using Web Audio Synthesizer fallback.', e);
                });
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
            
            var url = detectSoundUrl();
            if (url) {
                preloadSound(url);
            }
        });
    }

    // Synthesize a clean, professional notification bell (ding) sound programmatically
    function playSynthNotificationSound(ctx) {
        var now = ctx.currentTime;
        
        // Master Gain
        var masterGain = ctx.createGain();
        masterGain.gain.setValueAtTime(0, now);
        masterGain.gain.linearRampToValueAtTime(0.3, now + 0.02);
        masterGain.gain.exponentialRampToValueAtTime(0.001, now + 0.8);
        masterGain.connect(ctx.destination);

        // Core Bell Frequency (Oscillator 1)
        var osc1 = ctx.createOscillator();
        osc1.type = 'sine';
        osc1.frequency.setValueAtTime(880, now); // A5 note
        osc1.connect(masterGain);

        // Harmonic Frequency (Oscillator 2)
        var osc2 = ctx.createOscillator();
        osc2.type = 'sine';
        osc2.frequency.setValueAtTime(1320, now); // E6 note (harmonic fifth)
        
        var harmonicGain = ctx.createGain();
        harmonicGain.gain.setValueAtTime(0.12, now);
        harmonicGain.gain.exponentialRampToValueAtTime(0.001, now + 0.4);
        
        osc2.connect(harmonicGain);
        harmonicGain.connect(ctx.destination);

        // Start and Stop
        osc1.start(now);
        osc1.stop(now + 0.85);
        osc2.start(now);
        osc2.stop(now + 0.45);
    }

    // Register audio unlock triggers
    document.addEventListener('click', unlockAudio, { once: false, passive: true });
    document.addEventListener('keydown', unlockAudio, { once: false, passive: true });
    document.addEventListener('touchstart', unlockAudio, { once: false, passive: true });

    window.addEventListener('load', function () {
        var url = detectSoundUrl();
        if (url) {
            preloadSound(url);
        }
    });

    // Public API
    window.playNotificationSound = function () {
        _getAudioContext().then(function (ctx) {
            // 1. Try playing custom preloaded buffer if available
            if (ctx && _soundBuffer) {
                try {
                    var src = ctx.createBufferSource();
                    src.buffer = _soundBuffer;
                    src.connect(ctx.destination);
                    src.start(0);
                    return;
                } catch (e) {
                    console.warn('[SoundService] Buffer play failed, falling back...', e);
                }
            }
            
            // 2. Try Web Audio Synth if context is active
            if (ctx && ctx.state === 'running') {
                try {
                    playSynthNotificationSound(ctx);
                    return;
                } catch (e) {
                    console.warn('[SoundService] Synth failed, falling back to HTMLAudio...', e);
                }
            }

            // 3. Fallback: Use standard HTML5 Audio element to bypass AudioContext state limits
            var el = document.getElementById('notification-sound') || document.getElementById('shop-notification-sound');
            if (el) {
                try {
                    el.currentTime = 0;
                    var playPromise = el.play();
                    if (playPromise !== undefined) {
                        playPromise.catch(function (error) {
                            console.warn('[SoundService] HTMLAudio play blocked by browser autoplay policy:', error);
                        });
                    }
                } catch (err) {
                    console.warn('[SoundService] HTMLAudio fallback failed:', err);
                }
            } else {
                // If no element exists, create an ad-hoc one with the default ding synth
                if (ctx) {
                    playSynthNotificationSound(ctx);
                }
            }
        });
    };

})(window);
