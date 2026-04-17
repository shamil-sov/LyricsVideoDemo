using System.Runtime.InteropServices;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extensions.Downloader;
using Microsoft.Extensions.Configuration;
using OpenAI.Audio;

// ──────────────────────────────────────────────────────────────────────
//  Lyrics Video Generator
//
//  Takes an audio file, extracts lyrics as SRT via OpenAI Whisper,
//  then generates an MP4 lyrics video with FFmpeg.
//
//  Usage:
//    dotnet run -- <audioFilePath>
//
//  Example:
//    dotnet run -- "C:\music\song.mp3"
//
//  Configuration:
//    Set your OpenAI API key in appsettings.json
// ──────────────────────────────────────────────────────────────────────

if (args.Length < 1)
{
    Console.Error.WriteLine("Error: No audio file specified.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage: dotnet run -- <audioFilePath>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  audioFilePath  Path to the audio file (mp3, m4a, wav, flac, ogg, webm)");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine("  dotnet run -- \"C:\\music\\song.mp3\"");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Make sure your OpenAI API key is configured in appsettings.json");
    return 1;
}

var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(configPath))
{
    Console.Error.WriteLine("Error: appsettings.json not found.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Create appsettings.json in the project root with the following content:");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  {");
    Console.Error.WriteLine("    \"OpenAI\": {");
    Console.Error.WriteLine("      \"ApiKey\": \"sk-your-key-here\"");
    Console.Error.WriteLine("    }");
    Console.Error.WriteLine("  }");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var openAiApiKey = configuration["OpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(openAiApiKey) || openAiApiKey == "YOUR_OPENAI_API_KEY_HERE")
{
    Console.Error.WriteLine("Error: OpenAI API key is not configured.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Open appsettings.json and replace the placeholder with your actual API key:");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  {");
    Console.Error.WriteLine("    \"OpenAI\": {");
    Console.Error.WriteLine("      \"ApiKey\": \"sk-your-key-here\"");
    Console.Error.WriteLine("    }");
    Console.Error.WriteLine("  }");
    Console.Error.WriteLine();
    Console.Error.WriteLine("You can get an API key at: https://platform.openai.com/api-keys");
    return 1;
}

var audioFilePath = Path.GetFullPath(args[0]);

var backgroundImagePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "background.png");
backgroundImagePath = Path.GetFullPath(backgroundImagePath);

if (!File.Exists(audioFilePath))
{
    Console.Error.WriteLine($"Error: Audio file not found: {audioFilePath}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Please check that:");
    Console.Error.WriteLine("  - The file path is correct");
    Console.Error.WriteLine("  - The file exists and is accessible");
    Console.Error.WriteLine("  - Supported formats: mp3, m4a, wav, flac, ogg, webm");
    return 1;
}

if (!File.Exists(backgroundImagePath))
{
    Console.Error.WriteLine($"Background image not found: {backgroundImagePath}");
    Console.Error.WriteLine("Make sure background.png is in the project root.");
    return 1;
}

var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Output");
Directory.CreateDirectory(outputDir);

// ── Step 1: Extract lyrics SRT via OpenAI Whisper ────────────────────

Console.WriteLine("Step 1/3: Extracting lyrics from audio via OpenAI Whisper...");

var srtPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(audioFilePath) + ".srt");

var client = new AudioClient(model: "whisper-1", apiKey: openAiApiKey);

var transcriptionOptions = new AudioTranscriptionOptions
{
    ResponseFormat = AudioTranscriptionFormat.Srt,
    Prompt = "Transcribe only the sung lyrics from this audio. "
           + "Preserve repeated words and short repeated phrases. "
           + "Format subtitles as short natural lyric phrases, not long speech paragraphs. "
           + "Prefer short subtitle blocks, maximum 2 lines per subtitle. "
           + "Do not add commentary, outro text, or words that are not actually sung."
};

AudioTranscription transcription = await client.TranscribeAudioAsync(audioFilePath, transcriptionOptions);

await File.WriteAllTextAsync(srtPath, transcription.Text);
Console.WriteLine($"   SRT saved to: {srtPath}");

// ── Step 2: Ensure FFmpeg is available ───────────────────────────────

Console.WriteLine("Step 2/3: Ensuring FFmpeg is available...");

var ffmpegDir = Path.Combine(Path.GetTempPath(), "ffmpegcore");
Directory.CreateDirectory(ffmpegDir);
GlobalFFOptions.Configure(o => o.BinaryFolder = ffmpegDir);

var ffmpegName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
var ffprobeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffprobe.exe" : "ffprobe";

if (!File.Exists(Path.Combine(ffmpegDir, ffmpegName)) ||
    !File.Exists(Path.Combine(ffmpegDir, ffprobeName)))
{
    Console.WriteLine("   Downloading FFmpeg binaries (one-time)...");
    await FFMpegDownloader.DownloadBinaries();
}

Console.WriteLine("   FFmpeg ready.");

// ── Step 3: Generate the lyrics video ────────────────────────────────

Console.WriteLine("Step 3/3: Generating lyrics video...");

var videoOutputPath = Path.Combine(
    outputDir,
    Path.GetFileNameWithoutExtension(audioFilePath) + "-lyrics.mp4");

var escapedSrtPath = Path.GetFullPath(srtPath)
    .Replace("\\", "/")
    .Replace(":", "\\:")
    .Replace("'", "\\'");

var subtitleFilter =
    $"subtitles=filename='{escapedSrtPath}':force_style='Alignment=2,MarginV=60,FontSize=18'";

var videoFilter =
    $"{subtitleFilter},scale=trunc(iw/2)*2:trunc(ih/2)*2";

await FFMpegArguments
    .FromFileInput(backgroundImagePath, verifyExists: true, options => options
        .WithCustomArgument("-loop 1"))
    .AddFileInput(audioFilePath, verifyExists: true)
    .OutputToFile(
        videoOutputPath,
        overwrite: true,
        options => options
            .WithCustomArgument($"-vf \"{videoFilter}\"")
            .WithVideoCodec(VideoCodec.LibX264)
            .WithAudioCodec(AudioCodec.Aac)
            .WithCustomArgument("-map 0:v:0")
            .WithCustomArgument("-map 1:a:0")
            .WithCustomArgument("-pix_fmt yuv420p")
            .WithCustomArgument("-tune stillimage")
            .WithCustomArgument("-shortest")
            .WithFastStart())
    .ProcessAsynchronously();

Console.WriteLine();
Console.WriteLine($"Done! Video saved to: {videoOutputPath}");

return 0;
