using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace MathTrainer;

public partial class MainWindow : Window
{
    // Change these constants to adjust timing
    private const int DEFAULT_INTERVAL_SECONDS = 5; // Default time between question cycles
    private const int DEFAULT_SECONDS_TO_ANSWER = 3; // Default time given to answer the question
    private const int BRIEF_PAUSE_MS = 1000; // Brief pause after asking question (in milliseconds)
    
    private readonly SpeechSynthesizer _synthesizer;
    private readonly MediaPlayer _mediaPlayer;
    private readonly Random _random;
    private bool _isRunning = false;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();

        _synthesizer = new SpeechSynthesizer();
        _mediaPlayer = new MediaPlayer();

        // Debug available voices
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            Console.WriteLine($"{voice.DisplayName} | {voice.Language}");
        }

        // Set Dutch voice immediately after creation
        SetVoice("nl-NL");
        
        _random = new Random();

        // Ensure the UI reflects the Dutch default so it doesn't switch back to English on start
        LanguageComboBox.SelectedIndex = 1;
        
        // Initialize UI with default constants
        AnswerTimeTextBox.Text = DEFAULT_SECONDS_TO_ANSWER.ToString();
        IntervalTextBox.Text = DEFAULT_INTERVAL_SECONDS.ToString();
    }

    private void StartStopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            StopTraining();
        }
        else
        {
            StartTraining();
        }
    }

    private bool IsEnglish => LanguageComboBox.SelectedIndex == 0;
    
    private void SetVoiceForLanguage()
    {
        SetVoice(IsEnglish ? "en-US" : "nl-NL");
    }

    private void SetVoice(string cultureCode)
    {
        try
        {
            var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Language.Contains(cultureCode));
            if (voice != null)
            {
                _synthesizer.Voice = voice;
            }
            else
            {
                Console.WriteLine($"Voice for {cultureCode} not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not set voice: {ex.Message}");
        }
    }
    
    private void StartTraining()
    {
        _isRunning = true;
        StartStopButton.Content = "Stop";
        
        // Disable the input fields while running
        LanguageComboBox.IsEnabled = false;
        AnswerTimeTextBox.IsEnabled = false;
        IntervalTextBox.IsEnabled = false;
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start the training loop
        _ = TrainingLoopAsync(_cancellationTokenSource.Token);
    }

    private void StopTraining()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        StartStopButton.Content = "Start";
        
        // Clear the countdown indicator
        CountdownTextBlock.Text = "";
        
        // Re-enable the input fields
        LanguageComboBox.IsEnabled = true;
        AnswerTimeTextBox.IsEnabled = true;
        IntervalTextBox.IsEnabled = true;
    }

    private async Task TrainingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await AskQuestionCycleAsync(cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                // Get the interval from the UI
                int intervalSeconds = GetIntervalSeconds();
                SetVoiceForLanguage();
                
                // Display countdown and wait for the interval before the next question cycle
                await CountdownAsync(intervalSeconds, true, cancellationToken);
            }
        }
        
        // Clear countdown when stopped
        await Dispatcher.UIThread.InvokeAsync(() => CountdownTextBlock.Text = "");
    }

    private async Task AskQuestionCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get the answer time from the UI
            int secondsToAnswer = GetAnswerTimeSeconds();
            
            // Generate a random number from 1 to 20
            int number = _random.Next(1, 21);
            int square = number * number;
            
            // Select language texts
            ILanguageTexts texts = IsEnglish ? new EnglishTexts() : new DutchTexts();
            
            // Phase 1: Ask the question
            SetVoiceForLanguage();
            string question = string.Format(texts.Question, square);
            await SpeakTextAsync(question, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 2: Brief pause
            await Task.Delay(BRIEF_PAUSE_MS, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 3: Tell how much time they have
            string timeAnnouncement = string.Format(texts.TimeAnnouncement, secondsToAnswer);
            await SpeakTextAsync(timeAnnouncement, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 4: Wait for the answer time with countdown
            await CountdownAsync(secondsToAnswer, false, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 5: Give the answer
            string answer = string.Format(texts.Answer, square, number);
            await SpeakTextAsync(answer, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when stopping
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping during await
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in question cycle: {ex.Message}");
        }
    }

    private async Task SpeakTextAsync(string text, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        try
        {
            // Generate the audio stream from text
            using var stream = await _synthesizer.SynthesizeTextToStreamAsync(text);
            
            var tcs = new TaskCompletionSource();
            
            // Define handler to signal completion
            TypedEventHandler<MediaPlayer, object> onEnded = (s, e) => tcs.TrySetResult();
            
            try
            {
                _mediaPlayer.MediaEnded += onEnded;
                _mediaPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
                _mediaPlayer.Play();
                
                // Wait for playback to finish or cancellation
                await tcs.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                _mediaPlayer.MediaEnded -= onEnded;
            }
        }
        catch (OperationCanceledException)
        {
            _mediaPlayer.Pause();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Speech error: {ex.Message}");
        }
    }
    
    private async Task CountdownAsync(int seconds, bool isNextQuestion, CancellationToken cancellationToken)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            // Update UI on the UI thread with different text based on context
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                if (isNextQuestion)
                {
                    CountdownTextBlock.Text = $"Next question in {i} second{(i != 1 ? "s" : "")}";
                }
                else
                {
                    CountdownTextBlock.Text = $"{i} second{(i != 1 ? "s" : "")} remaining";
                }
            });
            
            // Wait 1 second
            await Task.Delay(1000, cancellationToken);
        }
        
        // Clear the countdown text
        await Dispatcher.UIThread.InvokeAsync(() => CountdownTextBlock.Text = "");
    }
    
    private int GetAnswerTimeSeconds()
    {
        if (int.TryParse(AnswerTimeTextBox.Text, out int seconds) && seconds > 0)
        {
            return seconds;
        }
        return DEFAULT_SECONDS_TO_ANSWER;
    }
    
    private int GetIntervalSeconds()
    {
        if (int.TryParse(IntervalTextBox.Text, out int seconds) && seconds > 0)
        {
            return seconds;
        }
        return DEFAULT_INTERVAL_SECONDS;
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _synthesizer.Dispose();
        _mediaPlayer.Dispose();
        base.OnClosed(e);
    }
}
