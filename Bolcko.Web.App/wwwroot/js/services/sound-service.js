/**
 * BLOCKO Sound Service
 * Handles notification sound playback and bypasses browser autoplay restrictions.
 */
(function (window) {
    'use strict';

    var _unlocked = false;

    function getAudioElement() {
        var audio = document.getElementById('notification-sound');
        if (!audio) {
            audio = document.createElement('audio');
            audio.id = 'notification-sound';
            audio.src = '/sounds/default-notification.wav';
            audio.preload = 'auto';
            document.body.appendChild(audio);
        }
        return audio;
    }

    // Function to unlock audio context on user interaction (Required by Chrome/Safari/Firefox)
    function unlockAudio() {
        if (_unlocked) return;

        var audio = getAudioElement();
        if (audio) {
            // Play a short silent/empty sound to unlock the audio stream in the browser
            var originalVolume = audio.volume;
            var originalMute = audio.muted;
            
            audio.volume = 0;
            audio.muted = true;
            
            var promise = audio.play();
            if (promise !== undefined) {
                promise.then(function() {
                    audio.pause();
                    audio.currentTime = 0;
                    audio.volume = originalVolume;
                    audio.muted = originalMute;
                    _unlocked = true;
                    console.log('[SoundService] Audio stream unlocked successfully.');
                    
                    // Remove listeners once unlocked
                    document.removeEventListener('click', unlockAudio);
                    document.removeEventListener('touchstart', unlockAudio);
                }).catch(function(err) {
                    console.warn('[SoundService] Audio unlock deferred:', err.message);
                });
            }
        }
    }

    // Register user interaction listeners to unlock sound
    document.addEventListener('click', unlockAudio);
    document.addEventListener('touchstart', unlockAudio);

    window.playNotificationSound = function () {
        var audio = getAudioElement();
        if (!audio) return;

        // Respect backend setting to disable sound
        if (audio.hasAttribute('data-sound-disabled') || audio.getAttribute('data-sound-disabled') === 'true') {
            console.log('[SoundService] Sound is disabled by setting.');
            return;
        }

        try {
            audio.currentTime = 0;
            audio.muted = false; // Ensure it's not muted on actual play
            var promise = audio.play();
            if (promise !== undefined) {
                promise.catch(function (err) {
                    console.warn('[SoundService] Playback failed or was blocked by browser:', err.message);
                });
            }
        } catch (e) {
            console.error('[SoundService] Playback error:', e);
        }
    };

})(window);
