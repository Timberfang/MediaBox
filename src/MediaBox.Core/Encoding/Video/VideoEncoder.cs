using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.Utility;

namespace MediaBox.Core.Encoding.Video;

/// <summary>
///     Encodes a video file from an input path to an output path using FFmpeg.
/// </summary>
public class VideoEncoder : IVideoEncoder
{
	// IO
	/// <summary>
	///     Files to be processed.
	/// </summary>
	private readonly IEnumerable<string> _files;

	/// <summary>
	///     File extensions that will be considered 'video' files.
	/// </summary>
	private readonly HashSet<string> _filter = new(StringComparer.OrdinalIgnoreCase)
	{
		".mkv",
		".webm",
		".mp4",
		".m4v",
		".m4a",
		".avi",
		".mov",
		".qt",
		".ogv"
	};

	// Constructor
	/// <summary>
	///     Encodes a video file from an input path to an output path using FFmpeg.
	/// </summary>
	/// <param name="inPath">The path to the input video file.</param>
	/// <param name="outPath">The path to save the encoded video file.</param>
	/// <param name="preset">The encoding preset to use: "Quality", "Normal", or "Fast".</param>
	public VideoEncoder(string inPath, string outPath, EncoderPreset preset = EncoderPreset.Normal)
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

	// Quality settings
	/// <summary>
	///     The encoder 'preset' used by FFmpeg.
	/// </summary>
	/// <remarks>
	///     The preset defines the tradeoff between encoding speed and the size of the output file.
	///     Higher preset values result in smaller files but take longer to encode.
	///     Lower preset values result in larger files but encode faster.
	///     Quality is not affected by the preset value.
	/// </remarks>
	private int VideoPreset =>
		Preset switch
		{
			EncoderPreset.Quality => 6,
			EncoderPreset.Normal => 10,
			_ => throw new ArgumentOutOfRangeException(nameof(Preset), Preset, "Invalid encoder preset.")
		};

	/// <summary>
	///     The CRF (Constant Rate Factor) value used by FFmpeg.
	/// </summary>
	/// <remarks>
	///     CRF is a quality setting that controls the level of compression applied to the video.
	///     Lower CRF values result in higher quality but larger files.
	///     Higher CRF values result in lower quality but smaller files.
	///     CRF is a logarithmic scale, so the difference in quality between two CRF values is not linear.
	/// </remarks>
	private int VideoQuality
	{
		get
		{
			if (VideoCodec is VideoCodec.AV1 or VideoCodec.VP9)
			{
				return Preset switch
				{
					EncoderPreset.Quality => 27,
					EncoderPreset.Normal => 33,
					_ => throw new ArgumentOutOfRangeException(nameof(Preset), Preset, "Invalid encoder preset.")
				};
			}

			return Preset switch
			{
				EncoderPreset.Quality => 22,
				EncoderPreset.Normal => 28,
				_ => throw new ArgumentOutOfRangeException(nameof(Preset), Preset, "Invalid encoder preset.")
			};
		}
	}

	/// <summary>
	///     The target bitrate for the audio stream.
	/// </summary>
	private int AudioBitrate =>
		Preset switch
		{
			EncoderPreset.Quality => 128000,
			EncoderPreset.Normal => 96000,
			_ => throw new ArgumentOutOfRangeException(nameof(Preset), Preset, "Invalid encoder preset.")
		};

	private readonly VideoContainer[] _supportedContainers = [
		VideoContainer.MKV,
		VideoContainer.MP4,
		VideoContainer.WEBM
	];

	/// <summary>
	///     The target container for the video file.
	/// </summary>
	/// <remarks>
	///     The .mp4 container supports a limited feature set, but has better compatibility than .mkv.
	///     The .webm container is a technical subset of the .mkv container; only AV1, VP8, and VP9 are supported for
	///     video, and Opus and Vorbis for audio. If additional codecs are required, consider the mkv container.
	/// </remarks>
	public VideoContainer Container
	{
		get;
		set
		{
			if (!_supportedContainers.Contains(value))
			{
				throw new ArgumentOutOfRangeException(nameof(Container), Container, "Unsupported video container.");
			}
			field = value;
		}
	} = VideoContainer.MKV;

	private string Extension => Container switch
	{
		VideoContainer.MKV => ".mkv",
		VideoContainer.MP4 => ".mp4",
		VideoContainer.WEBM => ".webm",
		_ => throw new ArgumentOutOfRangeException(nameof(Container), Container, "Unsupported video container.")
	};

	// Shared
	/// <inheritdoc />
	public string InPath { get; set; }

	/// <inheritdoc />
	public string OutPath { get; set; }

	/// <inheritdoc />
	public EncoderPreset Preset { get; set; }

	// Codecs
	/// <inheritdoc />
	public VideoCodec VideoCodec { get; set; } = VideoCodec.Copy;

	/// <inheritdoc />
	public AudioCodec AudioCodec { get; set; } = AudioCodec.Copy;

	/// <inheritdoc />
	public SubtitleCodec SubtitleCodec { get; set; } = SubtitleCodec.Copy;

	/// <inheritdoc />
	public bool Force { get; set; }

	// Encoding
	/// <inheritdoc />
	public event EventHandler<string>? FileEncodingStarted;

	/// <inheritdoc />
	public async Task EncodeAsync() => await EncodeAsync(false);

	/// <inheritdoc cref="EncodeAsync()" />
	public async Task EncodeAsync(bool crop, CancellationToken cancellationToken = default)
	{
		// Begin building arguments for ffmpeg
		// Don't set subtitle codec yet - needs a file-specific check
		List<string> argsMain;
		List<string> argsTwoPass;
		string nullPath = Environment.OSVersion.Platform switch
		{
			PlatformID.Win32NT => "NUL",
			PlatformID.Unix => "/dev/null",
			_ => throw new PlatformNotSupportedException($"Unspported platform: '{Environment.OSVersion.Platform}'.")
		};
		if (VideoCodec is VideoCodec.VP9)
		{
			// TODO: Move this to json, make it configurable
			argsMain =
			[
				"-c:v",
				FFmpeg.VideoCodecs[VideoCodec],
				"-c:a",
				FFmpeg.AudioCodecs[AudioCodec],
				"-c:s",
				FFmpeg.SubtitleCodecs[SubtitleCodec],
				"-pass",
				"2",
				"-quality",
				"good",
				"-lag-in-frames",
				"25",
				"-crf",
				VideoQuality.ToString(),
				"-b:v",
				"0",
				"-cpu-used",
				"4",
				"-auto-alt-ref",
				"1",
				"-arnr-maxframes",
				"7",
				"-arnr-strength",
				"4",
				"-aq-mode",
				"0",
				"-tile-rows",
				"0",
				"-tile-columns",
				"1",
				"-enable-tpl",
				"1",
				"-row-mt",
				"1"
			];
			argsTwoPass =
			[
				"-c:v",
				FFmpeg.VideoCodecs[VideoCodec],
				"-pass",
				"1",
				"-quality",
				"good",
				"-lag-in-frames",
				"25",
				"-crf",
				VideoQuality.ToString(),
				"-b:v",
				"0",
				"-cpu-used",
				"4",
				"-auto-alt-ref",
				"1",
				"-arnr-maxframes",
				"7",
				"-arnr-strength",
				"4",
				"-aq-mode",
				"0",
				"-tile-rows",
				"0",
				"-tile-columns",
				"6",
				"-enable-tpl",
				"1",
				"-row-mt",
				"1",
				"-an",
				"-f",
				"null"
			];
		}
		else
		{
			argsMain =
			[
				"-c:v",
				FFmpeg.VideoCodecs[VideoCodec],
				"-c:a",
				FFmpeg.AudioCodecs[AudioCodec],
				"-c:s",
				FFmpeg.SubtitleCodecs[SubtitleCodec],
				"-crf",
				VideoQuality.ToString(),
				"-preset",
				VideoPreset.ToString()
			];
			argsTwoPass = [];
		}


		foreach (string file in _files)
		{
			bool notify = true;

			// Set up paths
			string target = Path.ChangeExtension(GetTargetPath(file), Extension);
			if (Path.Exists(target))
			{
				continue;
			}

			if (!Force && Path.GetExtension(file).Equals(Extension, StringComparison.OrdinalIgnoreCase))
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
			Task<string> croppingConfigTask = FFmpeg.GetCroppingConfig(file, cancellationToken);

			// Fix subtitle codec if needed - .mp4 files use MOV_TEXT, which other formats don't support.
			if (Path.GetExtension(file).Equals(".mp4") && Container is not VideoContainer.MP4)
			{
				argsMain.AddRange("-c:s", FFmpeg.SubtitleCodecs[SubtitleCodec.SRT]);
			}
			else if (!Path.GetExtension(file).Equals(".mp4") && Container is VideoContainer.MP4)
			{
				argsMain.AddRange("-c:s", FFmpeg.SubtitleCodecs[SubtitleCodec.MOVTEXT]);
			}
			else
			{
				argsMain.AddRange("-c:s", FFmpeg.SubtitleCodecs[SubtitleCodec]);
			}

			// Handle VP9-specific two-pass encoding
			if (VideoCodec is VideoCodec.VP9)
			{
				if (notify)
				{
					FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
					notify = false;
				}

				await FFmpeg.RunAsync(file, nullPath, argsTwoPass, cancellationToken);
			}

			// Handle audio bitrate
			int targetAudioBitrate = await channelCountTask switch
			{
				>= 7 => Convert.ToInt32(AudioBitrate * 2.5),
				>= 5 => Convert.ToInt32(AudioBitrate) * 2,
				_ => AudioBitrate
			};
			argsMain.AddRange(["-b:a", targetAudioBitrate.ToString()]);

			// Handle cropping configuration
			if (crop)
			{
				string croppingConfig = await croppingConfigTask;
				if (!string.IsNullOrEmpty(croppingConfig))
				{
					argsMain.AddRange(["-vf", croppingConfig]);
				}
			}

			// Workaround for an opus/ffmpeg bug, see https://trac.ffmpeg.org/ticket/5718
			if (AudioCodec == AudioCodec.OPUS)
			{
				argsMain.AddRange(["-af", "aformat=channel_layouts=7.1|5.1|stereo"]);
			}

			// Encode
			if (notify)
			{
				FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
				// ReSharper disable once RedundantAssignment
				notify = false;
			}

			await FFmpeg.RunAsync(file, target, argsMain, cancellationToken);

			// Cleanup
			string ffmpegLogPath = Path.Join(Directory.GetCurrentDirectory(), "ffmpeg2pass-0.log");
			if (VideoCodec == VideoCodec.VP9 && File.Exists(ffmpegLogPath))
			{
				File.Delete(ffmpegLogPath);
			}
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
