using System.CommandLine;

using MediaBox.Encoding;
using MediaBox.Metadata;

namespace MediaBox.Utilities;

public static class CommandLine
{
	/// <summary>
	/// 	Parses command-line arguments.
	/// </summary>
	/// <param name="args">Arguments passed to the program.</param>
	/// <returns>0 if the program was successful, and 1 if it was not.</returns>
	public static int StartCommandline(string[] args)
	{
		// Transcoding command
		Command transcodeCommand = new("transcode", "Transcode media to a different format");
		Option<MediaType> typeOption = new("--type", "-t")
		{
			Description = "Type of media",
			Required = true
		};
		Option<DirectoryInfo> transcodePathOption = new("--path", "-p")
		{
			Description = "Path to the media file or directory",
			Required = true
		};
		Option<DirectoryInfo> transcodeDestinationOption = new("-d", "--destination")
		{
			Description = "Path where the transcoded media will be saved",
			Required = true
		};
		Option<EncoderPreset> presetOption = new("--preset")
		{
			Description = "Quality preset for the media",
			DefaultValueFactory = _ => EncoderPreset.Normal
		};
		Option<VideoCodec> videoCodecOption = new("--video-codec")
		{
			Description = "The codec to use for video",
			DefaultValueFactory = _ => VideoCodec.Copy
		};
		Option<AudioCodec> audioCodecOption = new("--audio-codec")
		{
			Description = "The codec to use for audio",
			DefaultValueFactory = _ => AudioCodec.Copy
		};
		Option<SubtitleCodec> subtitleCodecOption = new("--subtitle-codec")
		{
			Description = "The codec to use for subtitles",
			DefaultValueFactory = _ => SubtitleCodec.Copy
		};
		Option<ImageCodec> imageCodecOption = new("--image-codec")
		{
			Description = "The codec to use for images",
			DefaultValueFactory = _ => ImageCodec.JPEG
		};
		transcodeCommand.Add(typeOption);
		transcodeCommand.Add(transcodePathOption);
		transcodeCommand.Add(transcodeDestinationOption);
		transcodeCommand.Add(presetOption);
		transcodeCommand.Add(videoCodecOption);
		transcodeCommand.Add(audioCodecOption);
		transcodeCommand.Add(subtitleCodecOption);
		transcodeCommand.SetAction(async parseResult =>
		{
			MediaType type = parseResult.GetValue(typeOption);
			DirectoryInfo? path = parseResult.GetValue(transcodePathOption);
			DirectoryInfo? destination = parseResult.GetValue(transcodeDestinationOption);
			EncoderPreset preset = parseResult.GetValue(presetOption);
			VideoCodec videoCodec = parseResult.GetValue(videoCodecOption);
			AudioCodec audioCodec = parseResult.GetValue(audioCodecOption);
			SubtitleCodec subtitleCodec = parseResult.GetValue(subtitleCodecOption);
			ImageCodec imageCodec = parseResult.GetValue(imageCodecOption);
			if (path == null)
			{
				await Console.Error.WriteLineAsync($"{path} cannot be null");
				return;
			}
			if (destination == null)
			{
				await Console.Error.WriteLineAsync($"{destination} cannot be null");
				return;
			}
			if (!path.Exists)
			{
				await Console.Error.WriteLineAsync($"Path at '{path} does not exist");
				return;
			}

			switch (type)
			{
				case MediaType.Video:
					await TranscodeVideo(path, destination, preset, videoCodec, audioCodec, subtitleCodec);
					break;
				case MediaType.Audio:
					await TranscodeAudio(path, destination, preset, audioCodec);
					break;
				case MediaType.Image:
					await TranscodeImage(path, destination, preset, imageCodec);
					break;
			}
		});

		// Show command
		Command showCommand = new("show", "Display metadata for media from a .json file");
		Option<FileInfo> showPathOption = new("--path", "-p")
		{
			Description = "Path to the metadata file",
			Required = true
		};
		showCommand.Add(showPathOption);
		showCommand.SetAction(parseResult =>
		{
			FileInfo? path = parseResult.GetValue(showPathOption);
			if (path == null)
			{
				Console.Error.WriteLine($"{path} cannot be null");
				return;
			}
			MediaInfo mediaInfo = new();
			mediaInfo.Load(path.FullName);
			Console.WriteLine(mediaInfo.ToString());
		});

		// Root command
		RootCommand rootCommand = [];
		rootCommand.Description = "A wrapper for FFmpeg and libvips for video, audio, and image transcoding";
		rootCommand.Add(transcodeCommand);
		rootCommand.Add(showCommand);
		Option<bool> aboutOption = new("--about")
		{
			Description = "Get copyright information for MediaBox"
		};
		Option<bool> thirdPartyOption = new("--third-party-notices")
		{
			Description = "Get copyright information for bundled third-party software"
		};
		rootCommand.Add(aboutOption);
		rootCommand.Add(thirdPartyOption);
		rootCommand.SetAction(parseResult =>
		{
			if (parseResult.GetValue(aboutOption)) { Console.WriteLine(Licenses.Copyright); }
			else if (parseResult.GetValue(thirdPartyOption)) { Console.WriteLine(Licenses.ThirdPartyCopyright); }
		}
		);

		// Parse arguments
		ParseResult parseResult = rootCommand.Parse(args);
		return parseResult.Invoke();
	}

	/// <summary>
	/// 	Transcodes video from one format to another.
	/// </summary>
	/// <param name="path">Path to the media file or directory.</param>
	/// <param name="destination">Path where the transcoded media will be saved.</param>
	/// <param name="preset">Quality preset for the media.</param>
	/// <param name="videoCodec">The codec to use for video.</param>
	/// <param name="audioCodec">The codec to use for audio.</param>
	/// <param name="subtitleCodec">The codec to use for subtitles.</param>
	/// <returns>A Task object.</returns>
	private static async Task TranscodeVideo(
		DirectoryInfo path,
		DirectoryInfo destination,
		EncoderPreset preset,
		VideoCodec videoCodec,
		AudioCodec audioCodec,
		SubtitleCodec subtitleCodec
		)
	{
		VideoEncoder videoEncoder = new(path.FullName, destination.FullName, preset)
		{
			VideoCodec = videoCodec,
			AudioCodec = audioCodec,
			SubtitleCodec = subtitleCodec,
		};
		videoEncoder.FileEncodingStarted +=
			(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
		await videoEncoder.EncodeAsync();
	}

	/// <summary>
	/// 	Transcodes audio from one format to another.
	/// </summary>
	/// <param name="path">Path to the media file or directory.</param>
	/// <param name="destination">Path where the transcoded media will be saved.</param>
	/// <param name="preset">Quality preset for the media.</param>
	/// <param name="audioCodec">The codec to use for audio.</param>
	/// <returns>A Task object.</returns>
	private static async Task TranscodeAudio(
		DirectoryInfo path,
		DirectoryInfo destination,
		EncoderPreset preset,
		AudioCodec audioCodec
	)
	{
		AudioEncoder audioEncoder = new(path.FullName, destination.FullName, preset)
		{
			AudioCodec = audioCodec,
		};
		audioEncoder.FileEncodingStarted +=
			(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
		await audioEncoder.EncodeAsync();
	}

	/// <summary>
	/// 	Transcodes image from one format to another.
	/// </summary>
	/// <param name="path">Path to the media file or directory.</param>
	/// <param name="destination">Path where the transcoded media will be saved.</param>
	/// <param name="preset">Quality preset for the media.</param>
	/// <param name="imageCodec">The codec to use for images.</param>
	/// <returns>A Task object.</returns>
	private static async Task TranscodeImage(
		DirectoryInfo path,
		DirectoryInfo destination,
		EncoderPreset preset,
		ImageCodec imageCodec
	)
	{
		ImageEncoder imageEncoder = new(path.FullName, destination.FullName, preset)
		{
			ImageCodec = imageCodec
		};
		imageEncoder.FileEncodingStarted +=
			(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
		await imageEncoder.EncodeAsync();
	}
}
