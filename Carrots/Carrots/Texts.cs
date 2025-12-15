namespace Carrots;

public interface ILanguageTexts
{
    // Speech texts
    string Question { get; }
    string Announcement { get; }
    string Answer { get; }
    
    // UI texts
    string WindowTitle { get; }
    string WindowSubtitle { get; }
    string LanguageLabel { get; }
    string AnswerTimeLabel { get; }
    string IntervalTimeLabel { get; }
    string LowestNumberLabel { get; }
    string HighestNumberLabel { get; }
    string StartButton { get; }
    string StopButton { get; }
    string CountdownNextQuestion { get; } // Format: "Next question in {0} second(s)"
    string CountdownRemaining { get; } // Format: "{0} second(s) remaining"
    string Seconds { get; }
    string Second { get; }
    string ErrorMinMaxValidation { get; }
    string ErrorMinTooLow { get; }
    string ErrorMaxTooHigh { get; }
}

public class EnglishTexts : ILanguageTexts
{
    // Speech texts
    public string Question => "What is the square root of {0}?";
    public string Announcement => "The answer follows shortly.";
    public string Answer => "The square root of {0} is {1}.";
    
    // UI texts
    public string WindowTitle => "Square Root Trainer";
    public string WindowSubtitle => "Start the app and keep it running in the background while you answer the questions in your head when they come up.";
    public string LanguageLabel => "Language";
    public string AnswerTimeLabel => "Time to answer (s)";
    public string IntervalTimeLabel => "Interval time (s)";
    public string LowestNumberLabel => "Lowest Number";
    public string HighestNumberLabel => "Highest Number";
    public string StartButton => "Start Training";
    public string StopButton => "Stop";
    public string CountdownNextQuestion => "Next question in {0} {1}";
    public string CountdownRemaining => "{0} {1} remaining";
    public string Seconds => "seconds";
    public string Second => "second";
    public string ErrorMinMaxValidation => "Min must be ≤ max";
    public string ErrorMinTooLow => "Min must be ≥ 1";
    public string ErrorMaxTooHigh => "Max must be ≤ 20";
}

public class DutchTexts : ILanguageTexts
{
    // Speech texts
    public string Question => "Wat is de wortel van {0}?";
    public string Announcement => "Het antwoord volgt zodadelijk.";
    public string Answer => "De wortel van {0} is {1}.";
    
    // UI texts
    public string WindowTitle => "Worteltrainer";
    public string WindowSubtitle => "Start de app en laat deze op de achtergrond draaien terwijl je de vragen in je hoofd beantwoordt wanneer ze gesteld worden.";
    public string LanguageLabel => "Taal";
    public string AnswerTimeLabel => "Tijd om te antwoorden (s)";
    public string IntervalTimeLabel => "Interval tijd (s)";
    public string LowestNumberLabel => "Laagste Getal";
    public string HighestNumberLabel => "Hoogste Getal";
    public string StartButton => "Start Training";
    public string StopButton => "Stop";
    public string CountdownNextQuestion => "Volgende vraag over {0} {1}";
    public string CountdownRemaining => "Nog {0} {1}";
    public string Seconds => "seconden";
    public string Second => "seconde";
    public string ErrorMinMaxValidation => "Min moet ≤ max zijn";
    public string ErrorMinTooLow => "Min moet ≥ 1 zijn";
    public string ErrorMaxTooHigh => "Max moet ≤ 20 zijn";
}
