using System.Text;

using MediaBox.ExternalProcess;

namespace MediaBox.Encoding;

/// <summary>
///     Encodes an audio file from an input path to an output path using FFmpeg.
/// </summary>
/// <param name="inPath">The path to the input audio file.</param>
/// <param name="outPath">The path to save the encoded audio file.</param>
/// <param name="preset">The encoding preset to use: "Quality", "Normal", or "Fast".</param>
public class AudioEncoder(string inPath, string outPath, EncoderPreset preset = EncoderPreset.Normal) : IAudioEncoder
{
	/// <summary>
	///     A hash set of file extensions that will be considered 'audio' files.
	/// </summary>
	private readonly HashSet<string> _filter = [".mp3", ".wav", ".flac", ".ogg", ".opus"];
	
	/// <summary>
	///     The target bitrate for the audio stream.
	/// </summary>
	private readonly Dictionary<EncoderPreset, int> _audioBitrate =
		new() { { EncoderPreset.Quality, 128000 }, { EncoderPreset.Normal, 96000 } };
	
	/// <summary>
	///     Convert audio codecs from the AudioCodec enum into FFmpeg values.
	/// </summary>
	private readonly Dictionary<AudioCodec, string> _audioCodec = new()
	{
		{ AudioCodec.Copy, "copy" },
		{ AudioCodec.MP3, "mp3" },
		{ AudioCodec.AAC, "aac" },
		{ AudioCodec.OPUS, "opus" }
	};

	/// <summary>
	///     The bitrate FFmpeg is targeting for the audio stream.
	/// </summary>
	public int AudioBitrate => _audioBitrate[Preset];
	
	/// <inheritdoc />
	public AudioCodec AudioCodec { get; set; }

	/// <inheritdoc />
	public string InPath { get; set; } = inPath;

	/// <inheritdoc />
	public string OutPath { get; set; } = outPath;

	/// <inheritdoc />
	public EncoderPreset Preset { get; set; } = preset;

	/// <inheritdoc />
	public event EventHandler<string>? FileEncodingStarted;

	/// <inheritdoc />
	/// <exception cref="FileNotFoundException">Thrown when the input path does not exist.</exception>
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

		foreach (string file in files)
		{
			// Prepare input/output paths
			string target = Path.ChangeExtension(GetTargetPath(file), GetNewExtension(file));
			string? targetParent = Directory.GetParent(target)?.FullName;
			if (Path.Exists(target)) { continue; }
			if (!Directory.Exists(targetParent) && targetParent != null) { Directory.CreateDirectory(targetParent); }

			// Encode
			FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
			string args = await GetArgs(file);
			FFmpegConfig config = new(file, target, args);
			await FFmpeg.RunAsync(config);
		}
	}

	/// <summary>
	///     Builds an array of arguments to pass to FFmpeg.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private async Task<string> GetArgs(string path)
	{
		// Start most expensive operations in background tasks
		Task<int> channelCountTask = FFmpeg.GetChannelCount(path);

		// Build basic arguments
		// Workaround for a bug with opus in ffmpeg, see https://trac.ffmpeg.org/ticket/5718
		StringBuilder args = new($"-c:a {_audioCodec[AudioCodec]} -af aformat=channel_layouts=7.1|5.1|stereo");

		// Handle audio bitrate
		int targetAudioBitrate = await channelCountTask switch
		{
			>= 7 => Convert.ToInt32(AudioBitrate * 2.5),
			>= 5 => Convert.ToInt32(AudioBitrate) * 2,
			_ => AudioBitrate
		};
		args.Append($" -b:a {targetAudioBitrate}");

		// Return output
		return args.ToString();
	}

	/// <summary>
	///     Replicates the directory structure of the input path in the output path.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private string GetTargetPath(string path)
	{
		return Path.GetExtension(OutPath).Length == 0
			? Path.Join(OutPath, path.Replace(InPath, string.Empty))
			: Path.ChangeExtension(OutPath, ".opus");
	}

	private string GetNewExtension(string path)
	{
		return AudioCodec switch
		{
			AudioCodec.Copy => Path.GetExtension(path),
			AudioCodec.MP3 => ".mp3",
			AudioCodec.AAC => ".aac",
			AudioCodec.OPUS => ".opus",
			_ => throw new ArgumentOutOfRangeException(path)
		};
	}
}
