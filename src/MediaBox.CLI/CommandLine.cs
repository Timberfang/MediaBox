using System.CommandLine;
using System.Runtime.InteropServices;
using MediaBox.Core.Encoding;
using MediaBox.Core.Encoding.Audio;
using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Encoding.Image;
using MediaBox.Core.Encoding.Video;
using MediaBox.Core.Utility;

namespace MediaBox.CLI;

public static class CommandLine
{
	// IO
	private static readonly Argument<string> PathArgument = new("path")
	{
		Description = "Path to the input file or directory",
		Validators =
		{
			result =>
			{
				string path = result.Tokens[0].Value;
				if (!Path.Exists(path))
				{
					result.AddError($"Path at '{path}' does not exist");
				}
			}
		}
	};

	private static readonly Argument<string> DestinationArgument = new("destination")
	{
		Description = "Path to the output file or directory",
		Validators =
		{
			result =>
			{
				if (result.Tokens.Count <= 0)
				{
					return;
				}

				char[] invalidPathChars = Path.GetInvalidPathChars();
				char[] invalidChars = Path.GetInvalidFileNameChars();
				string path = result.Tokens[0].Value;
				if (path.Length == 0
					|| path.Any(c => invalidPathChars.Contains(c))
					|| Path.GetFileName(path).Any(c => invalidChars.Contains(c)))
				{
					result.AddError($"Path at '{path}' is invalid");
				}
			}
		},
		DefaultValueFactory = _ => Directory.GetCurrentDirectory()
	};

	// Transcoding
	private static readonly Option<EncoderPreset> PresetOption = new("--preset")
	{
		Description = "Quality preset for the media",
		DefaultValueFactory = _ => EncoderPreset.Normal
	};

	private static readonly Option<VideoCodec> VideoCodecOption = new("--video-codec")
	{
		Description = "The codec to use for video",
		DefaultValueFactory = _ => VideoCodec.Copy
	};

	private static readonly Option<AudioCodec> AudioCodecOption = new("--audio-codec")
	{
		Description = "The codec to use for audio",
		DefaultValueFactory = _ => AudioCodec.Copy
	};

	private static readonly Option<SubtitleCodec> SubtitleCodecOption = new("--subtitle-codec")
	{
		Description = "The codec to use for subtitles",
		DefaultValueFactory = _ => SubtitleCodec.Copy
	};

	private static readonly Option<ImageCodec> ImageCodecOption = new("--image-codec")
	{
		Description = "The codec to use for images",
		DefaultValueFactory = _ => ImageCodec.JPEG
	};

	private static readonly Option<VideoContainer> VideoContainerOption = new("--video-container")
	{
		Description = "The container to use for video",
		DefaultValueFactory = _ => VideoContainer.MKV
	};

	private static readonly Option<bool> ForceOption = new("--force")
	{
		Description = "Encode files even if their file extension already matches the target."
	};

	// Other
	private static readonly Option<bool> AboutOption = new("--about")
	{
		Description = "Get copyright information for MediaBox"
	};

	private static readonly Option<bool> ThirdPartyOption = new("--third-party-notices")
	{
		Description = "Get copyright information for bundled third-party software"
	};

	/// <summary>
	///     Parses command-line arguments.
	/// </summary>
	/// <param name="args">Arguments passed to the program.</param>
	/// <param name="ct">Cancellation token to cancel the process.</param>
	/// <returns>0 if the program was successful, and 1 if it was not.</returns>
	public static Task<int> StartCommandline(string[] args, CancellationToken ct = default)
	{
		// Transcoding commands
		Command videoCommand = new("video", "transcode video to a different format")
		{
			PathArgument,
			DestinationArgument,
			PresetOption,
			VideoCodecOption,
			AudioCodecOption,
			SubtitleCodecOption,
			VideoContainerOption,
			ForceOption
		};
		videoCommand.SetAction((parseResult, cancellationToken) =>
		{
			string? path = parseResult.GetValue(PathArgument);
			string? destination = parseResult.GetValue(DestinationArgument);
			EncoderPreset preset = parseResult.GetValue(PresetOption);
			VideoCodec videoCodec = parseResult.GetValue(VideoCodecOption);
			AudioCodec audioCodec = parseResult.GetValue(AudioCodecOption);
			SubtitleCodec subtitleCodec = parseResult.GetValue(SubtitleCodecOption);
			VideoContainer videoContainer = parseResult.GetValue(VideoContainerOption);
			bool force = parseResult.GetValue(ForceOption);

			if (path is null)
			{
				return Console.Error.WriteLineAsync("Path cannot be null");
			}

			if (destination is null)
			{
				return Console.Error.WriteLineAsync("Destination cannot be null");
			}

			if (Path.HasExtension(path) && !Path.HasExtension(destination))
			{
				destination = Path.Join(destination, Path.GetFileName(path));
			}

			if ((videoContainer == VideoContainer.WEBM &&
				 videoCodec != VideoCodec.VP9 && videoCodec != VideoCodec.Copy) ||
				(audioCodec != AudioCodec.OPUS && audioCodec != AudioCodec.Copy))
			{
				Console.WriteLine(
					"Container '.webm' always uses VP9 video and OPUS audio, ignoring configured codecs...");
				videoContainer = VideoContainer.WEBM;
				audioCodec = AudioCodec.OPUS;
			}

			return TranscodeVideo(path, destination, preset, videoCodec, audioCodec,
				subtitleCodec, videoContainer, force, cancellationToken);
		});
		Command audioCommand = new("audio", "transcode audio to a different format")
		{
			PathArgument,
			DestinationArgument,
			PresetOption,
			AudioCodecOption,
			ForceOption
		};
		audioCommand.SetAction((parseResult, cancellationToken) =>
		{
			string? path = parseResult.GetValue(PathArgument);
			string? destination = parseResult.GetValue(DestinationArgument);
			EncoderPreset preset = parseResult.GetValue(PresetOption);
			AudioCodec audioCodec = parseResult.GetValue(AudioCodecOption);
			bool force = parseResult.GetValue(ForceOption);

			if (path is null)
			{
				return Console.Error.WriteLineAsync("Path cannot be null");
			}

			if (destination is null)
			{
				return Console.Error.WriteLineAsync("Destination cannot be null");
			}

			if (Path.HasExtension(path) && !Path.HasExtension(destination))
			{
				destination = Path.Join(destination, Path.GetFileName(path));
			}

			return TranscodeAudio(path, destination, preset, audioCodec, force, cancellationToken);
		});
		Command imageCommand = new("image", "transcode images to a different format")
		{
			PathArgument,
			DestinationArgument,
			PresetOption,
			ImageCodecOption,
			ForceOption
		};
		imageCommand.SetAction(parseResult =>
		{
			string? path = parseResult.GetValue(PathArgument);
			string? destination = parseResult.GetValue(DestinationArgument);
			EncoderPreset preset = parseResult.GetValue(PresetOption);
			ImageCodec imageCodec = parseResult.GetValue(ImageCodecOption);
			bool force = parseResult.GetValue(ForceOption);

			if (path is null)
			{
				return Console.Error.WriteLineAsync("Path cannot be null");
			}

			if (destination is null)
			{
				return Console.Error.WriteLineAsync("Destination cannot be null");
			}

			if (Path.HasExtension(path) && !Path.HasExtension(destination))
			{
				destination = Path.Join(destination, Path.GetFileName(path));
			}

			return TranscodeImage(path, destination, preset, imageCodec, force);
		});

		// Transcoding command
		Command transcodeCommand = new("transcode", "Transcode media to a different format")
		{
			videoCommand, audioCommand, imageCommand
		};

		// Root command
		RootCommand rootCommand = new()
		{
			Description = "A wrapper for FFmpeg and libvips for video, audio, and image transcoding",
			Subcommands = { transcodeCommand },
			Options = { AboutOption, ThirdPartyOption }
		};
		rootCommand.SetAction(parseResult =>
			{
				if (parseResult.GetValue(AboutOption))
				{
					Console.WriteLine(License.Copyright);
				}
				else if (parseResult.GetValue(ThirdPartyOption))
				{
					string separator = Environment.NewLine +
									   "---------------------------------------------------------" +
									   Environment.NewLine;
					Console.WriteLine(string.Join(separator, parseResult.GetValue(AboutOption)));
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
	/// <param name="videoContainer">The container to use for video.</param>
	/// <param name="force">Encode files even if their file extension already matches the target.</param>
	/// <param name="cancellationToken">Token to cancel the encoding.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeVideo(
		string path,
		string destination,
		EncoderPreset preset,
		VideoCodec videoCodec,
		AudioCodec audioCodec,
		SubtitleCodec subtitleCodec,
		VideoContainer videoContainer,
		bool force,
		CancellationToken cancellationToken
	)
	{
		VideoEncoder videoEncoder = new(path, destination, preset)
		{
			VideoCodec = videoCodec,
			AudioCodec = audioCodec,
			SubtitleCodec = subtitleCodec,
			VideoContainer = videoContainer,
			Force = force
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
		catch (ExternalException e)
		{
			await Console.Error.WriteLineAsync($"ffmpeg error(s): \"{e.Message}\"");
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
	/// <param name="force">Encode files even if their file extension already matches the target.</param>
	/// <param name="cancellationToken">Token to cancel the encoding.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeAudio(
		string path,
		string destination,
		EncoderPreset preset,
		AudioCodec audioCodec,
		bool force,
		CancellationToken cancellationToken
	)
	{
		AudioEncoder audioEncoder = new(path, destination, preset) { AudioCodec = audioCodec, Force = force };
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
		catch (ExternalException e)
		{
			await Console.Error.WriteLineAsync($"ffmpeg error(s): \"{e.Message}\"");
			if (File.Exists(destination))
			{
				File.Delete(destination);
			}

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
	/// <param name="force">Encode files even if their file extension already matches the target.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeImage(
		string path,
		string destination,
		EncoderPreset preset,
		ImageCodec imageCodec,
		bool force
	)
	{
		ImageEncoder imageEncoder = new(path, destination, preset) { ImageCodec = imageCodec, Force = force };
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
}
