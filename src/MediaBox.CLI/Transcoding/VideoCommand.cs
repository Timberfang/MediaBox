using System.CommandLine;
using System.Runtime.InteropServices;
using MediaBox.Core.Encoding;
using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Encoding.Video;

namespace MediaBox.CLI.Transcoding;

public class VideoCommand : TranscodeCommand
{
	private static readonly Option<VideoCodec> s_videoCodecOption = new("--video-codec", "--vcodec", "-c:v")
	{
		Description = "Specify video codec.",
		DefaultValueFactory = _ => VideoCodec.Copy
	};

	private static readonly Option<AudioCodec> s_audioCodecOption = new("--audio-codec", "--acodec", "-c:a")
	{
		Description = "Specify audio codec.",
		DefaultValueFactory = _ => AudioCodec.Copy
	};

	private static readonly Option<SubtitleCodec> s_subtitleCodecOption = new("--subtitle-codec", "--scodec", "-c:s")
	{
		Description = "Specify subtitle codec.",
		DefaultValueFactory = _ => SubtitleCodec.Copy
	};

	private static readonly Option<VideoContainer> s_videoContainerOption =
	new("--video-container", "--container", "--format", "-f")
	{
		Description = "Specify video container.",
		DefaultValueFactory = _ => VideoContainer.MKV
	};

	public readonly Command Command = new("video", "Transcode video files")
	{
		SharedOptions.s_pathArgument,
		SharedOptions.s_destinationArgument,
		s_presetOption,
		s_videoCodecOption,
		s_audioCodecOption,
		s_subtitleCodecOption,
		s_videoContainerOption,
		SharedOptions.s_forceOption
	};

	public VideoCommand()
	{
		Command.SetAction((parseResult, cancellationToken) =>
		{
			string? path = parseResult.GetValue(SharedOptions.s_pathArgument);
			string? destination = parseResult.GetValue(SharedOptions.s_destinationArgument);
			EncoderPreset preset = parseResult.GetValue(s_presetOption);
			VideoCodec videoCodec = parseResult.GetValue(s_videoCodecOption);
			AudioCodec audioCodec = parseResult.GetValue(s_audioCodecOption);
			SubtitleCodec subtitleCodec = parseResult.GetValue(s_subtitleCodecOption);
			VideoContainer videoContainer = parseResult.GetValue(s_videoContainerOption);
			bool force = parseResult.GetValue(SharedOptions.s_forceOption);

			if (path is null)
			{
				return Console.Error.WriteLineAsync("Path cannot be null");
			}

			if (destination is null)
			{
				return Console.Error.WriteLineAsync("Destination cannot be null");
			}

			path = Path.GetFullPath(path);
			destination = Path.GetFullPath(destination);

			if (Path.HasExtension(path) && !Path.HasExtension(destination))
			{
				destination = Path.Join(destination, Path.GetFileName(path));
			}

			VideoEncoder encoder = new(path, destination, preset, videoCodec, audioCodec, subtitleCodec, videoContainer, force);
			return TranscodeVideo(encoder, cancellationToken);
		});
	}

	/// <summary>
	///     Transcodes video from one format to another.
	/// </summary>
	/// <param name="encoder"><see cref="VideoEncoder"/> configuration.</param>
	/// <param name="token"><see cref="CancellationToken"/> to cancel the encoding.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeVideo(
		VideoEncoder encoder,
		CancellationToken token
	)
	{
		encoder.FileEncodingStarted += (_, filePath) => Console.WriteLine($"Encoding file: \"{filePath}\"");
		encoder.Error += async (_, message) => await Console.Error.WriteLineAsync($"Error: {message}");
		try
		{
			await encoder.EncodeAsync(true, token);
			return 0;
		}
		catch (ExternalException e)
		{
			await Console.Error.WriteLineAsync($"ffmpeg error(s): \"{e.Message}\"");
			return 1;
		}
	}
}
