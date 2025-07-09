using ConsoleAppFramework;
using Cysharp.Diagnostics;
using MediaBox.Encoding;

namespace MediaBox.Utilities;

public static class CommandLine
{
	private static readonly Dictionary<string, EncoderPreset> s_encoderPresets =
		new() { { "quality", EncoderPreset.Quality }, { "normal", EncoderPreset.Normal } };
	private static readonly string[] s_allowedTypes = ["video", "audio", "image"];
	private static readonly string[] s_allowedPresets = ["quality", "normal"];

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
		public static async Task Transcode(
			string type,
			string path,
			string destination,
			string preset = "normal",
			bool noCrop = true)
		{
			// [AllowedValues()] Could be used here, but it's not compatible with AOT.
			if (!s_allowedTypes.Contains(type))
			{
				await Console.Error.WriteLineAsync("The type must be one of: 'video', 'audio', 'image'.");
				return;
			}
			if (!s_allowedPresets.Contains(preset))
			{
				await Console.Error.WriteLineAsync("The preset must be one of: 'quality', 'normal'.");
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
				switch (type)
				{
					case "video":
						VideoEncoder videoEncoder = new(path, destination, s_encoderPresets[preset.ToLowerInvariant()]);
						videoEncoder.FileEncodingStarted +=
							(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
						await videoEncoder.EncodeAsync(!noCrop);
						break;
					case "audio":
						AudioEncoder audioEncoder = new(path, destination, s_encoderPresets[preset]);
						audioEncoder.FileEncodingStarted +=
							(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
						await audioEncoder.EncodeAsync();
						break;
					case "image":
						ImageEncoder imageEncoder = new(path, destination, s_encoderPresets[preset]);
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
			catch (ProcessErrorException ex)
			{
				await Console.Error.WriteLineAsync($"FFmpeg crashed with error code {ex.ExitCode}:");
				Console.Error.WriteLine(ex.ErrorOutput);
			}
			catch (InvalidDataException ex)
			{
				await Console.Error.WriteLineAsync($"FFprobe returned an invalid data format: {ex.Message}");
			}
		}
	}
}
