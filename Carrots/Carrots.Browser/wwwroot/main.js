import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// Audio playback functionality
let currentAudio = null;

function playAudio(audioPath) {
    console.log('JavaScript playAudio called with path:', audioPath);
    
    // Stop any currently playing audio
    stopAudio();
    
    // Create and play new audio
    currentAudio = new Audio(audioPath);
    console.log('Created Audio element, attempting to play...');
    
    currentAudio.play().catch(err => {
        console.error('Audio playback failed:', err);
    });
    
    // Clean up when audio ends
    currentAudio.addEventListener('ended', () => {
        console.log('Audio playback ended');
        currentAudio = null;
    });
}

function stopAudio() {
    if (currentAudio) {
        currentAudio.pause();
        currentAudio.currentTime = 0;
        currentAudio = null;
    }
}

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

// Export functions for JSImport AFTER dotnet runtime is created
globalThis.playAudio = playAudio;
globalThis.stopAudio = stopAudio;
console.log('Audio functions exported to globalThis');

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
