using NetVips;

namespace MediaBox.Encoding;

public class ImageEncoder(string inPath, string outPath, EncoderPreset preset = EncoderPreset.Normal) : IImageEncoder
{
	/// <summary>
	///     A hash set of file extensions that will be considered 'image' files.
	/// </summary>
	private readonly HashSet<string> _filter = [".jpg", ".jpeg", ".jfif", ".png", ".heif", ".heic", ".avif"];
	/// <summary>
	///     The target quality setting for the image compression.
	/// </summary>
	private readonly Dictionary<EncoderPreset, int> _imageQuality =
		new() { { EncoderPreset.Quality, 95 }, { EncoderPreset.Normal, 85 } };

	/// <inheritdoc />
	public string InPath { get; set; } = inPath;

	/// <inheritdoc />
	public string OutPath { get; set; } = outPath;

	/// <inheritdoc />
	public EncoderPreset Preset { get; set; } = preset;

	/// <inheritdoc />
	public int ImageQuality => _imageQuality[Preset];

	/// <inheritdoc />
	public event EventHandler<string>? FileEncodingStarted;

	/// <inheritdoc />
	public async Task EncodeAsync()
	{
		// Get files to process
		IEnumerable<string> files;
		if (Directory.Exists(InPath))
		{
			files = Directory.EnumerateFiles(InPath, "*", SearchOption.AllDirectories)
				.Where(f => _filter.Contains(Path.GetExtension(f)));
		}
		else if (File.Exists(InPath)) { files = [InPath]; }
		else { throw new FileNotFoundException(InPath); }

		// Prepare configuration
		VOption imageOptions = new() { { "Q", ImageQuality } };

		// Encode
		// Task.Run(() => Parallel.ForEach(..) is faster than creating one task per item.
		// See https://stackoverflow.com/a/19103047
		await Task.Run(() => Parallel.ForEach(files, file =>
		{
			// Prepare input/output paths
			string target = Path.ChangeExtension(GetTargetPath(file), ".webp");
			if (Path.Exists(target)) { return; }
			string? targetParent = Path.GetDirectoryName(target);
			if (targetParent != null && !Directory.Exists(targetParent)) { Directory.CreateDirectory(targetParent); }

			// Encode files
			FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
			using Image image = Image.NewFromFile(file);
			image.WriteToFile(target, imageOptions);
		}));
	}

	/// <summary>
	///     Replicates the directory structure of the input path in the output path.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private string GetTargetPath(string path) => Path.GetExtension(OutPath).Length == 0
		? Path.Join(OutPath, path.Replace(InPath, string.Empty))
		: OutPath;
}
