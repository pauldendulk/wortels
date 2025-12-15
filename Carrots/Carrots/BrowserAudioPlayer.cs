#if BROWSER
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Carrots;

/// <summary>
/// Browser implementation of audio playback using HTML5 Audio via JavaScript interop.
/// </summary>
[SupportedOSPlatform("browser")]
public partial class BrowserAudioPlayer : IAudioPlayer
{
    private readonly string _audioBasePath;
    
    /// <summary>
    /// Creates a new browser audio player.
    /// </summary>
    /// <param name="audioBasePath">Base path to the audio files directory</param>
    public BrowserAudioPlayer(string audioBasePath)
    {
        _audioBasePath = audioBasePath;
        Console.WriteLine($"BrowserAudioPlayer created with base path: {audioBasePath}");
    }
    
    /// <summary>
    /// Plays an audio file and waits for it to complete.
    /// </summary>
    /// <param name="fileName">Name of the audio file (e.g., "question_4.wav")</param>
    /// <param name="languageCode">Language code for the audio (e.g., "en-US")</param>
    /// <param name="cancellationToken">Token to cancel playback</param>
    public async Task PlayAsync(string fileName, string languageCode, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        try
        {
            var audioPath = $"{_audioBasePath}/{languageCode}/{fileName}";
            Console.WriteLine($"BrowserAudioPlayer: Attempting to play {audioPath}");
            
            // Use JavaScript interop to play audio
            await PlayAudioJS(audioPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            StopAudioJS();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Browser audio playback error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    public void Stop()
    {
        StopAudioJS();
    }
    
    private async Task PlayAudioJS(string audioPath, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        
        // Register cancellation
        using var registration = cancellationToken.Register(() => 
        {
            StopAudioJS();
            tcs.TrySetCanceled();
        });
        
        // Call JavaScript to play audio and wait for completion
        PlayAudio(audioPath);
        
        // Wait for audio to complete (JavaScript will signal completion)
        // For now, we'll estimate duration - in production, JavaScript should call back
        await Task.Delay(5000, cancellationToken); // Rough estimate
        
        tcs.TrySetResult();
        await tcs.Task;
    }
    
    [JSImport("globalThis.playAudio")]
    private static partial void PlayAudio(string audioPath);
    
    [JSImport("globalThis.stopAudio")]
    private static partial void StopAudioJS();
    
    public void Dispose()
    {
        Stop();
    }
}
#endif
