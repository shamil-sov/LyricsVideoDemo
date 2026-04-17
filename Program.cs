using System.Runtime.InteropServices;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extensions.Downloader;
using OpenAI.Audio;

// ──────────────────────────────────────────────────────────────────────
//  Lyrics Video Generator Demo
//
//  Takes an audio file, extracts lyrics as SRT via OpenAI Whisper,
//  then generates an MP4 lyrics video with FFmpeg.
//
//  Usage:
//    dotnet run -- <audioFilePath> <backgroundImagePath> <openAiApiKey>
//
//  Example:
//    dotnet run -- "C:\music\song.mp3" "C:\images\bg.png" "sk-..."
// ──────────────────────────────────────────────────────────────────────

if (args.Length < 3)
{
    Console.WriteLine("Usage: dotnet run -- <audioFilePath> <backgroundImagePath> <openAiApiKey>");
    Console.WriteLine();
    Console.WriteLine("  audioFilePath       Path to the audio file (mp3, m4a, wav, etc.)");
    Console.WriteLine("  backgroundImagePath Path to a background image (png, jpg)");
    Console.WriteLine("  openAiApiKey        Your OpenAI API key");
    return 1;
}

var audioFilePath = Path.GetFullPath(args[0]);
var backgroundImagePath = Path.GetFullPath(args[1]);
var openAiApiKey = args[2];

if (!File.Exists(audioFilePath))
{
    Console.Error.WriteLine($"Audio file not found: {audioFilePath}");
    return 1;
}

if (!File.Exists(backgroundImagePath))
{
    Console.Error.WriteLine($"Background image not found: {backgroundImagePath}");
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
