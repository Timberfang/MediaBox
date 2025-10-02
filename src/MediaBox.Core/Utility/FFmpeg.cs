using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Utility;

public static partial class FFmpeg
{
	/// <summary>
	///     Convert audio codec settings into FFmpeg values.
	/// </summary>
	internal static readonly Dictionary<AudioCodec, string> AudioCodecs = new()
	{
		{ AudioCodec.Copy, "copy" },
		{ AudioCodec.MP3, "libmp3lame" },
		{ AudioCodec.AAC, "aac" },
		{ AudioCodec.OPUS, "libopus" }
	};

	/// <summary>
	///     Convert video codec settings into FFmpeg values.
	/// </summary>
	internal static readonly Dictionary<VideoCodec, string> VideoCodecs = new()
	{
		{ VideoCodec.Copy, "copy" },
		{ VideoCodec.AVC, "libx264" },
		{ VideoCodec.HEVC, "libx265" },
		{ VideoCodec.AV1, "libsvtav1" },
		{ VideoCodec.VP9, "libvpx-vp9" }
	};

	/// <summary>
	///     Convert subtitle codec settings into FFmpeg values.
	/// </summary>
	internal static readonly Dictionary<SubtitleCodec, string> SubtitleCodecs = new()
	{
		{ SubtitleCodec.Copy, "copy" },
		{ SubtitleCodec.SRT, "subrip" },
		{ SubtitleCodec.SSA, "ass" },
		{ SubtitleCodec.MOVTEXT, "MOV_TEXT" }
	};

	/// <summary>
	///     Runs FFmpeg with the given configuration.
	/// </summary>
	/// <param name="inPath">The path to the input file.</param>
	/// <param name="outPath">The path where the output file will be written.</param>
	/// <param name="arguments">Arguments to be passed to FFmpeg.</param>
	/// <param name="cts">Cancellation token to cancel ffmpeg operations.</param>
	internal static async Task RunAsync(string inPath, string outPath, IEnumerable<string> arguments,
		CancellationToken cts = default)
	{
		string ffmpeg = ProcessManager.GetPath("ffmpeg");

		// Prepare input/output paths
		string? directory = Directory.GetParent(inPath)?.FullName;
		if (Path.Exists(outPath))
		{
			return;
		}

		if (!Directory.Exists(directory) && directory != null)
		{
			Directory.CreateDirectory(directory);
		}

		// Encode
		List<string> args =
		[
			"-loglevel",
			"error",
			"-nostdin",
			"-i",
			inPath
		];
		args.AddRange(arguments);
		args.Add(outPath);
		try
		{
			// Silences SVT_AV1's output
			Dictionary<string, string> svtSilencer = new() { { "SVT_LOG", "0" } };
			await ProcessManager.StartAsync(ffmpeg, args, environmentVariables: svtSilencer, ct: cts);
		}
		catch (ExternalException)
		{
			if (File.Exists(outPath))
			{
				File.Delete(outPath);
			}

			throw;
		}
	}

	/// <summary>
	///     Runs ffmpeg with the given arguments, producing no output.
	/// </summary>
	/// <param name="path">The path to be analyzed.</param>
	/// <param name="preArguments">Arguments to be placed before the path.</param>
	/// <param name="postArguments">Arguments to be placed after the path.</param>
	/// <param name="cts">Cancellation token to cancel ffmpeg operations.</param>
	/// <returns>FFmpeg's output.</returns>
	/// <exception cref="FileNotFoundException">Thrown if the given path does not exist, or is not a file.</exception>
	private static async Task<string> AnalyzeAsync(string path, string[] preArguments, string[] postArguments,
		CancellationToken cts = default)
	{
		string ffmpeg = ProcessManager.GetPath("ffmpeg");
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"File at {path} does not exist");
		}

		List<string> args =
		[
			"-loglevel",
			"error",
			"-nostdin"
		];
		args.AddRange(preArguments);
		args.AddRange(["-i", path]);
		args.AddRange(postArguments);
		args.AddRange(["-f", "null", "-"]);
		return await ProcessManager.StartAsync(ffmpeg, args, ct: cts);
	}

	/// <summary>
	///     Runs FFprobe with the given arguments.
	/// </summary>
	/// <param name="path">The path to be probed.</param>
	/// <param name="arguments">The arguments to be passed to FFprobe.</param>
	/// <param name="cts">Cancellation token to cancel ffmpeg operations.</param>
	/// <returns>FFprobe's output.</returns>
	/// <exception cref="FileNotFoundException">Thrown if the given path does not exist, or is not a file.</exception>
	private static async Task<string> ProbeAsync(string path, string[] arguments, CancellationToken cts = default)
	{
		string ffprobe = ProcessManager.GetPath("ffprobe");
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"File at {path} does not exist");
		}

		List<string> args =
		[
			"-loglevel",
			"error"
		];
		args.AddRange(arguments);
		args.AddRange(["-i", path]);
		return await ProcessManager.StartAsync(ffprobe, args, ct: cts);
	}

	/// <summary>
	///     Detects the duration of a video file.
	/// </summary>
	/// <param name="path">The path of the file to be processed.</param>
	/// <param name="cts">Cancellation token to cancel ffmpeg operations.</param>
	/// <returns>The duration of the video in seconds.</returns>
	/// <exception cref="InvalidDataException">Thrown when FFprobe returns an invalid duration.</exception>
	public static async Task<int> GetDuration(string path, CancellationToken cts = default)
	{
		string[] args =
		[
			"-select_streams",
			"v:0",
			"-show_entries",
			"format=duration",
			"-of",
			"compact=p=0:nk=1"
		];
		string ffprobeOutput = await ProbeAsync(path, args, cts);
		return float.TryParse(ffprobeOutput, out float durationFloat)
			? (int)durationFloat
			: throw new InvalidDataException(
				$"{ffprobeOutput} (from file '{Path.GetFileNameWithoutExtension(path)}') is not a float");
	}

	/// <summary>
	///     Detects the number of audio channels in a video file.
	/// </summary>
	/// <param name="path">The path of the file to be processed.</param>
	/// <param name="cts">Cancellation token to cancel ffmpeg operations.</param>
	/// <returns>The number of audio channels.</returns>
	public static async Task<int> GetChannelCount(string path, CancellationToken cts = default)
	{
		string[] args =
		[
			"-select_streams",
			"a:0",
			"-show_entries",
			"stream=channels",
			"-of",
			"compact=p=0:nk=1"
		];
		string ffprobeOutput = await ProbeAsync(path, args, cts);
		return int.TryParse(ffprobeOutput, out int channels) ? channels : 0;
	}

	/// <summary>
	///     Detects black borders that must be cropped by the encoder.
	/// </summary>
	/// <remarks>
	///     This method takes several shortcuts to try to speed up execution:
	///     - Only keyframes are analysed.
	///     - Accurate seeking is disabled.
	///     - Non-video streams are ignored.
	///     - Only the first 20 keyframes past the start-time are checked.
	///     - If the video is at least 10 minutes (600 seconds) long, it starts at 5 minutes (300 seconds).
	///     - Otherwise, it starts at the beginning (0 seconds).
	/// </remarks>
	/// <param name="path">The path of the file to be processed.</param>
	/// <param name="cts">Cancellation token to cancel ffmpeg operations.</param>
	/// <returns>The crop information, in FFmpeg's format.</returns>
	public static async Task<string> GetCroppingConfig(string path, CancellationToken cts = default)
	{
		int startTime = await GetDuration(path, cts) < 600 ? 0 : 300;
		string[] argsBefore =
		[
			"-skip_frame",
			"nokey",
			"-noaccurate_seek",
			"-ss",
			startTime.ToString()
		];
		string[] argsAfter =
		[
			"-frames:v",
			"20",
			"-vf",
			"cropdetect",
			"-an"
		];
		string ffmpegOutput = await AnalyzeAsync(path, argsBefore, argsAfter, cts);
		return CroppingRegex().Match(ffmpegOutput).Groups[0].Value;
	}

	/// <summary>
	///     The regular expression filter used to parse ffmpeg's crop-detection filter.
	/// </summary>
	/// <returns>A compiled regular expression pattern.</returns>
	[GeneratedRegex("crop=.*", RegexOptions.RightToLeft)]
	private static partial Regex CroppingRegex();
}
