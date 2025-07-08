using ConsoleAppFramework;
using MediaBox.Encoding;

namespace MediaBox.Utilities;

public static class CommandLine
{
	private static readonly Dictionary<string, EncoderPreset> s_encoderPresets =
		new() { { "quality", EncoderPreset.Quality }, { "normal", EncoderPreset.Normal } };
	private static readonly string[] s_allowedTypes = ["video", "audio"];
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
		/// <param name="type">-t, Type of media. The only allowed value is "video".</param>
		/// <param name="path">-p, Path to the media file or directory.</param>
		/// <param name="destination">-d, Path where the transcoded media will be saved.</param>
		/// <param name="preset">Preset for the media. Allowed values are "quality" and "normal".</param>
		public static async Task Transcode(
			string type,
			string path,
			string destination,
			string preset = "normal")
		{
			// [AllowedValues()] Could be used here, but it's not compatible with AOT.
			if (!s_allowedTypes.Contains(type))
			{
				Console.WriteLine("The type must be one of: 'video', 'audio'.");
				return;
			}
			if (!s_allowedPresets.Contains(preset))
			{
				Console.WriteLine("The preset must be one of: 'quality', 'normal'.");
				return;
			}
			if (!Directory.Exists(path) && !File.Exists(path))
			{
				Console.WriteLine("Invalid path: " + path);
				return;
			}

			// It's okay if the destination exists as a directory; the encoder will place files inside the directory.
			if (File.Exists(destination))
			{
				Console.WriteLine("Destination already exists: " + destination);
				return;
			}
			switch (type)
			{
				case "video":
					VideoEncoder videoEncoder = new(path, destination, s_encoderPresets[preset.ToLowerInvariant()]);
					await videoEncoder.EncodeAsync();
					break;
				case "audio":
					AudioEncoder audioEncoder = new(path, destination, s_encoderPresets[preset]);
					await audioEncoder.EncodeAsync();
					break;
			}
		}
	}
}
