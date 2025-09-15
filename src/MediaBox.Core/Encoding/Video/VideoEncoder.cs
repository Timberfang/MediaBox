using MediaBox.Core.Encoding.Codecs;
using MediaBox.Core.External;

namespace MediaBox.Core.Encoding.Video;

/// <summary>
///     Encodes a video file from an input path to an output path using FFmpeg.
/// </summary>
public class VideoEncoder : IVideoEncoder
{
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
		{ AudioCodec.MP3, "libmp3lame" },
		{ AudioCodec.AAC, "aac" },
		{ AudioCodec.OPUS, "libopus" }
	};

	/// <summary>
	///     An IEnumerable containing all the files to be processed.
	/// </summary>
	private readonly IEnumerable<string> _files;

	/// <summary>
	///     A hash set of file extensions that will be considered 'video' files.
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

	/// <summary>
	///     Convert subtitle codecs from the SubtitleCodec enum into FFmpeg values.
	/// </summary>
	private readonly Dictionary<SubtitleCodec, string> _subtitleCodec = new()
	{
		{ SubtitleCodec.Copy, "copy" }, { SubtitleCodec.SRT, "subrip" }, { SubtitleCodec.SSA, "ass" }
	};

	/// <summary>
	///     Convert video codecs from the VideoCodec enum into FFmpeg values.
	/// </summary>
	private readonly Dictionary<VideoCodec, string> _videoCodec = new()
	{
		{ VideoCodec.Copy, "copy" },
		{ VideoCodec.AVC, "libx264" },
		{ VideoCodec.HEVC, "libx265" },
		{ VideoCodec.AV1, "libsvtav1" },
		{ VideoCodec.VP9, "libvpx-vp9" }
	};

	/// <summary>
	///     The encoder 'preset' to use. It must be an integer between 0 and 13 (inclusive).
	/// </summary>
	/// <remarks>
	///     The preset defines the tradeoff between encoding speed and the size of the output file.
	///     Higher preset values result in smaller files but take longer to encode.
	///     Lower preset values result in larger files but encode faster.
	///     Quality is not affected by the preset value.
	/// </remarks>
	private readonly Dictionary<EncoderPreset, int> _videoPreset =
		new() { { EncoderPreset.Quality, 6 }, { EncoderPreset.Normal, 10 } };

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

	/// <inheritdoc />
	public VideoCodec VideoCodec { get; set; } = VideoCodec.AV1;

	/// <inheritdoc />
	public AudioCodec AudioCodec { get; set; } = AudioCodec.OPUS;

	/// <inheritdoc />
	public SubtitleCodec SubtitleCodec { get; set; } = SubtitleCodec.Copy;

	/// <inheritdoc />
	public int VideoPreset => _videoPreset[Preset];

	/// <inheritdoc />
	public int VideoQuality => GetVideoQuality();

	/// <inheritdoc />
	public int AudioBitrate => _audioBitrate[Preset];

	/// <inheritdoc />
	public string InPath { get; set; }

	/// <inheritdoc />
	public string OutPath { get; set; }

	/// <inheritdoc />
	public EncoderPreset Preset { get; set; }

	/// <inheritdoc />
	public event EventHandler<string>? FileEncodingStarted;

	/// <inheritdoc />
	/// <exception cref="FileNotFoundException">Thrown when the input path does not exist.</exception>
	public Task EncodeAsync() => EncodeAsync(true);

	/// <inheritdoc cref="EncodeAsync()" />
	/// <param name="crop">Whether to attempt to crop the video file.</param>
	/// <param name="cancellationToken">Token to cancel the encoding.</param>
	public async Task EncodeAsync(bool crop, CancellationToken cancellationToken = default)
	{
		foreach (string file in _files)
		{
			// Prepare input/output paths
			// TODO: Make container configurable
			string target = Path.ChangeExtension(GetTargetPath(file), VideoCodec == VideoCodec.VP9 ? ".webm" : ".mkv");

			string? targetParent = Directory.GetParent(target)?.FullName;
			if (Path.Exists(target))
			{
				continue;
			}

			if (!Directory.Exists(targetParent) && targetParent != null)
			{
				Directory.CreateDirectory(targetParent);
			}

			// Fix subtitle codec if needed - .mp4 files use MOV_TEXT, which other formats don't support.
			if (SubtitleCodec == SubtitleCodec.Copy && Path.GetExtension(file).Equals(".mp4") &&
				!Path.GetExtension(target).Equals(".mp4"))
			{
				SubtitleCodec = SubtitleCodec.SRT;
			}

			// Encode
			string[] args;
			FFmpegConfig config;
			FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
			if (VideoCodec != VideoCodec.VP9)
			{
				args = await GetArgs(file, crop);
			}
			else
			{
				// VP9 settings sourced from:
				// - https://wiki.webmproject.org/ffmpeg/vp9-encoding-guide
				// - https://www.reddit.com/r/AV1/comments/k7colv/encoder_tuning_part_1_tuning_libvpxvp9_be_more/
				string nullPath = Environment.OSVersion.Platform switch
				{
					PlatformID.Win32NT => "NUL",
					PlatformID.Unix => "/dev/null",
					_ => throw new PlatformNotSupportedException(Environment.OSVersion.Platform.ToString())
				};
				args = await GetArgsVp9(file, crop);
				config = new FFmpegConfig(file, nullPath, args, cancellationToken);
				await FFmpeg.RunAsync(config);
				args = await GetArgsVp9(file, crop, true);
			}

			config = new FFmpegConfig(file, target, args, cancellationToken);
			await FFmpeg.RunAsync(config);

			// Cleanup
			string ffmpegLogPath = Path.Join(Directory.GetCurrentDirectory(), "ffmpeg2pass-0.log");
			if (VideoCodec == VideoCodec.VP9 && File.Exists(ffmpegLogPath))
			{
				File.Delete(ffmpegLogPath);
			}
		}
	}

	public async Task SilenceVideoAsync(CancellationToken cancellationToken = default)
	{
		foreach (string file in _files)
		{
			// TODO: Make output name configurable
			string baseName = Path.GetFileNameWithoutExtension(file);
			string extension = Path.GetExtension(file);
			string target = $"{baseName}.silent.{extension}";
			if (File.Exists(target))
			{
				continue;
			}

			FFmpegConfig config = new(file, target, ["-c", "copy", "-an"], cancellationToken);
			await FFmpeg.RunAsync(config);
		}
	}

	public async Task TrimVideoAsync(string startTime = "", string endTime = "",
		CancellationToken cancellationToken = default)
	{
		foreach (string file in _files)
		{
			// TODO: Make output name configurable
			string baseName = Path.GetFileNameWithoutExtension(file);
			string extension = Path.GetExtension(file);
			string target = $"{baseName}.trimmed.{extension}";
			if (File.Exists(target))
			{
				continue;
			}

			bool startTimeConfigured = startTime.Length > 0;
			bool endTimeConfigured = endTime.Length > 0;

			// Prepare arguments
			List<string> args = ["-c", "copy"];
			if (!startTimeConfigured && !endTimeConfigured)
			{
				continue;
			}

			if (startTimeConfigured)
			{
				args.AddRange(["-ss", startTime]);
			}

			if (startTimeConfigured)
			{
				args.AddRange(["-to", endTime]);
			}

			// Trim
			FFmpegConfig config = new(file, target, args, cancellationToken);
			await FFmpeg.RunAsync(config);
		}
	}

	/// <summary>
	///     Builds an array of arguments to pass to FFmpeg.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <param name="crop">Whether to attempt to crop the video file.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private async Task<string[]> GetArgs(string path, bool crop = true)
	{
		// Start most expensive operations in background tasks
		Task<int> channelCountTask = FFmpeg.GetChannelCount(path);
		Task<string> croppingConfigTask = FFmpeg.GetCroppingConfig(path);

		// Build basic arguments
		// The aformat is a workaround for an opus/ffmpeg bug, see https://trac.ffmpeg.org/ticket/5718
		List<string> args =
		[
			"-c:v",
			_videoCodec[VideoCodec],
			"-crf",
			VideoQuality.ToString(),
			"-preset",
			VideoPreset.ToString(),
			"-c:a",
			_audioCodec[AudioCodec],
			"-c:s",
			_subtitleCodec[SubtitleCodec]
		];

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

		// Return output
		return args.ToArray();
	}

	/// <summary>
	///     Builds an array of arguments to pass to FFmpeg, using the two-pass method for VP9.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <param name="crop">Whether to attempt to crop the video file.</param>
	/// <param name="secondPass">Whether to return the arguments for the first or second pass.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private async Task<string[]> GetArgsVp9(string path, bool crop = true, bool secondPass = false)
	{
		// Start most expensive operations in background tasks
		Task<string> croppingConfigTask = FFmpeg.GetCroppingConfig(path);

		// Build basic arguments
		// VP9 encoding uses two passes, see https://trac.ffmpeg.org/wiki/Encode/VP9#twopass
		// The aformat is a workaround for an opus/ffmpeg bug, see https://trac.ffmpeg.org/ticket/5718
		// -row-mt is a multi-threading optimization, see https://trac.ffmpeg.org/wiki/Encode/VP9#rowmt
		List<string> args;
		if (!secondPass)
		{
			args =
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
				"-row-mt",
				"1"
			];
		}
		else
		{
			Task<int> channelCountTask = FFmpeg.GetChannelCount(path);
			args =
			[
				"-c:v",
				"libvpx-vp9",
				"-b:v",
				"0",
				"-crf",
				VideoQuality.ToString(),
				"-preset",
				VideoPreset.ToString(),
				"-pass",
				"2",
				"-c:a",
				"libopus",
				"-row-mt",
				"1"
			];

			// Handle audio bitrate
			// The '-af' is a workaround for an opus/ffmpeg bug, see https://trac.ffmpeg.org/ticket/5718
			int targetAudioBitrate = await channelCountTask switch
			{
				>= 7 => Convert.ToInt32(AudioBitrate * 2.5),
				>= 5 => Convert.ToInt32(AudioBitrate) * 2,
				_ => AudioBitrate
			};
			args.AddRange(["-b:a", targetAudioBitrate.ToString(), "-af", "aformat=channel_layouts=7.1|5.1|stereo"]);
		}

		// Handle cropping configuration
		if (crop)
		{
			string croppingConfig = await croppingConfigTask;
			if (!string.IsNullOrEmpty(croppingConfig))
			{
				args.AddRange(["-vf", croppingConfig]);
			}
		}

		return args.ToArray();
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

	/// <summary>
	///     The CRF (Constant Rate Factor) value to use. It must be an integer between 0 and the maximum CRF value of
	///     the codec (usually 51).
	/// </summary>
	/// <remarks>
	///     CRF is a quality setting that controls the level of compression applied to the video.
	///     Lower CRF values result in higher quality but larger files.
	///     Higher CRF values result in lower quality but smaller files.
	///     CRF is a logarithmic scale, so the difference in quality between two CRF values is not linear.
	///     The H.264/H.265 codecs allow CRF values from 0 to 51.
	///     The AV1 codec allows CRF values from 0 to 63.
	///     The VP9 codec allows CRF values from 4 to 63.
	/// </remarks>
	private int GetVideoQuality()
	{
		// See https://developers.google.com/media/vp9/settings/vod#quality VP9 CRF recommendations
		bool av1Quality = VideoCodec is VideoCodec.AV1 or VideoCodec.VP9;
		return Preset switch
		{
			EncoderPreset.Quality => av1Quality ? 27 : 22,
			EncoderPreset.Normal => av1Quality ? 33 : 28,
			_ => throw new ArgumentOutOfRangeException(nameof(Preset))
		};
	}
}
