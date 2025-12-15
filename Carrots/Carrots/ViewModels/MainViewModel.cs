using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Carrots.ViewModels;

// Helper class to store language information
public class LanguageOption
{
    public required string DisplayName { get; init; }
    public required string LanguageCode { get; init; }
    
    public override string ToString() => DisplayName;
}

public partial class MainViewModel : ViewModelBase
{
    private const int DEFAULT_INTERVAL_SECONDS = 300;
    private const int DEFAULT_SECONDS_TO_ANSWER = 3;
    private const int DEFAULT_LOWEST_NUMBER = 4;
    private const int DEFAULT_HIGHEST_NUMBER = 20;
    private const int MAX_SUPPORTED_NUMBER = 20;
    private const int MIN_SUPPORTED_NUMBER = 1;
    
    private const string ERROR_COLOR = "#DC2626";
    private const string NORMAL_COLOR = "#6366F1";
    
    private readonly IAudioPlayer _audioPlayer;
    private readonly TrainingSession _trainingSession;
    private ILanguageTexts _currentTexts;
    
    [ObservableProperty]
    private string _windowTitle = "Square Root Trainer";
    
    [ObservableProperty]
    private string _windowSubtitle = "";
    
    [ObservableProperty]
    private string _languageLabel = "Language";
    
    [ObservableProperty]
    private string _answerTimeLabel = "Time to answer (s)";
    
    [ObservableProperty]
    private string _intervalTimeLabel = "Interval time (s)";
    
    [ObservableProperty]
    private string _lowestNumberLabel = "Lowest Number";
    
    [ObservableProperty]
    private string _highestNumberLabel = "Highest Number";
    
    [ObservableProperty]
    private string _startButtonText = "Start Training";
    
    [ObservableProperty]
    private string _stopButtonText = "Stop";
    
    [ObservableProperty]
    private List<LanguageOption> _availableLanguages = new();
    
    [ObservableProperty]
    private LanguageOption? _selectedLanguage;
    
    [ObservableProperty]
    private string _answerTime = DEFAULT_SECONDS_TO_ANSWER.ToString();
    
    [ObservableProperty]
    private string _intervalTime = DEFAULT_INTERVAL_SECONDS.ToString();
    
    [ObservableProperty]
    private string _lowestNumber = DEFAULT_LOWEST_NUMBER.ToString();
    
    [ObservableProperty]
    private string _highestNumber = DEFAULT_HIGHEST_NUMBER.ToString();
    
    [ObservableProperty]
    private string _countdownText = "";
    
    [ObservableProperty]
    private string _countdownColor = NORMAL_COLOR;
    
    [ObservableProperty]
    private string _errorMessage = "";
    
    [ObservableProperty]
    private bool _isTraining = false;
    
    [ObservableProperty]
    private bool _canEditSettings = true;
    
    public MainViewModel()
    {
        // Set audio base path - check if we're running in browser
        var isBrowser = OperatingSystem.IsBrowser();
        var audioBasePath = isBrowser ? "audio" : Path.Combine(AppContext.BaseDirectory, "audio");
        
        Console.WriteLine($"Running in browser: {isBrowser}, audio path: {audioBasePath}");
        
        // Initialize audio player (platform-specific)
        _audioPlayer = CreateAudioPlayer(audioBasePath);
        
        // Initialize training session with callbacks
        _trainingSession = new TrainingSession(
            playAudioCallback: PlayAudioFileAsync,
            updateCountdownCallback: UpdateCountdownTextAsync,
            formatCountdownCallback: FormatAndUpdateCountdownAsync
        );
        
        // Populate available languages from audio folders
        PopulateAvailableLanguages(audioBasePath);
        
        // Initialize current texts to Dutch or fallback to English
        var dutchLanguage = AvailableLanguages.FirstOrDefault(l => l.LanguageCode.StartsWith("nl"));
        var defaultLanguage = dutchLanguage ?? AvailableLanguages.FirstOrDefault();
        
        if (defaultLanguage != null)
        {
            _currentTexts = defaultLanguage.LanguageCode.StartsWith("nl") ? new DutchTexts() : new EnglishTexts();
            SelectedLanguage = defaultLanguage;
        }
        else
        {
            _currentTexts = new EnglishTexts();
        }
        
        // Update UI with current language
        UpdateUILanguage();
    }
    
    private static IAudioPlayer CreateAudioPlayer(string audioBasePath)
    {
#if BROWSER
        Console.WriteLine("Creating BrowserAudioPlayer");
        return new BrowserAudioPlayer(audioBasePath);
#else
        Console.WriteLine("Creating TrainingAudioPlayer (NAudio)");
        #pragma warning disable CA1416 // Validate platform compatibility
        return new TrainingAudioPlayer(audioBasePath);
        #pragma warning restore CA1416 // Validate platform compatibility
#endif
    }
    
    private void PopulateAvailableLanguages(string audioBasePath)
    {
        Console.WriteLine($"PopulateAvailableLanguages called with path: {audioBasePath}");
        var languages = new List<LanguageOption>();
        var isBrowser = OperatingSystem.IsBrowser();
        
        if (isBrowser)
        {
            // In browser, we can't scan directories, so hardcode the available languages
            Console.WriteLine("BROWSER mode (runtime): Adding hardcoded languages");
            languages.Add(new LanguageOption
            {
                DisplayName = "English (United States) (en-US)",
                LanguageCode = "en-US"
            });
            
            languages.Add(new LanguageOption
            {
                DisplayName = "Nederlands (Nederland) (nl-NL)",
                LanguageCode = "nl-NL"
            });
            
            Console.WriteLine($"Browser: Added {languages.Count} hardcoded language options");
        }
        else
        {
            Console.WriteLine("DESKTOP mode: Scanning directories");
            // Check if audio directory exists
            if (!Directory.Exists(audioBasePath))
            {
                Console.WriteLine($"Warning: Audio directory not found at {audioBasePath}");
                AvailableLanguages = languages;
                return;
            }

            // Get all language folders
            var languageDirs = Directory.GetDirectories(audioBasePath);
            
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
                
                languages.Add(new LanguageOption
                {
                    DisplayName = displayName,
                    LanguageCode = languageCode
                });
                
                Console.WriteLine($"Available: {displayName}");
            }
        }
        
        Console.WriteLine($"Setting AvailableLanguages with {languages.Count} items");
        AvailableLanguages = languages;
    }
    
    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value != null)
        {
            _currentTexts = value.LanguageCode.StartsWith("nl") ? new DutchTexts() : new EnglishTexts();
            UpdateUILanguage();
        }
    }
    
    private void UpdateUILanguage()
    {
        WindowTitle = _currentTexts.WindowTitle;
        WindowSubtitle = _currentTexts.WindowSubtitle;
        LanguageLabel = _currentTexts.LanguageLabel;
        AnswerTimeLabel = _currentTexts.AnswerTimeLabel;
        IntervalTimeLabel = _currentTexts.IntervalTimeLabel;
        LowestNumberLabel = _currentTexts.LowestNumberLabel;
        HighestNumberLabel = _currentTexts.HighestNumberLabel;
        StartButtonText = _currentTexts.StartButton;
        StopButtonText = _currentTexts.StopButton;
    }
    
    [RelayCommand]
    private void StartTraining()
    {
        // Validate inputs
        if (!ValidateInputs())
        {
            return;
        }
        
        ErrorMessage = "";
        IsTraining = true;
        CanEditSettings = false;
        
        var config = new TrainingSessionConfig
        {
            AnswerTimeSeconds = int.Parse(AnswerTime),
            IntervalSeconds = int.Parse(IntervalTime),
            LowestNumber = int.Parse(LowestNumber),
            HighestNumber = int.Parse(HighestNumber),
            LanguageCode = SelectedLanguage?.LanguageCode ?? "en-US"
        };
        
        _trainingSession.Start(config);
    }
    
    [RelayCommand]
    private async Task StopTraining()
    {
        await _trainingSession.StopAsync();
        _audioPlayer.Stop();
        IsTraining = false;
        CanEditSettings = true;
        CountdownText = "";
    }
    
    private bool ValidateInputs()
    {
        if (!int.TryParse(LowestNumber, out int min) || min < MIN_SUPPORTED_NUMBER)
        {
            ErrorMessage = _currentTexts.ErrorMinTooLow;
            return false;
        }
        
        if (!int.TryParse(HighestNumber, out int max) || max > MAX_SUPPORTED_NUMBER)
        {
            ErrorMessage = _currentTexts.ErrorMaxTooHigh;
            return false;
        }
        
        if (min > max)
        {
            ErrorMessage = _currentTexts.ErrorMinMaxValidation;
            return false;
        }
        
        return true;
    }
    
    private async Task PlayAudioFileAsync(string fileName, string languageCode, System.Threading.CancellationToken cancellationToken)
    {
        await _audioPlayer.PlayAsync(fileName, languageCode, cancellationToken);
    }
    
    private Task UpdateCountdownTextAsync(string text)
    {
        CountdownText = text;
        return Task.CompletedTask;
    }
    
    private Task FormatAndUpdateCountdownAsync(int seconds, bool isNextQuestion)
    {
        var secondsText = seconds == 1 ? _currentTexts.Second : _currentTexts.Seconds;
        
        if (isNextQuestion)
        {
            CountdownText = string.Format(_currentTexts.CountdownNextQuestion, seconds, secondsText);
            CountdownColor = NORMAL_COLOR;
        }
        else
        {
            CountdownText = string.Format(_currentTexts.CountdownRemaining, seconds, secondsText);
            CountdownColor = ERROR_COLOR;
        }
        
        return Task.CompletedTask;
    }
    
    public void Cleanup()
    {
        _audioPlayer?.Dispose();
    }
}
