using System.CommandLine;
using MediaBox.Core.Encoding;
using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Encoding.Image;

namespace MediaBox.CLI.Transcoding;

public class ImageCommand : TranscodeCommand
{
	private static readonly Option<ImageCodec> s_imageCodecOption = new("--image-codec", "--icodec", "-c:i")
	{
		Description = "The codec to use for images",
		DefaultValueFactory = _ => ImageCodec.JPEG
	};

	public readonly Command Command = new("image", "transcode images to a different format")
	{
		SharedOptions.s_pathArgument,
		SharedOptions.s_destinationArgument,
		s_presetOption,
		s_imageCodecOption,
		SharedOptions.s_forceOption
	};

	public ImageCommand()
	{
		Command.SetAction(parseResult =>
		{
			string? path = parseResult.GetValue(SharedOptions.s_pathArgument);
			string? destination = parseResult.GetValue(SharedOptions.s_destinationArgument);
			EncoderPreset preset = parseResult.GetValue(s_presetOption);
			ImageCodec imageCodec = parseResult.GetValue(s_imageCodecOption);
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

			return TranscodeImage(path, destination, preset, imageCodec, force);
		});
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
