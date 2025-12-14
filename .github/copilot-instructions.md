# GitHub Copilot Instructions

## Application Summary

**Square Root Trainer** is a desktop application designed to help users memorize square roots of numbers 1-20 through spaced repetition and audio prompts. Currently runs on Windows, with browser support planned.

### How It Works
The application uses pre-generated audio files to periodically quiz users on square roots:
1. **Question Phase**: Asks "What is the square root of [number]?" (where number is a random perfect square from 1-400)
2. **Time Announcement**: Tells the user how many seconds they have to think of the answer
3. **Wait Period**: Gives the user time to mentally calculate the answer
4. **Answer Phase**: Provides the correct answer: "The square root of [number] is [answer]"
5. **Rest Interval**: Waits before starting the next question cycle

### Features
- **Bilingual Support**: English and Dutch language options with pre-generated audio files
- **Configurable Timing**: Users can adjust:
  - Time to answer questions (default: 3 seconds)
  - Time between question cycles (default: 5 seconds)
- **Start/Stop Control**: Simple button to start and stop the training session
- **Pre-generated Audio**: All audio files are generated ahead of time using Windows Speech Synthesis
- **Clean UI**: Minimalist Avalonia interface focused on functionality

### Technical Implementation
- Training runs in an asynchronous loop that can be cancelled cleanly
- Audio playback abstracted through `IAudioPlayer` interface for cross-platform support
- Windows implementation uses NAudio for audio playback
- Pre-generated WAV files stored in `audio/en-US/` and `audio/nl-NL/` directories
- Input fields are disabled during active training to prevent mid-session changes
- Proper resource disposal on application close

## Project Overview
This is a desktop application built with:
- **Avalonia UI** (version 11.2.2)
- **.NET 9**
- **NAudio** for cross-platform audio playback on Windows
- Planned browser support using HTML5 Audio via JavaScript interop

## Code Style & Practices
- Use C# 12 features where appropriate
- Follow modern async/await patterns
- Use nullable reference types
- Keep code clean and well-commented
- Use meaningful variable and method names

## Testing & Verification
- **After making changes, run the application** using `dotnet run` from the `SquareRootTrainer` project folder
- Use `dotnet build` to check for compilation errors without showing the UI
- **Always run tests if they exist** using `dotnet test`
- Verify that the application compiles without errors before completing a task
- Check for compiler warnings and address them when possible

## Decision Making
- **If you are not sure what to do, please ask for clarification instead of just doing something and hoping for the best**
- When multiple approaches are possible, explain the tradeoffs and ask for preference
- If requirements are ambiguous, clarify before implementing

## Project-Specific Guidelines
- **Audio Generation**: Speech synthesis is ONLY done by the AudioGenerator CLI tool, NOT at runtime
- **Audio Playback**: Uses NAudio on Windows; browser will use HTML5 Audio via JS interop
- **Platform Abstraction**: Use `IAudioPlayer` interface for all audio playback
- Language support is currently English and Dutch
- Keep the UI simple and functional - this is a utility app, not a showcase
- Configuration values should be easily adjustable (prefer constants or UI inputs over hardcoded values)
- Handle cancellation properly when stopping the training loop

## Project Structure
```
/
├── SquareRootTrainer.sln           # Solution file
├── SquareRootTrainer/              # Main application project
│   ├── SquareRootTrainer.csproj
│   ├── IAudioPlayer.cs             # Interface for platform-specific audio
│   ├── TrainingAudioPlayer.cs      # Windows implementation using NAudio
│   ├── TrainingSession.cs          # Core training logic
│   ├── Texts.cs                    # Localized strings
│   ├── MainWindow.axaml            # UI layout
│   ├── MainWindow.axaml.cs         # UI logic and event handlers
│   ├── Program.cs                  # Application entry point
│   ├── App.axaml / App.axaml.cs    # Application-level setup
│   └── audio/                      # Pre-generated audio files
│       ├── en-US/
│       └── nl-NL/
└── AudioGenerator/                 # CLI tool for generating audio files
    ├── AudioGenerator.csproj
    └── Program.cs                  # Uses Windows.Media.SpeechSynthesis
```
