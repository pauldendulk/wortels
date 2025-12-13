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
                
                // Wait for the interval before the next question cycle
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
            }
        }
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
            
            // Phase 1: Ask the question
            SetVoiceForLanguage();
            string questionTemplate = IsEnglish ? Texts.English.Question : Texts.Dutch.Question;
            string question = string.Format(questionTemplate, square);
            await SpeakTextAsync(question, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 2: Brief pause
            await Task.Delay(BRIEF_PAUSE_MS, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 3: Tell how much time they have
            string timeTemplate = IsEnglish ? Texts.English.TimeAnnouncement : Texts.Dutch.TimeAnnouncement;
            string timeAnnouncement = string.Format(timeTemplate, secondsToAnswer);
            await SpeakTextAsync(timeAnnouncement, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 4: Wait for the answer time
            await Task.Delay(TimeSpan.FromSeconds(secondsToAnswer), cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 5: Give the answer
            string answerTemplate = IsEnglish ? Texts.English.Answer : Texts.Dutch.Answer;
            string answer = string.Format(answerTemplate, square, number);
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
