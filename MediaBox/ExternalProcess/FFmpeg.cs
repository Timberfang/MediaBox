using System.Text.RegularExpressions;

using CliWrap;
using CliWrap.Buffered;

namespace MediaBox.ExternalProcess;

public static partial class FFmpeg
{
	/// <summary>
	///     Runs FFmpeg with the given configuration.
	/// </summary>
	/// <param name="config">A configuration object for FFmpeg.</param>
	public static async Task RunAsync(FFmpegConfig config)
	{
		// Prepare input/output paths
		string? directory = Directory.GetParent(config.InPath)?.FullName;
		if (Path.Exists(config.OutPath)) { return; }

		if (!Directory.Exists(directory) && directory != null) { Directory.CreateDirectory(directory); }

		// Encode
		List<string> args =
		[
			"-loglevel",
			"error",
			"-nostdin",
			"-i",
			config.InPath
		];
		args.AddRange(config.Arguments);
		args.Add(config.OutPath);
		await Cli.Wrap("ffmpeg")
			.WithArguments(args)
			.ExecuteAsync(config.CancellationToken);
	}

	/// <summary>
	///     Runs ffmpeg with the given arguments, producing no output.
	/// </summary>
	/// <param name="path">The path to be analyzed.</param>
	/// <param name="preArguments">Arguments to be placed before the path.</param>
	/// <param name="postArguments">Arguments to be placed after the path.</param>
	/// <returns>FFmpeg's output.</returns>
	/// <exception cref="FileNotFoundException">Thrown if the given path does not exist, or is not a file.</exception>
	public static async Task<string> AnalyzeAsync(string path, string[] preArguments, string[] postArguments)
	{
		if (!File.Exists(path)) { throw new FileNotFoundException($"File at {path} does not exist"); }

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
		BufferedCommandResult ffmpegOutput = await Cli.Wrap("ffmpeg")
			.WithArguments(args)
			.ExecuteBufferedAsync();
		return ffmpegOutput.StandardOutput;
	}

	/// <summary>
	///     Runs FFprobe with the given arguments.
	/// </summary>
	/// <param name="path">The path to be probed.</param>
	/// <param name="arguments">The arguments to be passed to FFprobe.</param>
	/// <returns>FFprobe's output.</returns>
	/// <exception cref="FileNotFoundException">Thrown if the given path does not exist, or is not a file.</exception>
	public static async Task<string> ProbeAsync(string path, string[] arguments)
	{
		if (!File.Exists(path)) { throw new FileNotFoundException($"File at {path} does not exist"); }

		List<string> args =
		[
			"-loglevel",
			"error"
		];
		args.AddRange(arguments);
		args.AddRange(["-i", path]);
		BufferedCommandResult ffprobeOutput = await Cli.Wrap("ffprobe")
			.WithArguments(args)
			.ExecuteBufferedAsync();
		return ffprobeOutput.StandardOutput;
	}

	/// <summary>
	///     Detects the duration of a video file.
	/// </summary>
	/// <param name="path">The path of the file to be processed.</param>
	/// <returns>The duration of the video in seconds.</returns>
	/// <exception cref="InvalidDataException">Thrown when FFprobe returns an invalid duration.</exception>
	public static async Task<int> GetDuration(string path)
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
		string ffprobeOutput = await ProbeAsync(path, args);
		return float.TryParse(ffprobeOutput, out float durationFloat)
			? (int)durationFloat
			: throw new InvalidDataException(
				$"{ffprobeOutput} (from file '{Path.GetFileNameWithoutExtension(path)}') is not a float");
	}

	/// <summary>
	///     Detects the number of audio channels in a video file.
	/// </summary>
	/// <param name="path">The path of the file to be processed.</param>
	/// <returns>The number of audio channels.</returns>
	public static async Task<int> GetChannelCount(string path)
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
		string ffprobeOutput = await ProbeAsync(path, args);
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
	/// <returns>The crop information, in FFmpeg's format.</returns>
	public static async Task<string> GetCroppingConfig(string path)
	{
		int startTime = await GetDuration(path) < 600 ? 0 : 300;
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
		string ffmpegOutput = await AnalyzeAsync(path, argsBefore, argsAfter);
		return CroppingRegex().Match(ffmpegOutput).Groups[0].Value;
	}

	/// <summary>
	///     The regular expression filter used to parse ffmpeg's crop-detection filter.
	/// </summary>
	/// <returns>A compiled regular expression pattern.</returns>
	[GeneratedRegex("crop=.*", RegexOptions.RightToLeft)]
	private static partial Regex CroppingRegex();
}
