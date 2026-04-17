# Lyrics Video Generator

Generates a lyrics video from an audio file using OpenAI Whisper for transcription and FFmpeg for video rendering.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An [OpenAI API key](https://platform.openai.com/api-keys) (Whisper access)

FFmpeg is downloaded automatically on first run — no manual install needed.

## Usage

```bash
dotnet run -- <audioFilePath> <backgroundImagePath> <openAiApiKey>
```

### Example

```bash
dotnet run -- "C:\music\song.mp3" "C:\images\background.png" "sk-your-key-here"
```

## What it does

1. Sends the audio to **OpenAI Whisper** and extracts lyrics as an SRT subtitle file
2. Downloads **FFmpeg** binaries automatically (one-time, cached in temp)
3. Generates an **MP4 video** with the background image, audio, and burned-in lyric subtitles

Output files (`.srt` and `.mp4`) are saved to an `Output/` folder in the project directory.
