# Lyrics Video Generator

A simple CLI tool that takes a song audio file and produces a lyrics video (MP4).  
It uses **OpenAI Whisper** to transcribe the lyrics and **FFmpeg** to render the video.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An [OpenAI API key](https://platform.openai.com/api-keys) with Whisper access

> FFmpeg is **downloaded automatically** on first run — no manual install needed.

## Quick Start

1. Clone and configure:

```bash
git clone https://github.com/shamil-sov/LyricsVideoDemo.git
cd LyricsVideoDemo
```

2. Create `appsettings.json` in the project root with your OpenAI API key:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-key"
  }
}
```

3. Run:

```bash
dotnet run -- "path/to/song.mp3"
```

The generated `.srt` and `.mp4` files will appear in the `Output/` folder.

> **Note:** `appsettings.json` is git-ignored to prevent accidental key leaks.

## How It Works

1. **Extract lyrics** — The audio is sent to OpenAI Whisper which returns time-stamped lyrics in SRT format  
2. **Download FFmpeg** — FFmpeg binaries are auto-downloaded and cached (one-time)  
3. **Render video** — FFmpeg combines a background image (`background.png` included in the repo), the audio, and the SRT subtitles into an MP4

## Notes

- Supported audio formats: mp3, m4a, wav, etc. (anything Whisper accepts)
- To use a different background, just replace `background.png` in the project root
- Output video uses H.264 video + AAC audio for broad compatibility
