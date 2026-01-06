using System.Collections.Immutable;
using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Utility;
using NetVips;

namespace MediaBox.Core.Encoding.Image;

/// <summary>
/// 	Encode images using libvips.
/// </summary>
public class ImageEncoder : IImageEncoder
{
	// IO
	/// <summary>
	///     Files to be processed.
	/// </summary>
	private readonly ImmutableArray<string> _files;

	/// <summary>
	///     File extensions that will be considered 'image' files.
	/// </summary>
	private readonly HashSet<string> _filter = new(StringComparer.OrdinalIgnoreCase)
	{
		".jpg",
		".jpeg",
		".jfif",
		".png",
		".heif",
		".heic",
		".webp",
		".avif"
	};

	// Image settings
	/// <summary>
	///     The target quality setting for the image compression.
	/// </summary>
	private readonly Dictionary<EncoderPreset, int> _imageQuality =
		new() { { EncoderPreset.Quality, 95 }, { EncoderPreset.Normal, 85 } };

	// Constructor
	/// <summary>
	///     Encodes an image file from an input path to an output path using FFmpeg.
	/// </summary>
	/// <param name="inPath">The path to the input image file.</param>
	/// <param name="outPath">The path to save the encoded image file.</param>
	/// <param name="preset">The encoding preset to use: "Quality", "Normal", or "Fast".</param>
	public ImageEncoder(string inPath, string outPath, EncoderPreset preset = EncoderPreset.Normal)
	{
		InPath = inPath;
		OutPath = outPath;
		Preset = preset;

		if (Directory.Exists(InPath))
		{
			_files = ImmutableArray.Create([.. Directory.EnumerateFiles(InPath, "*", SearchOption.AllDirectories)
				.Where(f => _filter.Contains(Path.GetExtension(f)))]);;
		}
		else if (File.Exists(InPath))
		{
			_files = [InPath];
		}
		else
		{
			throw new FileNotFoundException(InPath);
		}
	}

	/// <inheritdoc />
	public ImageCodec ImageCodec { get; set; }

	// Shared
	/// <inheritdoc />
	public string InPath { get; set; }

	/// <inheritdoc />
	public string OutPath { get; set; }

	/// <inheritdoc />
	public EncoderPreset Preset { get; set; }

	/// <inheritdoc />
	public bool Force { get; set; }

	// Encoding
	/// <inheritdoc />
	public event EventHandler<string>? FileEncodingStarted;

	/// <inheritdoc />
	public async Task EncodeAsync()
	{
		// Build configuration
		VOption imageOptions = new() { { "Q", _imageQuality[Preset] } };

		// Encode
		// Task.Run(() => Parallel.ForEach(..) is faster than creating one task per item.
		// See https://stackoverflow.com/a/19103047
		await Task.Run(() => Parallel.ForEach(_files, file =>
		{
			// Set up paths
			string target = FileManager.GetTargetPath(file, InPath, OutPath);
			string extension = FileManager.GetExtension(ImageCodec);
			target = Path.ChangeExtension(target, extension);
			if (Path.Exists(target) || (!Force && Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}

			string? targetParent = Path.GetDirectoryName(target);
			if (!string.IsNullOrWhiteSpace(targetParent) && !Directory.Exists(targetParent))
			{
				Directory.CreateDirectory(targetParent);
			}

			// Encode
			FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
			using NetVips.Image image = NetVips.Image.NewFromFile(file);
			image.WriteToFile(target, imageOptions);
		}));
	}
}
