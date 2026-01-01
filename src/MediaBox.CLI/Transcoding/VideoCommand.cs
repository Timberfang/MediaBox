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
		Description = "The codec to use for video",
		DefaultValueFactory = _ => VideoCodec.Copy
	};

	private static readonly Option<AudioCodec> s_audioCodecOption = new("--audio-codec", "--acodec", "-c:a")
	{
		Description = "The codec to use for audio",
		DefaultValueFactory = _ => AudioCodec.Copy
	};

	private static readonly Option<SubtitleCodec> s_subtitleCodecOption = new("--subtitle-codec", "--scodec", "-c:s")
	{
		Description = "The codec to use for subtitles",
		DefaultValueFactory = _ => SubtitleCodec.Copy
	};

	private static readonly Option<VideoContainer> s_videoContainerOption =
	new("--video-container", "--container", "--format", "-f")
	{
		Description = "The container ('.mp4', '.mkv', '.webm', etc.).",
		DefaultValueFactory = _ => VideoContainer.MKV
	};

	public readonly Command Command = new("video", "transcode video to a different format")
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

			if ((videoContainer is VideoContainer.WEBM &&
				 videoCodec is not (VideoCodec.VP9 or VideoCodec.AV1 or VideoCodec.Copy)) ||
				audioCodec is not (AudioCodec.OPUS or AudioCodec.Copy))
			{
				Console.WriteLine(
					"Container 'webm' does not support selected codecs, using defaults...");
				videoContainer = VideoContainer.WEBM;
				videoCodec = VideoCodec.VP9;
				audioCodec = AudioCodec.OPUS;
			}

			return TranscodeVideo(path, destination, preset, videoCodec, audioCodec,
				subtitleCodec, videoContainer, force, cancellationToken);
		});
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
			Container = videoContainer,
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
}
