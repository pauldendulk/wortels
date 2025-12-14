using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace SquareRootTrainer;

// Helper class to store language information
public class LanguageOption
{
    public required string DisplayName { get; init; }
    public required string LanguageCode { get; init; }
    
    public override string ToString() => DisplayName;
}

public partial class MainWindow : Window
{
    // Windows API to prevent system sleep
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;
    private const uint ES_AWAYMODE_REQUIRED = 0x00000040;
    // Change these constants to adjust timing
    private const int DEFAULT_INTERVAL_SECONDS = 60; // Default time between question cycles
    private const int DEFAULT_SECONDS_TO_ANSWER = 3; // Default time given to answer the question
    private const int BRIEF_PAUSE_MS = 1000; // Brief pause after asking question (in milliseconds)
    private const int DEFAULT_LOWEST_NUMBER = 4; // Default lowest number for square roots
    private const int DEFAULT_HIGHEST_NUMBER = 20; // Default highest number for square roots
    private const int MAX_SUPPORTED_NUMBER = 20; // Maximum number supported by audio files
    
    private readonly MediaPlayer _mediaPlayer;
    private readonly Random _random;
    private bool _isRunning = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private ILanguageTexts _currentTexts;
    private List<LanguageOption> _availableLanguages = new();
    private string _audioBasePath = "";

    public MainWindow()
    {
        InitializeComponent();

        _mediaPlayer = new MediaPlayer();
        _random = new Random();

        // Set audio base path
        _audioBasePath = Path.Combine(AppContext.BaseDirectory, "audio");
        
        // Populate available languages from audio folders
        PopulateAvailableLanguages();
        
        // Initialize current texts to Dutch or fallback to English
        var dutchLanguage = _availableLanguages.FirstOrDefault(l => l.LanguageCode.StartsWith("nl"));
        var defaultLanguage = dutchLanguage ?? _availableLanguages.FirstOrDefault();
        
        if (defaultLanguage != null)
        {
            _currentTexts = defaultLanguage.LanguageCode.StartsWith("nl") ? new DutchTexts() : new EnglishTexts();
        }
        else
        {
            _currentTexts = new EnglishTexts();
        }
        
        // Initialize UI with default constants
        AnswerTimeTextBox.Text = DEFAULT_SECONDS_TO_ANSWER.ToString();
        IntervalTextBox.Text = DEFAULT_INTERVAL_SECONDS.ToString();
        LowestNumberTextBox.Text = DEFAULT_LOWEST_NUMBER.ToString();
        HighestNumberTextBox.Text = DEFAULT_HIGHEST_NUMBER.ToString();
        
        // Update UI with current language
        UpdateUILanguage();
    }
    
    private void PopulateAvailableLanguages()
    {
        // Check if audio directory exists
        if (!Directory.Exists(_audioBasePath))
        {
            Console.WriteLine($"Warning: Audio directory not found at {_audioBasePath}");
            return;
        }

        // Get all language folders
        var languageDirs = Directory.GetDirectories(_audioBasePath);
        
        foreach (var langDir in languageDirs)
        {
            var languageCode = Path.GetFileName(langDir);
            
            // Try to get a friendly display name
            string displayName;
            try
            {
                var culture = new CultureInfo(languageCode);
                displayName = $"{culture.DisplayName} ({languageCode})";
            }
            catch
            {
                // Fallback if culture info is not available
                displayName = languageCode;
            }
            
            _availableLanguages.Add(new LanguageOption
            {
                DisplayName = displayName,
                LanguageCode = languageCode
            });
            
            Console.WriteLine($"Available: {displayName}");
        }
        
        // Populate the ComboBox
        LanguageComboBox.ItemsSource = _availableLanguages;
        
        // Try to select Dutch by default, otherwise select first
        var dutchIndex = _availableLanguages.FindIndex(l => l.LanguageCode.StartsWith("nl"));
        LanguageComboBox.SelectedIndex = dutchIndex >= 0 ? dutchIndex : 0;
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
    
    private void LanguageComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Ignore events during initialization before controls are ready
        if (LanguageComboBox == null || LanguageComboBox.SelectedItem == null)
        {
            return;
        }
        
        if (_isRunning)
        {
            // Don't allow language change while training is running
            return;
        }
        
        var selectedLanguage = (LanguageOption)LanguageComboBox.SelectedItem;
        
        // Determine which text set to use based on language code
        // Support for nl-* for Dutch, en-* for English, fallback to English for others
        if (selectedLanguage.LanguageCode.StartsWith("nl"))
        {
            _currentTexts = new DutchTexts();
        }
        else
        {
            _currentTexts = new EnglishTexts();
        }
        
        UpdateUILanguage();
    }
    
    private void UpdateUILanguage()
    {
        Title = _currentTexts.WindowTitle;
        TitleTextBlock.Text = _currentTexts.WindowTitle;
        SubtitleTextBlock.Text = _currentTexts.WindowSubtitle;
        LanguageLabelTextBlock.Text = _currentTexts.LanguageLabel;
        AnswerTimeLabelTextBlock.Text = _currentTexts.AnswerTimeLabel;
        IntervalTimeLabelTextBlock.Text = _currentTexts.IntervalTimeLabel;
        LowestNumberLabelTextBlock.Text = _currentTexts.LowestNumberLabel;
        HighestNumberLabelTextBlock.Text = _currentTexts.HighestNumberLabel;
        StartStopButton.Content = _isRunning ? _currentTexts.StopButton : _currentTexts.StartButton;
    }
   
    private void StartTraining()
    {
        _isRunning = true;
        StartStopButton.Content = _currentTexts.StopButton;
        
        // Disable the input fields while running
        LanguageComboBox.IsEnabled = false;
        AnswerTimeTextBox.IsEnabled = false;
        IntervalTextBox.IsEnabled = false;
        LowestNumberTextBox.IsEnabled = false;
        HighestNumberTextBox.IsEnabled = false;
        
        // Keep system awake for audio playback, but allow display to turn off (like Spotify)
        // ES_CONTINUOUS | ES_SYSTEM_REQUIRED keeps system awake
        // ES_AWAYMODE_REQUIRED allows display sleep while playing audio
        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start the training loop
        _ = TrainingLoopAsync(_cancellationTokenSource.Token);
    }

    private void StopTraining()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        StartStopButton.Content = _currentTexts.StartButton;
        
        // Restore normal power management
        SetThreadExecutionState(ES_CONTINUOUS);
        
        // Clear the countdown indicator
        CountdownTextBlock.Text = "";
        
        // Re-enable the input fields
        LanguageComboBox.IsEnabled = true;
        AnswerTimeTextBox.IsEnabled = true;
        IntervalTextBox.IsEnabled = true;
        LowestNumberTextBox.IsEnabled = true;
        HighestNumberTextBox.IsEnabled = true;
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
            
            // Get the configured range from the UI
            int lowestNumber = GetLowestNumber();
            int highestNumber = GetHighestNumber();
            
            // Generate a random number within the configured range
            int number = _random.Next(lowestNumber, highestNumber + 1);
            
            // Get selected language
            var selectedLanguage = (LanguageOption?)LanguageComboBox.SelectedItem;
            if (selectedLanguage == null) return;
            
            // Phase 1: Ask the question
            await PlayAudioFileAsync($"question_{number}.wav", selectedLanguage.LanguageCode, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 2: Brief pause
            await Task.Delay(BRIEF_PAUSE_MS, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 3: Announcement
            await PlayAudioFileAsync("announcement.wav", selectedLanguage.LanguageCode, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 4: Wait for the answer time with countdown
            await CountdownAsync(secondsToAnswer, false, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Phase 5: Give the answer
            await PlayAudioFileAsync($"answer_{number}.wav", selectedLanguage.LanguageCode, cancellationToken);
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

    private async Task PlayAudioFileAsync(string fileName, string languageCode, CancellationToken cancellationToken)
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
            
            var file = await StorageFile.GetFileFromPathAsync(audioPath);
            
            var tcs = new TaskCompletionSource();
            
            // Define handler to signal completion
            TypedEventHandler<MediaPlayer, object> onEnded = (s, e) => tcs.TrySetResult();
            
            try
            {
                _mediaPlayer.MediaEnded += onEnded;
                _mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
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
            Console.WriteLine($"Audio playback error: {ex.Message}");
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
                string secondWord = i != 1 ? _currentTexts.Seconds : _currentTexts.Second;
                if (isNextQuestion)
                {
                    CountdownTextBlock.Text = string.Format(_currentTexts.CountdownNextQuestion, i, secondWord);
                }
                else
                {
                    CountdownTextBlock.Text = string.Format(_currentTexts.CountdownRemaining, i, secondWord);
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
    
    private int GetLowestNumber()
    {
        if (int.TryParse(LowestNumberTextBox.Text, out int number) && number > 0)
        {
            // Clamp to supported range
            if (number > MAX_SUPPORTED_NUMBER)
            {
                number = MAX_SUPPORTED_NUMBER;
                LowestNumberTextBox.Text = number.ToString();
            }
            return number;
        }
        return DEFAULT_LOWEST_NUMBER;
    }
    
    private int GetHighestNumber()
    {
        if (int.TryParse(HighestNumberTextBox.Text, out int number) && number > 0)
        {
            // Clamp to supported range
            if (number > MAX_SUPPORTED_NUMBER)
            {
                number = MAX_SUPPORTED_NUMBER;
                HighestNumberTextBox.Text = number.ToString();
            }
            
            var lowest = GetLowestNumber();
            // Ensure highest is at least equal to lowest
            return number >= lowest ? number : lowest;
        }
        return DEFAULT_HIGHEST_NUMBER;
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        
        // Ensure power management is restored when window closes
        SetThreadExecutionState(ES_CONTINUOUS);
        _mediaPlayer.Dispose();
        base.OnClosed(e);
    }
}
