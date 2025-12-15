using System;
using System.Threading;
using System.Threading.Tasks;

namespace Carrots;

/// <summary>
/// Configuration for a training session.
/// </summary>
public class TrainingSessionConfig
{
    public required int AnswerTimeSeconds { get; init; }
    public required int IntervalSeconds { get; init; }
    public required int LowestNumber { get; init; }
    public required int HighestNumber { get; init; }
    public required string LanguageCode { get; init; }
}

/// <summary>
/// Manages the training session logic, including question cycles and countdowns.
/// </summary>
public class TrainingSession
{
    private const int BRIEF_PAUSE_MS = 1000; // Brief pause after asking question (in milliseconds)
    
    private readonly Random _random;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;
    
    // Callbacks for external operations
    private readonly Func<string, string, CancellationToken, Task> _playAudioCallback;
    private readonly Func<string, Task> _updateCountdownCallback;
    private readonly Func<int, bool, Task> _formatCountdownCallback;
    
    public bool IsRunning => _isRunning;
    
    /// <summary>
    /// Creates a new training session.
    /// </summary>
    /// <param name="playAudioCallback">Callback to play audio files (filename, languageCode, cancellationToken)</param>
    /// <param name="updateCountdownCallback">Callback to update countdown text in UI</param>
    /// <param name="formatCountdownCallback">Callback to format countdown text (seconds, isNextQuestion)</param>
    public TrainingSession(
        Func<string, string, CancellationToken, Task> playAudioCallback,
        Func<string, Task> updateCountdownCallback,
        Func<int, bool, Task> formatCountdownCallback)
    {
        _random = new Random();
        _playAudioCallback = playAudioCallback;
        _updateCountdownCallback = updateCountdownCallback;
        _formatCountdownCallback = formatCountdownCallback;
    }
    
    /// <summary>
    /// Starts the training session with the given configuration.
    /// </summary>
    public void Start(TrainingSessionConfig config)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Training session is already running");
        }
        
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start the training loop (fire and forget - UI will handle lifecycle)
        _ = TrainingLoopAsync(config, _cancellationTokenSource.Token);
    }
    
    /// <summary>
    /// Stops the currently running training session.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            return;
        }
        
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        
        // Clear countdown when stopped
        await _updateCountdownCallback("");
    }
    
    private async Task TrainingLoopAsync(TrainingSessionConfig config, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await AskQuestionCycleAsync(config, cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                // Display countdown and wait for the interval before the next question cycle
                await CountdownAsync(config.IntervalSeconds, true, cancellationToken);
            }
        }
        
        // Clear countdown when loop ends
        await _updateCountdownCallback("");
    }
    
    private async Task AskQuestionCycleAsync(TrainingSessionConfig config, CancellationToken cancellationToken)
    {
        try
        {
            // Generate a random number within the configured range
            int number = _random.Next(config.LowestNumber, config.HighestNumber + 1);
            
            // Phase 1: Ask the question
            await _playAudioCallback($"question_{number}.wav", config.LanguageCode, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 2: Brief pause
            await Task.Delay(BRIEF_PAUSE_MS, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 3: Announcement
            await _playAudioCallback("announcement.wav", config.LanguageCode, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 4: Wait for the answer time with countdown
            await CountdownAsync(config.AnswerTimeSeconds, false, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 5: Give the answer
            await _playAudioCallback($"answer_{number}.wav", config.LanguageCode, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when stopping
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
    }
    
    private async Task CountdownAsync(int seconds, bool isNextQuestion, CancellationToken cancellationToken)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            // Use callback to format and display countdown
            await _formatCountdownCallback(i, isNextQuestion);
            
            // Wait 1 second
            await Task.Delay(1000, cancellationToken);
        }
        
        // Clear the countdown text
        await _updateCountdownCallback("");
    }
}
