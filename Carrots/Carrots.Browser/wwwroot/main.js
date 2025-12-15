import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

globalThis.addEventListener('error', (e) => {
    console.error('window error:', e.error ?? e.message ?? e);
});

globalThis.addEventListener('unhandledrejection', (e) => {
    console.error('unhandled rejection:', e.reason ?? e);
});

console.log('boot info:', {
    href: globalThis.location?.href,
    baseURI: globalThis.document?.baseURI,
    crossOriginIsolated: globalThis.crossOriginIsolated,
    sharedArrayBuffer: typeof globalThis.SharedArrayBuffer,
});

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
    .withDiagnosticTracing(true)
    .withApplicationArgumentsFromQuery()
    .create();

// Export functions for JSImport AFTER dotnet runtime is created
globalThis.playAudio = playAudio;
globalThis.stopAudio = stopAudio;
console.log('Audio functions exported to globalThis');

const config = dotnetRuntime.getConfig();

try {
    await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
}
catch (err) {
    console.error('runMain failed:', err);
    throw err;
}
