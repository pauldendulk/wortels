using System;
using System.Threading;
using System.Threading.Tasks;

namespace Carrots;

/// <summary>
/// Interface for audio playback implementations across different platforms.
/// </summary>
public interface IAudioPlayer : IDisposable
{
    /// <summary>
    /// Plays an audio file and waits for it to complete.
    /// </summary>
    /// <param name="fileName">Name of the audio file (e.g., "question_4.wav")</param>
    /// <param name="languageCode">Language code for the audio (e.g., "en-US")</param>
    /// <param name="cancellationToken">Token to cancel playback</param>
    Task PlayAsync(string fileName, string languageCode, CancellationToken cancellationToken);
    
    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    void Stop();
}
