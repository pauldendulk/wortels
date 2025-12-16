// In local dev, browsers can keep stale cached framework assets across rebuilds.
// When that happens, you can end up with mismatched dotnet.js / dotnet.runtime.*
// which crashes with "function signature mismatch".
// We cache-bust framework assets on localhost only.
const isLocalhost = globalThis.location?.hostname === 'localhost' || globalThis.location?.hostname === '127.0.0.1';
const cacheBust = isLocalhost ? Date.now().toString() : null;

const dotnetModuleUrl = new URL('./_framework/dotnet.js', import.meta.url);
if (cacheBust) dotnetModuleUrl.searchParams.set('v', cacheBust);
const { dotnet } = await import(dotnetModuleUrl.href);

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// Audio playback functionality
let currentAudio = null;

function playAudio(audioPath) {
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
    .withResourceLoader((type, name, defaultUri) => {
        if (!cacheBust) return defaultUri;
        const url = new URL(defaultUri, globalThis.location.href);
        url.searchParams.set('v', cacheBust);
        return url.toString();
    })
    .create();

// Export functions for JSImport AFTER dotnet runtime is created
globalThis.playAudio = playAudio;
globalThis.stopAudio = stopAudio;

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
