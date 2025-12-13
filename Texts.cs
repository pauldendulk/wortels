namespace MathTrainer;

public interface ILanguageTexts
{
    // Speech texts
    string Question { get; }
    string TimeAnnouncement { get; }
    string Answer { get; }
    
    // UI texts
    string WindowTitle { get; }
    string WindowSubtitle { get; }
    string LanguageLabel { get; }
    string AnswerTimeLabel { get; }
    string IntervalTimeLabel { get; }
    string StartButton { get; }
    string StopButton { get; }
    string CountdownNextQuestion { get; } // Format: "Next question in {0} second(s)"
    string CountdownRemaining { get; } // Format: "{0} second(s) remaining"
    string Seconds { get; }
    string Second { get; }
}

public class EnglishTexts : ILanguageTexts
{
    // Speech texts
    public string Question => "What is the square root of {0}?";
    public string TimeAnnouncement => "The answer follows in {0} seconds.";
    public string Answer => "The square root of {0} is {1}.";
    
    // UI texts
    public string WindowTitle => "Square Root Trainer";
    public string WindowSubtitle => "Sharpen your mental math skills";
    public string LanguageLabel => "Language";
    public string AnswerTimeLabel => "Time to answer (s)";
    public string IntervalTimeLabel => "Interval time (s)";
    public string StartButton => "Start Training";
    public string StopButton => "Stop";
    public string CountdownNextQuestion => "Next question in {0} {1}";
    public string CountdownRemaining => "{0} {1} remaining";
    public string Seconds => "seconds";
    public string Second => "second";
}

public class DutchTexts : ILanguageTexts
{
    // Speech texts
    public string Question => "Wat is de wortel van {0}?";
    public string TimeAnnouncement => "Het antwoord volgt over {0} seconden.";
    public string Answer => "De wortel van {0} is {1}.";
    
    // UI texts
    public string WindowTitle => "Worteltrainer";
    public string WindowSubtitle => "Verbeter je hoofdrekenen";
    public string LanguageLabel => "Taal";
    public string AnswerTimeLabel => "Tijd om te antwoorden (s)";
    public string IntervalTimeLabel => "Interval tijd (s)";
    public string StartButton => "Start Training";
    public string StopButton => "Stop";
    public string CountdownNextQuestion => "Volgende vraag over {0} {1}";
    public string CountdownRemaining => "Nog {0} {1}";
    public string Seconds => "seconden";
    public string Second => "seconde";
}
