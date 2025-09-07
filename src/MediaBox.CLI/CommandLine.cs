using System.CommandLine;
using MediaBox.Core.Encoding;
using MediaBox.Core.Encoding.Audio;
using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Encoding.Image;
using MediaBox.Core.Encoding.Video;
using MediaBox.Core.Metadata;
using MediaBox.Core.Utility;

namespace MediaBox.CLI;

public static class CommandLine
{
	/// <summary>
	///     Parses command-line arguments.
	/// </summary>
	/// <param name="args">Arguments passed to the program.</param>
	/// <param name="ct">Cancellation token to cancel the process.</param>
	/// <returns>0 if the program was successful, and 1 if it was not.</returns>
	public static Task<int> StartCommandline(string[] args, CancellationToken ct = default)
	{
		// Options
		// IO
		Argument<PathInfo> pathArgument = new("path")
		{
			Description = "Path to the input file or directory",
			CustomParser = result => new PathInfo(Path.GetFullPath(result.Tokens[0].Value)),
			Validators =
			{
				result =>
				{
					PathInfo pathInfo = new(Path.GetFullPath(result.Tokens[0].Value));
					if (!pathInfo.Exists)
					{
						result.AddError($"Path at '{pathInfo.Path}' does not exist");
					}
				}
			}
		};
		Argument<PathInfo> destinationArgument = new("destination")
		{
			Description = "Path to the output file or directory",
			CustomParser = result => new PathInfo(Path.GetFullPath(result.Tokens[0].Value)),
			Validators =
			{
				result =>
				{
					PathInfo pathInfo = new(Path.GetFullPath(result.Tokens[0].Value));
					if (!pathInfo.IsValid)
					{
						result.AddError($"Path at '{pathInfo.Path}' is invalid");
					}
					// else if (!pathInfo.IsWritable) result.AddError($"Path at '{pathInfo.Path}' is not writable");
				}
			}
		};
		// Transcoding
		Option<MediaType> typeOption = new("--type", "-t")
		{
			Description = "Type of media",
			DefaultValueFactory = _ => MediaType.Unknown
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
		// Other
		Option<bool> aboutOption = new("--about") { Description = "Get copyright information for MediaBox" };
		Option<bool> thirdPartyOption = new("--third-party-notices")
		{
			Description = "Get copyright information for bundled third-party software"
		};

		// Transcoding command
		Command transcodeCommand = new("transcode", "Transcode media to a different format")
		{
			pathArgument,
			destinationArgument,
			typeOption,
			presetOption,
			videoCodecOption,
			audioCodecOption,
			subtitleCodecOption,
			imageCodecOption
		};
		transcodeCommand.SetAction((parseResult, cancellationToken) =>
		{
			MediaType type = parseResult.GetValue(typeOption);
			PathInfo? pathInfo = parseResult.GetValue(pathArgument);
			PathInfo? destinationInfo = parseResult.GetValue(destinationArgument);
			EncoderPreset preset = parseResult.GetValue(presetOption);
			VideoCodec videoCodec = parseResult.GetValue(videoCodecOption);
			AudioCodec audioCodec = parseResult.GetValue(audioCodecOption);
			SubtitleCodec subtitleCodec = parseResult.GetValue(subtitleCodecOption);
			ImageCodec imageCodec = parseResult.GetValue(imageCodecOption);
			if (pathInfo is null)
			{
				return Console.Error.WriteLineAsync("Path cannot be null");
			}

			if (destinationInfo is null)
			{
				return Console.Error.WriteLineAsync("Destination cannot be null");
			}

			if (type == MediaType.Unknown)
			{
				type = GuessMediaType(pathInfo);
			}

			return type switch
			{
				MediaType.Video => TranscodeVideo(pathInfo, destinationInfo, preset, videoCodec, audioCodec,
					subtitleCodec, cancellationToken),
				MediaType.Audio => TranscodeAudio(pathInfo, destinationInfo, preset, audioCodec, cancellationToken),
				MediaType.Image => TranscodeImage(pathInfo, destinationInfo, preset, imageCodec),
				MediaType.Other => Console.Error.WriteLineAsync("Media type is not supported"),
				MediaType.Unknown => Console.Error.WriteLineAsync(
					"Failed to detect media type, try adding the --type parameter"),
				_ => throw new ArgumentOutOfRangeException(type.ToString())
			};
		});

		// Root command
		RootCommand rootCommand = new()
		{
			Description = "A wrapper for FFmpeg and libvips for video, audio, and image transcoding",
			Subcommands = { transcodeCommand },
			Options = { aboutOption, thirdPartyOption }
		};
		rootCommand.SetAction(parseResult =>
			{
				if (parseResult.GetValue(aboutOption))
				{
					Console.WriteLine(License.Copyright);
				}
				else if (parseResult.GetValue(thirdPartyOption))
				{
					Console.WriteLine(License.ThirdPartyCopyright);
				}
			}
		);

		// Parse arguments
		return rootCommand.Parse(args).InvokeAsync(cancellationToken: ct);
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
		PathInfo path,
		PathInfo destination,
		EncoderPreset preset,
		VideoCodec videoCodec,
		AudioCodec audioCodec,
		SubtitleCodec subtitleCodec,
		CancellationToken cancellationToken
	)
	{
		VideoEncoder videoEncoder = new(path.Path, destination.Path, preset)
		{
			VideoCodec = videoCodec,
			AudioCodec = audioCodec,
			SubtitleCodec = subtitleCodec
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
		PathInfo path,
		PathInfo destination,
		EncoderPreset preset,
		AudioCodec audioCodec,
		CancellationToken cancellationToken
	)
	{
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
		PathInfo path,
		PathInfo destination,
		EncoderPreset preset,
		ImageCodec imageCodec
	)
	{
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

	private static MediaType GuessMediaType(PathInfo path)
	{
		HashSet<string> videoFilter = [".mkv", ".webm", ".mp4", ".m4v", ".m4a", ".avi", ".mov", ".qt", ".ogv"];
		HashSet<string> audioFilter = [".mp3", ".wav", ".flac", ".ogg", ".opus"];
		HashSet<string> imageFilter = [".jpg", ".jpeg", ".jfif", ".png", ".heif", ".heic", ".avif"];
		if (!File.Exists(path.Path))
		{
			return MediaType.Unknown;
		}

		string extension = Path.GetExtension(path.Path);
		if (videoFilter.Contains(extension))
		{
			return MediaType.Video;
		}

		if (audioFilter.Contains(extension))
		{
			return MediaType.Audio;
		}

		if (imageFilter.Contains(extension))
		{
			return MediaType.Image;
		}

		return MediaType.Unknown;
	}
}
