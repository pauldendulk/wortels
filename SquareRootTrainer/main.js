// JavaScript interop for browser audio playback

let currentAudio = null;

export function playAudio(audioPath) {
    // Stop any currently playing audio
    stopAudio();
    
    // Create and play new audio
    currentAudio = new Audio(audioPath);
    currentAudio.play().catch(err => {
        console.error('Audio playback failed:', err);
    });
    
    // Clean up when audio ends
    currentAudio.addEventListener('ended', () => {
        currentAudio = null;
    });
}

export function stopAudio() {
    if (currentAudio) {
        currentAudio.pause();
        currentAudio.currentTime = 0;
        currentAudio = null;
    }
}
