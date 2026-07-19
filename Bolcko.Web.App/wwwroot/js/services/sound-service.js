/**
 * BLOCKO Sound Service
 * Plays notification sound using the <audio> element injected by the Layout.
 * The Layout places: <audio id="notification-sound" src="..." preload="auto">
 * Falls back to a hardcoded URL if no element found.
 */
(function (window) {
    'use strict';

    window.playNotificationSound = function () {
        // The Layout renders <audio id="notification-sound"> when sound is enabled.
        var audio = document.getElementById('notification-sound');

        if (!audio) {
            // Fallback: create element dynamically
            audio = document.createElement('audio');
            audio.id = 'notification-sound';
            audio.src = '/sounds/default-notification.wav';
            audio.preload = 'auto';
            document.body.appendChild(audio);
        }

        // Respect setting to disable sound if configured in DB
        if (audio.hasAttribute('data-sound-disabled') || audio.getAttribute('data-sound-disabled') === 'true') {
            console.log('[SoundService] Sound is disabled via configuration settings.');
            return;
        }

        try {
            audio.currentTime = 0;
            var promise = audio.play();
            if (promise !== undefined) {
                promise.catch(function (err) {
                    console.warn('[SoundService] Autoplay blocked by browser (needs user interaction first):', err.message);
                });
            }
        } catch (e) {
            console.error('[SoundService] playback error:', e);
        }
    };

})(window);
