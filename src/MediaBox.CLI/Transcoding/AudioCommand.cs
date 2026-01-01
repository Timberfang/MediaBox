using System.CommandLine;
using System.Runtime.InteropServices;
using MediaBox.Core.Encoding;
using MediaBox.Core.Encoding.Audio;
using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.CLI.Transcoding;

public class AudioCommand : TranscodeCommand
{
	private static readonly Option<AudioCodec> s_audioCodecOption = new("--audio-codec", "--acodec", "-c:a")
	{
		Description = "The codec to use for audio",
		DefaultValueFactory = _ => AudioCodec.Copy
	};

	public readonly Command Command = new("audio", "transcode audio to a different format")
	{
		SharedOptions.s_pathArgument,
		SharedOptions.s_destinationArgument,
		s_presetOption,
		s_audioCodecOption,
		SharedOptions.s_forceOption
	};

	public AudioCommand()
	{
		Command.SetAction((parseResult, cancellationToken) =>
		{
			string? path = parseResult.GetValue(SharedOptions.s_pathArgument);
			string? destination = parseResult.GetValue(SharedOptions.s_destinationArgument);
			EncoderPreset preset = parseResult.GetValue(s_presetOption);
			AudioCodec audioCodec = parseResult.GetValue(s_audioCodecOption);
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

			return TranscodeAudio(path, destination, preset, audioCodec, force, cancellationToken);
		});
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
}
