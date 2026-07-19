/**
 * BLOCKO Sound Service (SOLID)
 * Simple and highly compatible audio play mechanisms for notifications.
 */
(Object.defineProperty(window, 'playNotificationSound', {
    value: function () {
        // Fallback sound URL
        var soundUrl = "/sounds/default-notification.wav";
        
        // Find existing audio element or create a new one dynamically
        var audio = document.getElementById('notification-sound-el');
        if (!audio) {
            audio = document.createElement('audio');
            audio.id = 'notification-sound-el';
            audio.src = soundUrl;
            audio.preload = 'auto';
            document.body.appendChild(audio);
        }

        try {
            audio.currentTime = 0;
            var playPromise = audio.play();
            if (playPromise !== undefined) {
                playPromise.catch(function (error) {
                    console.warn('[SoundService] Autoplay prevented. Sound will play upon next user interaction.', error);
                });
            }
        } catch (e) {
            console.error('[SoundService] Audio playback failed:', e);
        }
    },
    writable: true,
    configurable: true
}));
