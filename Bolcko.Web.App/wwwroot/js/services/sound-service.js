/**
 * BLOCKO Sound Service
 * Handles notification sound playback only when real notifications arrive.
 * Completely silent on user interactions/clicks.
 */
(function (window) {
    'use strict';

    function getAudioElement() {
        var audio = document.getElementById('notification-sound');
        if (!audio) {
            audio = document.createElement('audio');
            audio.id = 'notification-sound';
            audio.src = '/sounds/default-notification.mp3';
            audio.preload = 'none';
            document.body.appendChild(audio);
        }
        return audio;
    }

    // Safely plays the notification sound ONLY when explicitly called by notification receivers
    window.playNotificationSound = function () {
        var audio = getAudioElement();
        if (!audio) return;

        // Respect backend / user setting to disable sound
        if (audio.hasAttribute('data-sound-disabled') || audio.getAttribute('data-sound-disabled') === 'true') {
            console.log('[SoundService] Sound is disabled by setting.');
            return;
        }

        try {
            audio.currentTime = 0;
            audio.volume = 1;
            audio.muted = false;
            
            var promise = audio.play();
            if (promise !== undefined) {
                promise.catch(function (err) {
                    console.warn('[SoundService] Playback notice (waiting for user gesture):', err.message);
                });
            }
        } catch (e) {
            console.error('[SoundService] Playback error:', e);
        }
    };

})(window);
