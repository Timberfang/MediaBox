using System.CommandLine;
using System.Runtime.InteropServices;
using MediaBox.Core.Encoding;
using MediaBox.Core.Encoding.Audio;
using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.CLI.Transcoding;

public class AudioCommand : TranscodeCommand
{
	private static readonly Option<AudioCodec> s_audioCodecOption = new("--audio-codec", "--acodec", "-c:a", "-c")
	{
		Description = "Specify audio codec.",
		DefaultValueFactory = _ => AudioCodec.Copy
	};

	public readonly Command Command = new("audio", "Transcode audio files.")
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

			AudioEncoder encoder = new(path, destination, preset, audioCodec, force);
			return TranscodeAudio(encoder, cancellationToken);
		});
	}

	/// <summary>
	///     Transcodes audio from one format to another.
	/// </summary>
	/// <param name="encoder"><see cref="AudioEncoder"/> configuration.</param>
	/// <param name="token"><see cref="CancellationToken"/> to cancel the encoding.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeAudio(
		AudioEncoder encoder,
		CancellationToken token
	)
	{
		encoder.FileEncodingStarted += (_, filePath) => Console.WriteLine($"Encoding file: \"{filePath}\"");
		encoder.Error += async (_, message) => await Console.Error.WriteLineAsync($"Error: {message}");
		try
		{
			await encoder.EncodeAsync(token);
			return 0;
		}
		catch (ExternalException e)
		{
			await Console.Error.WriteLineAsync($"ffmpeg error(s): \"{e.Message}\"");
			if (File.Exists(encoder.OutPath)) { File.Delete(encoder.OutPath); }
			return 1;
		}
	}
}
