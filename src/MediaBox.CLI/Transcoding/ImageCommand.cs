using System.CommandLine;
using MediaBox.Core.Encoding;
using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Encoding.Image;

namespace MediaBox.CLI.Transcoding;

public class ImageCommand : TranscodeCommand
{
	private static readonly Option<ImageCodec> s_imageCodecOption = new("--image-codec", "--icodec", "-c:i", "-c")
	{
		Description = "Specify image codec.",
		DefaultValueFactory = _ => ImageCodec.JPEG
	};

	public readonly Command Command = new("image", "Transcode image files.")
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

			ImageEncoder encoder = new(path, destination, preset, imageCodec, force);
			return TranscodeImage(encoder);
		});
	}

	/// <summary>
	///     Transcodes image from one format to another.
	/// </summary>
	/// <param name="encoder"><see cref="ImageEncoder"/> configuration.</param>
	/// <returns>A Task object.</returns>
	private static async Task<int> TranscodeImage(ImageEncoder encoder)
	{
		encoder.FileEncodingStarted += (_, filePath) => Console.WriteLine($"Encoding file: \"{filePath}\"");
		encoder.Error += async (_, message) => await Console.Error.WriteLineAsync($"Error: {message}");
		await encoder.EncodeAsync();
		return 0;
	}
}
