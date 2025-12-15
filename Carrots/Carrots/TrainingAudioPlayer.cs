#if !BROWSER
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Carrots;

/// <summary>
/// Manages audio playback for the training session using NAudio.
/// </summary>
[SupportedOSPlatform("windows")]
public class TrainingAudioPlayer : IAudioPlayer
{
    private readonly string _audioBasePath;
    private IWavePlayer? _waveOutDevice;
    private AudioFileReader? _audioFileReader;
    private readonly object _lockObject = new();
    
    /// <summary>
    /// Creates a new audio player.
    /// </summary>
    /// <param name="audioBasePath">Base path to the audio files directory</param>
    public TrainingAudioPlayer(string audioBasePath)
    {
        _audioBasePath = audioBasePath;
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
            var audioPath = Path.Combine(_audioBasePath, languageCode, fileName);
            
            if (!File.Exists(audioPath))
            {
                Console.WriteLine($"Warning: Audio file not found: {audioPath}");
                return;
            }

            var tcs = new TaskCompletionSource();
            
            lock (_lockObject)
            {
                // Clean up any existing playback
                CleanupPlayback();
                
                // Create new audio file reader and output device
                _audioFileReader = new AudioFileReader(audioPath);
                _waveOutDevice = new WaveOutEvent();
                
                // Set up playback stopped event
                _waveOutDevice.PlaybackStopped += (s, e) => tcs.TrySetResult();
                
                // Initialize and play
                _waveOutDevice.Init(_audioFileReader);
                _waveOutDevice.Play();
            }
            
            // Wait for playback to finish or cancellation
            await tcs.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Stop();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio playback error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    public void Stop()
    {
        lock (_lockObject)
        {
            CleanupPlayback();
        }
    }
    
    private void CleanupPlayback()
    {
        // Must be called within lock
        _waveOutDevice?.Stop();
        _waveOutDevice?.Dispose();
        _waveOutDevice = null;
        
        _audioFileReader?.Dispose();
        _audioFileReader = null;
    }
    
    public void Dispose()
    {
        Stop();
    }
}
#endif
