# Square Root Trainer

A desktop application that helps you memorize square roots of numbers 1-20 through audio-based spaced repetition. Currently runs on Windows, with browser support planned.

![Square Root Trainer Application](docs/screenshot.png)

## What It Does

Square Root Trainer uses pre-generated audio files to quiz you on square roots at regular intervals. The app plays a question (like "What is the square root of 144?"), gives you time to think, and then tells you the answer. This hands-free approach lets you practice while doing other tasks.

## How to Use

1. **Choose your language** - English or Dutch
2. **Set your timing preferences**:
   - Time to answer: How many seconds you get to think (default: 3)
   - Interval time: How long to wait between questions (default: 5)
3. **Click "Start Training"** and listen to the questions
4. **Think of the answer** during the pause
5. **Hear the correct answer** and repeat

The app randomly selects from square roots 1-20 (perfect squares 1-400), helping you build familiarity through repetition.

## Requirements

- Windows 10 or later
- .NET 9.0 Runtime

## Installation

### Running the Application

From the repository root:
```powershell
dotnet run --project SquareRootTrainer/SquareRootTrainer.csproj
```

Or navigate to the project folder:
```powershell
cd SquareRootTrainer
dotnet run
```

### Building

```powershell
dotnet build
```

## Technical Details

### Audio Files

The application uses pre-generated audio files for both English and Dutch. All audio files are located in:
- `SquareRootTrainer/audio/en-US/` - English audio files
- `SquareRootTrainer/audio/nl-NL/` - Dutch audio files

### Generating Audio Files

Audio files are generated using the AudioGenerator CLI tool, which uses Windows Speech Synthesis:

```powershell
cd AudioGenerator
dotnet run
```

This will regenerate all audio files in both languages.

### Architecture

- **Cross-platform audio**: Uses NAudio for Windows desktop audio playback
- **Platform abstraction**: `IAudioPlayer` interface allows for different implementations (Windows/Browser)
- **Avalonia UI**: Cross-platform .NET UI framework
- **Async/await patterns**: Clean cancellation and resource management
