using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Utility;

namespace MediaBox.Core.Encoding.Audio;

/// <summary>
///     Encodes an audio file from an input path to an output path using FFmpeg.
/// </summary>
public class AudioEncoder : IAudioEncoder
{
	// IO
	/// <summary>
	///     Files to be processed.
	/// </summary>
	private readonly IEnumerable<string> _files;

	/// <summary>
	///     File extensions that will be considered 'audio' files.
	/// </summary>
	private readonly HashSet<string> _filter = new(StringComparer.OrdinalIgnoreCase)
	{
		".mp3",
		".wav",
		".flac",
		".ogg",
		".opus",
		".m4a"
	};

	// Constructor
	/// <summary>
	///     Encodes an audio file from an input path to an output path using FFmpeg.
	/// </summary>
	/// <param name="inPath">The path to the input audio file.</param>
	/// <param name="outPath">The path to save the encoded audio file.</param>
	/// <param name="preset">The encoding preset to use: "Quality", "Normal", or "Fast".</param>
	public AudioEncoder(string inPath, string outPath, EncoderPreset preset = EncoderPreset.Normal)
	{
		InPath = inPath;
		OutPath = outPath;
		Preset = preset;

		if (Directory.Exists(InPath))
		{
			_files = Directory.EnumerateFiles(InPath, "*", SearchOption.AllDirectories)
				.Where(f => _filter.Contains(Path.GetExtension(f)));
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

	// Audio settings
	/// <summary>
	///     The target bitrate for the audio stream.
	/// </summary>
	private int AudioBitrate =>
		Preset switch
		{
			EncoderPreset.Quality => 128000,
			EncoderPreset.Normal => 96000,
			_ => throw new ArgumentOutOfRangeException(nameof(Preset))
		};

	/// <inheritdoc />
	public AudioCodec AudioCodec { get; set; } = AudioCodec.Copy;

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
	public async Task EncodeAsync() => await EncodeAsync(CancellationToken.None);

	/// <inheritdoc cref="EncodeAsync()" />
	public async Task EncodeAsync(CancellationToken cancellationToken)
	{
		// Begin building arguments for ffmpeg
		List<string> args =
		[
			"-c:a",
			FFmpeg.AudioCodecs[AudioCodec]
		];

		foreach (string file in _files)
		{
			// Set up paths
			string extension = AudioCodec switch
			{
				AudioCodec.Copy => Path.GetExtension(file),
				AudioCodec.MP3 => ".mp3",
				AudioCodec.AAC => ".aac",
				AudioCodec.OPUS => ".opus",
				_ => throw new ArgumentException(nameof(AudioCodec))
			};
			string target = Path.ChangeExtension(GetTargetPath(file), extension);
			if (Path.Exists(target))
			{
				continue;
			}

			if (!Force && Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			string? targetParent = Directory.GetParent(target)?.FullName;
			if (!string.IsNullOrWhiteSpace(targetParent) && !Directory.Exists(targetParent))
			{
				Directory.CreateDirectory(targetParent);
			}

			// Now that we know the file exists, begin expensive tasks
			Task<int> channelCountTask = FFmpeg.GetChannelCount(file, cancellationToken);
			int targetAudioBitrate = await channelCountTask switch
			{
				>= 7 => Convert.ToInt32(AudioBitrate * 2.5),
				>= 5 => Convert.ToInt32(AudioBitrate) * 2,
				_ => AudioBitrate
			};
			args.AddRange(["-b:a", targetAudioBitrate.ToString()]);

			// Workaround for an opus/ffmpeg bug, see https://trac.ffmpeg.org/ticket/5718
			if (AudioCodec == AudioCodec.OPUS)
			{
				args.AddRange(["-af", "aformat=channel_layouts=7.1|5.1|stereo"]);
			}

			// Encode
			FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
			await FFmpeg.RunAsync(file, target, args, cancellationToken);
		}
	}

	/// <summary>
	///     Replicates the directory structure of the input path in the output path.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private string GetTargetPath(string path) =>
		Path.GetExtension(OutPath).Length == 0
			? Path.Join(OutPath, path.Replace(InPath, string.Empty))
			: Path.ChangeExtension(OutPath, ".mkv");
}
