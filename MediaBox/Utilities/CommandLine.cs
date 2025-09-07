using System.CommandLine;

using MediaBox.Encoding;
using MediaBox.Metadata;

namespace MediaBox.Utilities;

public static class CommandLine
{
	/// <summary>
	///     Parses command-line arguments.
	/// </summary>
	/// <param name="args">Arguments passed to the program.</param>
	/// <returns>0 if the program was successful, and 1 if it was not.</returns>
	public static Task<int> StartCommandline(string[] args)
	{
		// Options
		// IO
		Option<PathInfo> pathOption = new("--path", "-p")
		{
			Description = "Path to the input file or directory",
			Required = true,
			CustomParser = result => new PathInfo(Path.GetFullPath(result.Tokens[0].Value)),
			Validators =
			{
				result =>
				{
					PathInfo pathInfo = new(Path.GetFullPath(result.Tokens[0].Value));
					if (!pathInfo.Exists) { result.AddError($"Path at '{pathInfo.Path}' does not exist"); }
				}
			}
		};
		Option<PathInfo> destinationOption = new("-d", "--destination")
		{
			Description = "Path to the output file or directory",
			Required = true,
			CustomParser = result => new PathInfo(Path.GetFullPath(result.Tokens[0].Value)),
			Validators =
			{
				result =>
				{
					PathInfo pathInfo = new(Path.GetFullPath(result.Tokens[0].Value));
					if (!pathInfo.IsValid) { result.AddError($"Path at '{pathInfo.Path}' is invalid"); }
					else if (!pathInfo.IsWritable) { result.AddError($"Path at '{pathInfo.Path}' is not writable"); }
				}
			}
		};
		// Transcoding
		Option<MediaType> typeOption = new("--type", "-t") { Description = "Type of media", Required = true };
		Option<EncoderPreset> presetOption = new("--preset")
		{
			Description = "Quality preset for the media", DefaultValueFactory = _ => EncoderPreset.Normal
		};
		Option<VideoCodec> videoCodecOption = new("--video-codec")
		{
			Description = "The codec to use for video", DefaultValueFactory = _ => VideoCodec.Copy
		};
		Option<AudioCodec> audioCodecOption = new("--audio-codec")
		{
			Description = "The codec to use for audio", DefaultValueFactory = _ => AudioCodec.Copy
		};
		Option<SubtitleCodec> subtitleCodecOption = new("--subtitle-codec")
		{
			Description = "The codec to use for subtitles", DefaultValueFactory = _ => SubtitleCodec.Copy
		};
		Option<ImageCodec> imageCodecOption = new("--image-codec")
		{
			Description = "The codec to use for images", DefaultValueFactory = _ => ImageCodec.JPEG
		};
		// Metadata
		Option<string> metadataTitleOption = new("--title") { Description = "Title of the media", Required = true };
		Option<string> metadataDescriptionOption = new("--description")
		{
			Description = "Description of the media", Required = true
		};
		// Other
		Option<bool> aboutOption = new("--about") { Description = "Get copyright information for MediaBox" };
		Option<bool> thirdPartyOption = new("--third-party-notices")
		{
			Description = "Get copyright information for bundled third-party software"
		};

		// Transcoding command
		Command transcodeCommand = new("transcode", "Transcode media to a different format")
		{
			typeOption,
			pathOption,
			destinationOption,
			presetOption,
			videoCodecOption,
			audioCodecOption,
			subtitleCodecOption
		};
		transcodeCommand.SetAction((parseResult, cancellationToken) =>
		{
			MediaType type = parseResult.GetValue(typeOption);
			PathInfo? pathInfo = parseResult.GetValue(pathOption);
			PathInfo? destinationInfo = parseResult.GetValue(destinationOption);
			EncoderPreset preset = parseResult.GetValue(presetOption);
			VideoCodec videoCodec = parseResult.GetValue(videoCodecOption);
			AudioCodec audioCodec = parseResult.GetValue(audioCodecOption);
			SubtitleCodec subtitleCodec = parseResult.GetValue(subtitleCodecOption);
			ImageCodec imageCodec = parseResult.GetValue(imageCodecOption);
			return type switch
			{
				MediaType.Video => TranscodeVideo(pathInfo, destinationInfo, preset, videoCodec, audioCodec,
					subtitleCodec, cancellationToken),
				MediaType.Audio => TranscodeAudio(pathInfo, destinationInfo, preset, audioCodec, cancellationToken),
				MediaType.Image => TranscodeImage(pathInfo, destinationInfo, preset, imageCodec),
				_ => throw new ArgumentOutOfRangeException(type.ToString())
			};
		});

		// Load command
		Command loadCommand = new("load", "Load media information from a .json file") { pathOption };
		loadCommand.SetAction(parseResult =>
		{
			PathInfo? pathInfo = parseResult.GetValue(pathOption);
			return LoadMetadata(pathInfo);
		});

		// Save command
		Command saveCommand = new("save", "Save media information to a .json file")
		{
			destinationOption, metadataTitleOption, metadataDescriptionOption
		};
		saveCommand.SetAction(parseResult =>
		{
			PathInfo? destinationInfo = parseResult.GetValue(destinationOption);
			string? title = parseResult.GetValue(metadataTitleOption);
			string? description = parseResult.GetValue(metadataDescriptionOption);
			return SaveMetadata(destinationInfo, title, description);
		});

		// Root command
		RootCommand rootCommand = new()
		{
			Description = "A wrapper for FFmpeg and libvips for video, audio, and image transcoding",
			Subcommands = { transcodeCommand, loadCommand, saveCommand },
			Options = { aboutOption, thirdPartyOption }
		};
		rootCommand.SetAction(parseResult =>
			{
				if (parseResult.GetValue(aboutOption)) { Console.WriteLine(Licenses.Copyright); }
				else if (parseResult.GetValue(thirdPartyOption)) { Console.WriteLine(Licenses.ThirdPartyCopyright); }
			}
		);

		// Parse arguments
		return rootCommand.Parse(args).InvokeAsync();
	}

	/// <summary>
	///     Transcodes video from one format to another.
	/// </summary>
	/// <param name="path">Path to the media file or directory.</param>
	/// <param name="destination">Path where the transcoded media will be saved.</param>
	/// <param name="preset">Quality preset for the media.</param>
	/// <param name="videoCodec">The codec to use for video.</param>
	/// <param name="audioCodec">The codec to use for audio.</param>
	/// <param name="subtitleCodec">The codec to use for subtitles.</param>
	/// <param name="cancellationToken">Token to cancel the encoding.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeVideo(
		PathInfo? path,
		PathInfo? destination,
		EncoderPreset preset,
		VideoCodec videoCodec,
		AudioCodec audioCodec,
		SubtitleCodec subtitleCodec,
		CancellationToken cancellationToken
	)
	{
		if (path is null)
		{
			await Console.Error.WriteLineAsync("Path cannot be null");
			return 1;
		}

		if (destination is null)
		{
			await Console.Error.WriteLineAsync("Destination cannot be null");
			return 1;
		}

		VideoEncoder videoEncoder = new(path.Path, destination.Path, preset)
		{
			VideoCodec = videoCodec, AudioCodec = audioCodec, SubtitleCodec = subtitleCodec
		};
		videoEncoder.FileEncodingStarted +=
			(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
		try
		{
			await videoEncoder.EncodeAsync(true, cancellationToken);
			return 0;
		}
		catch (OperationCanceledException)
		{
			await Console.Error.WriteLineAsync("The operation was aborted");
			return 1;
		}
	}

	/// <summary>
	///     Transcodes audio from one format to another.
	/// </summary>
	/// <param name="path">Path to the media file or directory.</param>
	/// <param name="destination">Path where the transcoded media will be saved.</param>
	/// <param name="preset">Quality preset for the media.</param>
	/// <param name="audioCodec">The codec to use for audio.</param>
	/// <param name="cancellationToken">Token to cancel the encoding.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeAudio(
		PathInfo? path,
		PathInfo? destination,
		EncoderPreset preset,
		AudioCodec audioCodec,
		CancellationToken cancellationToken
	)
	{
		if (path is null)
		{
			await Console.Error.WriteLineAsync("Path cannot be null");
			return 1;
		}

		if (destination is null)
		{
			await Console.Error.WriteLineAsync("Destination cannot be null");
			return 1;
		}

		AudioEncoder audioEncoder = new(path.Path, destination.Path, preset) { AudioCodec = audioCodec };
		audioEncoder.FileEncodingStarted +=
			(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
		try
		{
			await audioEncoder.EncodeAsync(cancellationToken);
			return 0;
		}
		catch (OperationCanceledException)
		{
			await Console.Error.WriteLineAsync("The operation was aborted");
			return 1;
		}
	}

	/// <summary>
	///     Transcodes image from one format to another.
	/// </summary>
	/// <param name="path">Path to the media file or directory.</param>
	/// <param name="destination">Path where the transcoded media will be saved.</param>
	/// <param name="preset">Quality preset for the media.</param>
	/// <param name="imageCodec">The codec to use for images.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeImage(
		PathInfo? path,
		PathInfo? destination,
		EncoderPreset preset,
		ImageCodec imageCodec
	)
	{
		if (path is null)
		{
			await Console.Error.WriteLineAsync("Path cannot be null");
			return 1;
		}

		if (destination is null)
		{
			await Console.Error.WriteLineAsync("Destination cannot be null");
			return 1;
		}

		ImageEncoder imageEncoder = new(path.Path, destination.Path, preset) { ImageCodec = imageCodec };
		imageEncoder.FileEncodingStarted +=
			(_, filePath) => Console.WriteLine($"Encoding file: {filePath}");
		try
		{
			await imageEncoder.EncodeAsync();
			return 0;
		}
		catch (OperationCanceledException)
		{
			await Console.Error.WriteLineAsync("The operation was aborted");
			return 1;
		}
	}

	private static async Task<int> LoadMetadata(PathInfo? path)
	{
		if (path is null)
		{
			await Console.Error.WriteLineAsync("Path cannot be null");
			return 1;
		}

		MediaInfo mediaInfo = await Import.ImportMetadataAsync(path.Path);
		Console.WriteLine(mediaInfo.ToString());
		return 0;
	}

	private static async Task<int> SaveMetadata(PathInfo? path, string? title, string? description)
	{
		if (path is null)
		{
			await Console.Error.WriteLineAsync("Destination cannot be null");
			return 1;
		}

		MediaInfo mediaInfo = new(title, description);
		await Export.ExportMetadataAsync(path.Path, mediaInfo);
		return 0;
	}
}
