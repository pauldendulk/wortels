# GitHub Copilot Instructions

## Application Summary

**Square Root Trainer** is a Windows desktop application designed to help users memorize square roots of numbers 1-20 through spaced repetition and audio prompts.

### How It Works
The application uses text-to-speech to periodically quiz users on square roots:
1. **Question Phase**: Asks "What is the square root of [number]?" (where number is a random perfect square from 1-400)
2. **Time Announcement**: Tells the user how many seconds they have to think of the answer
3. **Wait Period**: Gives the user time to mentally calculate the answer
4. **Answer Phase**: Provides the correct answer: "The square root of [number] is [answer]"
5. **Rest Interval**: Waits before starting the next question cycle

### Features
- **Bilingual Support**: English and Dutch language options with native voice synthesis
- **Configurable Timing**: Users can adjust:
  - Time to answer questions (default: 3 seconds)
  - Time between question cycles (default: 5 seconds)
- **Start/Stop Control**: Simple button to start and stop the training session
- **Windows Speech Synthesis**: Uses Windows.Media.SpeechSynthesis for high-quality, natural-sounding voices
- **Clean UI**: Minimalist Avalonia interface focused on functionality

### Technical Implementation
- Training runs in an asynchronous loop that can be cancelled cleanly
- Voice selection automatically switches based on language choice (en-US / nl-NL)
- MediaPlayer handles audio playback with proper event handling
- Input fields are disabled during active training to prevent mid-session changes
- Proper resource disposal on application close

## Project Overview
This is a Windows desktop application built with:
- **Avalonia UI** (version 11.2.2)
- **.NET 9**
- **Windows.Media.SpeechSynthesis** for text-to-speech functionality

## Code Style & Practices
- Use C# 12 features where appropriate
- Follow modern async/await patterns
- Use nullable reference types
- Keep code clean and well-commented
- Use meaningful variable and method names

## Testing & Verification
- **After making changes, run the application** using `dotnet run` so the user can see how it looks
- Only use `dotnet build MathTrainer.csproj` if you specifically need to check for compilation errors without showing the UI
- **Always run tests if they exist** using `dotnet test`
- Verify that the application compiles without errors before completing a task
- Check for compiler warnings and address them when possible

## Decision Making
- **If you are not sure what to do, please ask for clarification instead of just doing something and hoping for the best**
- When multiple approaches are possible, explain the tradeoffs and ask for preference
- If requirements are ambiguous, clarify before implementing

## Project-Specific Guidelines
- Text-to-speech should work on Windows using Windows.Media.SpeechSynthesis
- Language support is currently English and Dutch
- Keep the UI simple and functional - this is a utility app, not a showcase
- Configuration values should be easily adjustable (prefer constants or UI inputs over hardcoded values)
- Handle cancellation properly when stopping the training loop

## File Structure
- `Texts.cs` - Contains all localized strings for English and Dutch
- `MainWindow.axaml` - UI layout definition
- `MainWindow.axaml.cs` - Main application logic and event handlers
- `Program.cs` - Application entry point
- `App.axaml` / `App.axaml.cs` - Application-level setup
