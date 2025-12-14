using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace AudioGenerator;

class Program
{
    private const int MIN_NUMBER = 1;
    private const int MAX_NUMBER = 20;
    
    // Hardcoded supported languages
    private static readonly (string Code, string Question, string Answer, string Announcement)[] Languages = 
    {
        ("en-US", "What is the square root of {0}?", "The square root of {0} is {1}.", "The answer follows shortly."),
        ("nl-NL", "Wat is de wortel van {0}?", "De wortel van {0} is {1}.", "Het antwoord volgt zodadelijk.")
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("Square Root Trainer - Audio Generator");
        Console.WriteLine("=====================================\n");

        var synthesizer = new SpeechSynthesizer();
        var audioDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "audio");
        
        // Ensure audio directory exists
        Directory.CreateDirectory(audioDir);

        foreach (var (languageCode, questionTemplate, answerTemplate, announcement) in Languages)
        {
            // Find voice for this language
            var voice = FindVoice(synthesizer, languageCode);
            if (voice == null)
            {
                Console.WriteLine($"⚠️  Warning: No voice found for {languageCode}, skipping...");
                continue;
            }

            Console.WriteLine($"\n🎤 Generating audio for {languageCode} using voice: {voice.DisplayName}");
            synthesizer.Voice = voice;

            var langDir = Path.Combine(audioDir, languageCode);
            Directory.CreateDirectory(langDir);

            // Generate announcement
            await GenerateAudio(synthesizer, announcement, Path.Combine(langDir, "announcement.wav"));
            Console.WriteLine($"   ✓ announcement.wav");

            // Generate questions and answers for numbers 1-20
            for (int number = MIN_NUMBER; number <= MAX_NUMBER; number++)
            {
                int square = number * number;
                
                // Question: "What is the square root of {square}?"
                var question = string.Format(questionTemplate, square);
                var questionFile = Path.Combine(langDir, $"question_{number}.wav");
                await GenerateAudio(synthesizer, question, questionFile);
                
                // Answer: "The square root of {square} is {number}"
                var answer = string.Format(answerTemplate, square, number);
                var answerFile = Path.Combine(langDir, $"answer_{number}.wav");
                await GenerateAudio(synthesizer, answer, answerFile);
                
                Console.WriteLine($"   ✓ question_{number}.wav & answer_{number}.wav");
            }
        }

        Console.WriteLine("\n✅ Audio generation complete!");
        Console.WriteLine($"📁 Files saved to: {Path.GetFullPath(audioDir)}");
    }

    private static VoiceInformation? FindVoice(SpeechSynthesizer synthesizer, string languageCode)
    {
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            if (voice.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return voice;
            }
        }
        return null;
    }

    private static async Task GenerateAudio(SpeechSynthesizer synthesizer, string text, string filePath)
    {
        using var stream = await synthesizer.SynthesizeTextToStreamAsync(text);
        
        // Read the stream and save to file
        using var fileStream = File.Create(filePath);
        using var reader = stream.AsStreamForRead();
        await reader.CopyToAsync(fileStream);
    }
}
