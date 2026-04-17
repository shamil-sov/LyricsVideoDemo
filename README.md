# Lyrics Video Generator

A CLI tool that takes a song audio file and produces a lyrics video (MP4).  
It uses **OpenAI Whisper** to transcribe the lyrics and **FFmpeg** to render the video with subtitles.

---

## Prerequisites

### 1. Install .NET 8 SDK

Download and install the .NET 8 SDK from the official site:

**https://dotnet.microsoft.com/download/dotnet/8.0**

Verify the installation by running:

```bash
dotnet --version
```

You should see a version starting with `8.x.x`.

### 2. Get an OpenAI API key

Sign up or log in at **https://platform.openai.com/api-keys** and create a new API key.  
The key starts with `sk-` and is required for the Whisper transcription step.

> FFmpeg is **downloaded automatically** on first run — no manual install needed.

---

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/shamil-sov/LyricsVideoDemo.git
cd LyricsVideoDemo
```

### 2. Configure your API key

Create a file called `appsettings.json` in the project root:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-key"
  }
}
```

Replace `sk-your-openai-key` with your actual OpenAI API key.

> **Important:** `appsettings.json` is listed in `.gitignore` so your key is never committed to the repository.

---

## Usage

```bash
dotnet run -- <audioFilePath>
```

### Example

```bash
dotnet run -- "C:\music\song.mp3"
```

### Example output

```
Step 1/3: Extracting lyrics from audio via OpenAI Whisper...
   SRT saved to: C:\...\Output\song.srt
Step 2/3: Ensuring FFmpeg is available...
   FFmpeg ready.
Step 3/3: Generating lyrics video...

Done! Video saved to: C:\...\Output\song-lyrics.mp4
```

The generated files are saved in the `Output/` folder:

| File | Description |
|------|-------------|
| `song.srt` | Time-stamped lyrics in SRT subtitle format |
| `song-lyrics.mp4` | Final lyrics video with embedded subtitles |

---

## How It Works

1. **Extract lyrics** — The audio is sent to OpenAI Whisper, which returns time-stamped lyrics in SRT format
2. **Download FFmpeg** — FFmpeg binaries are auto-downloaded and cached (one-time)
3. **Render video** — FFmpeg combines a background image, the audio, and the SRT subtitles into an MP4 video

---

## Notes

- **Supported audio formats:** mp3, m4a, wav, flac, ogg, webm
- **Custom background:** Replace `background.png` in the project root to use your own image
- **Video format:** H.264 video + AAC audio for broad compatibility
