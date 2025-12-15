import { dotnet } from '../AppBundle/_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

// Export audio functions for Avalonia to use
export function playAudio(audioPath) {
    const audio = new Audio(audioPath);
    return audio.play();
}

export function stopAudio() {
    // This would need to track the currently playing audio
}
