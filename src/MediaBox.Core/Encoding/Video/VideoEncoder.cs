using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.External;

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
			_ => throw new ArgumentOutOfRangeException()
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
					_ => throw new ArgumentOutOfRangeException(nameof(Preset))
				};
			}

			return Preset switch
			{
				EncoderPreset.Quality => 22,
				EncoderPreset.Normal => 28,
				_ => throw new ArgumentOutOfRangeException(nameof(Preset))
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
			_ => throw new ArgumentOutOfRangeException(nameof(Preset))
		};

	/// <summary>
	///     The target container for the video file.
	/// </summary>
	/// <remarks>
	///     The .mp4 container supports a limited feature set, but has better compatibility than .mkv.
	///     The .webm container is a technical subset of the .mkv container; mediabox assumes that it is being chosen
	///     for compatibility reasons, and will use VP9 video with OPUS audio to maintain high compatibility. If high
	///     compatibility is not required, consider the .mkv container.
	/// </remarks>
	public VideoContainer VideoContainer { get; set; } = VideoContainer.MKV;

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

	// Encoding
	/// <inheritdoc />
	public event EventHandler<string>? FileEncodingStarted;

	/// <inheritdoc />
	public async Task EncodeAsync() => await EncodeAsync(false);

	/// <inheritdoc cref="EncodeAsync()" />
	public async Task EncodeAsync(bool crop, CancellationToken cancellationToken = default)
	{
		// Configure video container
		string extension = VideoContainer switch
		{
			VideoContainer.MKV => ".mkv",
			VideoContainer.MP4 => ".mp4",
			VideoContainer.WEBM => ".webm",
			_ => throw new ArgumentException(nameof(VideoContainer))
		};

		// Use standard/compatible .webm files
		// If compatibility is not important, just use .mkv instead.
		if (VideoContainer is VideoContainer.WEBM)
		{
			VideoCodec = VideoCodec.VP9;
			AudioCodec = AudioCodec.OPUS;
		}

		// Begin building arguments for ffmpeg
		// Don't set subtitle codec yet - needs a file-specific check
		List<string> args =
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

		foreach (string file in _files)
		{
			// Set up paths
			string target = Path.ChangeExtension(GetTargetPath(file), extension);
			if (Path.Exists(target))
			{
				continue;
			}

			string? targetParent = Directory.GetParent(target)?.FullName;
			if (targetParent != null && !Directory.Exists(targetParent))
			{
				Directory.CreateDirectory(targetParent);
			}

			// Now that we know the file exists, begin expensive tasks
			Task<int> channelCountTask = FFmpeg.GetChannelCount(file);
			Task<string> croppingConfigTask = FFmpeg.GetCroppingConfig(file);

			// Fix subtitle codec if needed - .mp4 files use MOV_TEXT, which other formats don't support.
			if (Path.GetExtension(file).Equals(".mp4") && VideoContainer is not VideoContainer.MP4)
			{
				args.AddRange("-c:s", FFmpeg.SubtitleCodecs[SubtitleCodec.SRT]);
			}
			else if (!Path.GetExtension(file).Equals(".mp4") && VideoContainer is VideoContainer.MP4)
			{
				args.AddRange("-c:s", FFmpeg.SubtitleCodecs[SubtitleCodec.MOVTEXT]);
			}
			else
			{
				args.AddRange("-c:s", FFmpeg.SubtitleCodecs[SubtitleCodec]);
			}

			// Handle VP9-specific instructions
			if (VideoCodec is VideoCodec.VP9)
			{
				args.AddRange("-b:v", "0", "-pass", "2", "-row-mt", "1");

				// Handle first pass of VP9 two-pass encoding, see https://trac.ffmpeg.org/wiki/Encode/VP9#twopass
				string nullPath = Environment.OSVersion.Platform switch
				{
					PlatformID.Win32NT => "NUL",
					PlatformID.Unix => "/dev/null",
					_ => throw new PlatformNotSupportedException(Environment.OSVersion.Platform.ToString())
				};
				List<string> argsFirstPass =
				[
					"-c:v",
					"libvpx-vp9",
					"-b:v",
					"0",
					"-crf",
					VideoQuality.ToString(),
					"-pass",
					"1",
					"-an",
					"-f",
					"null",
					"-row-mt", // Multi-threading optimization, see https://trac.ffmpeg.org/wiki/Encode/VP9#rowmt
					"1"
				];
				await FFmpeg.RunAsync(new FFmpegConfig(file, nullPath, argsFirstPass, cancellationToken));
			}

			// Handle audio bitrate
			int targetAudioBitrate = await channelCountTask switch
			{
				>= 7 => Convert.ToInt32(AudioBitrate * 2.5),
				>= 5 => Convert.ToInt32(AudioBitrate) * 2,
				_ => AudioBitrate
			};
			args.AddRange(["-b:a", targetAudioBitrate.ToString()]);

			// Handle cropping configuration
			if (crop)
			{
				string croppingConfig = await croppingConfigTask;
				if (!string.IsNullOrEmpty(croppingConfig))
				{
					args.AddRange(["-vf", croppingConfig]);
				}
			}

			// Workaround for an opus/ffmpeg bug, see https://trac.ffmpeg.org/ticket/5718
			if (AudioCodec == AudioCodec.OPUS)
			{
				args.AddRange(["-af", "aformat=channel_layouts=7.1|5.1|stereo"]);
			}

			// Encode
			FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
			await FFmpeg.RunAsync(new FFmpegConfig(file, target, args, cancellationToken));

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
