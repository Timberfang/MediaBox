using ConsoleAppFramework;

using MediaBox.Encoding;

namespace MediaBox.Utilities;

public static class CommandLine
{
	private static readonly Dictionary<string, EncoderPreset> EncoderPresets =
		new() { { "quality", EncoderPreset.Quality }, { "normal", EncoderPreset.Normal } };
	private static readonly HashSet<string> AllowedTypes = ["video", "audio", "image"];
	private static readonly HashSet<string> AllowedPresets = ["quality", "normal"];
	private static readonly Dictionary<string, VideoCodec> VideoCodecNames = new() {
		{ "copy", VideoCodec.Copy },
		{ "avc", VideoCodec.AVC },
		{ "hevc", VideoCodec.HEVC },
		{ "av1", VideoCodec.AV1 }
	};
	private static readonly Dictionary<string, AudioCodec> AudioCodecNames = new() {
		{ "copy", AudioCodec.Copy },
		{ "mp3", AudioCodec.MP3 },
		{ "aac", AudioCodec.AAC },
		{ "opus", AudioCodec.OPUS }
	};
	private static readonly Dictionary<string, SubtitleCodec> SubtitleCodecNames = new() {
		{ "copy", SubtitleCodec.Copy },
		{ "srt", SubtitleCodec.SRT },
		{ "ssa", SubtitleCodec.SSA }
	};

	public static void StartCommandline(string[] args)
	{
		ConsoleApp.ConsoleAppBuilder app = ConsoleApp.Create();
		app.Add("transcode", Commands.Transcode);
		app.Run(args);
	}

	private static class Commands
	{
		/// <summary>
		///     Transcode media to a different format.
		/// </summary>
		/// <param name="type">-t, Type of media. Allowed values are "video" and "audio".</param>
		/// <param name="path">-p, Path to the media file or directory.</param>
		/// <param name="destination">-d, Path where the transcoded media will be saved.</param>
		/// <param name="preset">Preset for the media. Allowed values are "quality" and "normal".</param>
		/// <param name="noCrop">Whether video files should use crop detection to remove black borders.</param>
		/// <param name="videoCodec">What codec to use for video.</param>
		/// <param name="audioCodec">What codec to use for audio.</param>
		/// <param name="subtitleCodec">What codec to use for subtitles.</param>
		public static async Task Transcode(
			string type,
			string path,
			string destination,
			string preset = "normal",
			string videoCodec = "copy",
			string audioCodec = "copy",
			string subtitleCodec = "copy",
			bool noCrop = true,
			bool verbose = false)
		{
			videoCodec = videoCodec.ToLower();
			audioCodec = audioCodec.ToLower();
			subtitleCodec = subtitleCodec.ToLower();

			// [AllowedValues()] Could be used here, but it's not compatible with AOT.
			if (!AllowedTypes.Contains(type))
			{
				await Console.Error.WriteLineAsync("The type must be one of: 'video', 'audio', 'image'.");
				return;
			}
			if (!AllowedPresets.Contains(preset))
			{
				await Console.Error.WriteLineAsync("The preset must be one of: 'quality', 'normal'.");
				return;
			}
			if (!VideoCodecNames.TryGetValue(videoCodec, out VideoCodec targetVideoCodec))
			{
				await Console.Error.WriteLineAsync("The video codec must be one of: 'copy', 'avc', 'hevc', 'av1'.");
				return;
			}
			if (!AudioCodecNames.TryGetValue(audioCodec, out AudioCodec targetAudioCodec))
			{
				await Console.Error.WriteLineAsync("The audio codec must be one of: 'copy', 'mp3', 'aac', 'opus'.");
				return;
			}
			if (!SubtitleCodecNames.TryGetValue(subtitleCodec, out SubtitleCodec targetSubtitleCodec))
			{
				await Console.Error.WriteLineAsync("The subtitle codec must be one of: 'copy', 'srt', 'ssa'.");
				return;
			}
			if (!Directory.Exists(path) && !File.Exists(path))
				{
					await Console.Error.WriteLineAsync("Invalid path: " + path);
					return;
				}
			if (!noCrop && !type.Equals("video", StringComparison.OrdinalIgnoreCase))
			{
				await Console.Error.WriteLineAsync(
					"--no-crop only functions with video; this parameter will be ignored.");
			}

			// It's okay if the destination exists as a directory; the encoder will place files inside the directory.
			if (File.Exists(destination))
			{
				await Console.Error.WriteLineAsync("Destination already exists: " + destination);
				return;
			}
			try
			{
				if (verbose) { Console.WriteLine($"Preset is {preset}."); }
				switch (type)
				{
					case "video":
						if (verbose) { Console.WriteLine($"Encoding video with codecs: v=${videoCodec}, a=${audioCodec}, s=${subtitleCodec}."); }
						VideoEncoder videoEncoder = new(path, destination, EncoderPresets[preset.ToLowerInvariant()])
						{
							VideoCodec = targetVideoCodec,
							AudioCodec = targetAudioCodec,
							SubtitleCodec = targetSubtitleCodec,
						};
						videoEncoder.FileEncodingStarted +=
							(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
						await videoEncoder.EncodeAsync(!noCrop);
						break;
					case "audio":
						if (verbose) { Console.WriteLine($"Encoding audio with codec: {audioCodec}"); }
						AudioEncoder audioEncoder = new(path, destination, EncoderPresets[preset])
						{
							AudioCodec = targetAudioCodec
						};
						audioEncoder.FileEncodingStarted +=
							(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
						await audioEncoder.EncodeAsync();
						break;
					case "image":
						ImageEncoder imageEncoder = new(path, destination, EncoderPresets[preset]);
						imageEncoder.FileEncodingStarted +=
							(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
						await imageEncoder.EncodeAsync();
						break;
				}
			}
			catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
			{
				await Console.Error.WriteLineAsync("Path not found: " + path);
			}
			catch (InvalidDataException ex)
			{
				await Console.Error.WriteLineAsync($"FFprobe returned an invalid data format: {ex.Message}");
			}
		}
	}
}
