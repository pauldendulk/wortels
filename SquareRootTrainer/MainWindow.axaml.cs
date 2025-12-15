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
#if WINDOWS
    // Windows API to prevent system sleep
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;
    private const uint ES_AWAYMODE_REQUIRED = 0x00000040;
#endif
    
    // Change these constants to adjust timing
    private const int DEFAULT_INTERVAL_SECONDS = 300; // Default time between question cycles
    private const int DEFAULT_SECONDS_TO_ANSWER = 3; // Default time given to answer the question
    private const int DEFAULT_LOWEST_NUMBER = 4; // Default lowest number for square roots
    private const int DEFAULT_HIGHEST_NUMBER = 20; // Default highest number for square roots
    private const int MAX_SUPPORTED_NUMBER = 20; // Maximum number supported by audio files
    private const int MIN_SUPPORTED_NUMBER = 1; // Minimum number supported by audio files
    
    // UI Color constants
    private const string ERROR_COLOR = "#DC2626"; // Warm red for errors
    private const string NORMAL_COLOR = "#6366F1"; // Blue for normal countdown
    
    private readonly IAudioPlayer _audioPlayer;
    private readonly TrainingSession _trainingSession;
    private ILanguageTexts _currentTexts;
    private List<LanguageOption> _availableLanguages = new();
    private string _audioBasePath = "";

    public MainWindow()
    {
        InitializeComponent();

        // Set audio base path
        _audioBasePath = Path.Combine(AppContext.BaseDirectory, "audio");
        
        // Initialize audio player (platform-specific)
        _audioPlayer = CreateAudioPlayer(_audioBasePath);
        
        // Initialize training session with callbacks
        _trainingSession = new TrainingSession(
            playAudioCallback: PlayAudioFileAsync,
            updateCountdownCallback: UpdateCountdownTextAsync,
            formatCountdownCallback: FormatAndUpdateCountdownAsync
        );
        
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
    
    private static IAudioPlayer CreateAudioPlayer(string audioBasePath)
    {
#if BROWSER
        return new BrowserAudioPlayer(audioBasePath);
#else
        return new TrainingAudioPlayer(audioBasePath);
#endif
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

    /// <summary>
    /// Validates user inputs for lowest and highest numbers.
    /// </summary>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <returns>True if inputs are valid, false otherwise.</returns>
    private bool ValidateInputs(out string? errorMessage)
    {
        errorMessage = null;
        
        bool lowestValid = int.TryParse(LowestNumberTextBox.Text, out int lowestNumber);
        bool highestValid = int.TryParse(HighestNumberTextBox.Text, out int highestNumber);
        
        // Check if lowest < minimum supported
        if (lowestValid && lowestNumber < MIN_SUPPORTED_NUMBER)
        {
            errorMessage = _currentTexts.ErrorMinTooLow;
            return false;
        }
        
        // Check if highest > maximum supported
        if (highestValid && highestNumber > MAX_SUPPORTED_NUMBER)
        {
            errorMessage = _currentTexts.ErrorMaxTooHigh;
            return false;
        }
        
        // Check if min > max
        if (lowestValid && highestValid && lowestNumber > highestNumber)
        {
            errorMessage = _currentTexts.ErrorMinMaxValidation;
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Displays a validation error message to the user.
    /// </summary>
    private void ShowValidationError(string message)
    {
        CountdownTextBlock.Text = message;
        CountdownTextBlock.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(ERROR_COLOR));
    }
    
    /// <summary>
    /// Clears any validation error message and resets the text color.
    /// </summary>
    private void ClearValidationError()
    {
        CountdownTextBlock.Text = "";
        CountdownTextBlock.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(NORMAL_COLOR));
    }
    
    private void StartStopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_trainingSession.IsRunning)
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
        
        if (_trainingSession.IsRunning)
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
        StartStopButton.Content = _trainingSession.IsRunning ? _currentTexts.StopButton : _currentTexts.StartButton;
    }
   
    private void StartTraining()
    {
        // Validate inputs
        if (!ValidateInputs(out string? errorMessage))
        {
            ShowValidationError(errorMessage!);
            return;
        }
        
        // Clear any previous error and reset text color
        ClearValidationError();
        
        // Get selected language
        var selectedLanguage = (LanguageOption?)LanguageComboBox.SelectedItem;
        if (selectedLanguage == null) return;
        
        // Create configuration from UI inputs
        var config = new TrainingSessionConfig
        {
            AnswerTimeSeconds = GetAnswerTimeSeconds(),
            IntervalSeconds = GetIntervalSeconds(),
            LowestNumber = GetLowestNumber(),
            HighestNumber = GetHighestNumber(),
            LanguageCode = selectedLanguage.LanguageCode
        };
        
        // Update UI state
        StartStopButton.Content = _currentTexts.StopButton;
        
        // Disable the input fields while running
        LanguageComboBox.IsEnabled = false;
        AnswerTimeTextBox.IsEnabled = false;
        IntervalTextBox.IsEnabled = false;
        LowestNumberTextBox.IsEnabled = false;
        HighestNumberTextBox.IsEnabled = false;
        
#if WINDOWS
        // Keep system awake for audio playback, but allow display to turn off (like Spotify)
        // ES_CONTINUOUS | ES_SYSTEM_REQUIRED keeps system awake
        // ES_AWAYMODE_REQUIRED allows display sleep while playing audio
        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
#endif
        
        // Start the training session
        _trainingSession.Start(config);
    }

    private async void StopTraining()
    {
        await _trainingSession.StopAsync();
        
        StartStopButton.Content = _currentTexts.StartButton;
        
#if WINDOWS
        // Restore normal power management
        SetThreadExecutionState(ES_CONTINUOUS);
#endif
        
        // Re-enable the input fields
        LanguageComboBox.IsEnabled = true;
        AnswerTimeTextBox.IsEnabled = true;
        IntervalTextBox.IsEnabled = true;
        LowestNumberTextBox.IsEnabled = true;
        HighestNumberTextBox.IsEnabled = true;
    }

    // Callback methods for TrainingSession
    
    /// <summary>
    /// Callback for TrainingSession to play audio files.
    /// </summary>
    private async Task PlayAudioFileAsync(string fileName, string languageCode, CancellationToken cancellationToken)
    {
        await _audioPlayer.PlayAsync(fileName, languageCode, cancellationToken);
    }
    
    /// <summary>
    /// Callback for TrainingSession to update countdown text.
    /// </summary>
    private async Task UpdateCountdownTextAsync(string text)
    {
        await Dispatcher.UIThread.InvokeAsync(() => CountdownTextBlock.Text = text);
    }
    
    /// <summary>
    /// Callback for TrainingSession to format and update countdown text.
    /// </summary>
    private async Task FormatAndUpdateCountdownAsync(int seconds, bool isNextQuestion)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            string secondWord = seconds != 1 ? _currentTexts.Seconds : _currentTexts.Second;
            if (isNextQuestion)
            {
                CountdownTextBlock.Text = string.Format(_currentTexts.CountdownNextQuestion, seconds, secondWord);
            }
            else
            {
                CountdownTextBlock.Text = string.Format(_currentTexts.CountdownRemaining, seconds, secondWord);
            }
        });
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
    
    /// <summary>
    /// Gets the lowest number from the input field, with fallback to default.
    /// Note: Validation should be done before calling this method.
    /// </summary>
    private int GetLowestNumber()
    {
        if (int.TryParse(LowestNumberTextBox.Text, out int number))
        {
            return number;
        }
        return DEFAULT_LOWEST_NUMBER;
    }
    
    /// <summary>
    /// Gets the highest number from the input field, with fallback to default.
    /// Note: Validation should be done before calling this method.
    /// </summary>
    private int GetHighestNumber()
    {
        if (int.TryParse(HighestNumberTextBox.Text, out int number))
        {
            return number;
        }
        return DEFAULT_HIGHEST_NUMBER;
    }

    protected override async void OnClosed(EventArgs e)
    {
        await _trainingSession.StopAsync();
        
#if WINDOWS
        // Ensure power management is restored when window closes
        SetThreadExecutionState(ES_CONTINUOUS);
#endif
        _audioPlayer.Dispose();
        base.OnClosed(e);
    }
}
