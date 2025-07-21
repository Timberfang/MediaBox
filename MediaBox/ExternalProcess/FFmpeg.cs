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
		// Only use a cancellation token for the long-running process
		// The others finish so quickly it doesn't really matter
		Console.WriteLine("Starting...");
		using CancellationHandler cancellation = new();
		await Cli.Wrap("ffmpeg")
			.WithArguments(
				$"-loglevel error -nostdin -i \"{config.InPath}\" {config.Arguments} \"{config.OutPath}\"")
			.ExecuteAsync(cancellation.Token);
	}

	/// <summary>
	///     Detects the duration of a video file.
	/// </summary>
	/// <param name="path">The path of the file to be processed.</param>
	/// <returns>An integer representing the duration of the video in seconds.</returns>
	/// <exception cref="InvalidDataException">Thrown when FFprobe returns an invalid duration.</exception>
	public static async Task<int> GetDuration(string path)
	{
		BufferedCommandResult ffprobeOutput = await Cli.Wrap("ffprobe")
			.WithArguments(
				$"-select_streams v:0 -show_entries format=duration -of compact=p=0:nk=1 -loglevel error \"{path}\"")
			.ExecuteBufferedAsync();
		if (!float.TryParse(ffprobeOutput.StandardOutput, out float durationFloat))
		{
			throw new InvalidDataException(
				$"{ffprobeOutput} (from file '{Path.GetFileNameWithoutExtension(path)}') is not a float");
		}
		return (int)durationFloat;
	}

	/// <summary>
	///     Detects the number of audio channels in a video file.
	/// </summary>
	/// <param name="path">The path of the file to be processed.</param>
	/// <returns>An integer representing the number of audio channels.</returns>
	public static async Task<int> GetChannelCount(string path)
	{
		BufferedCommandResult ffprobeOutput = await Cli.Wrap("ffprobe")
			.WithArguments(
				$"-select_streams a:0 -show_entries stream=channels -of compact=p=0:nk=1 -loglevel error \"{path}\"")
			.ExecuteBufferedAsync();
		return int.TryParse(ffprobeOutput.StandardOutput, out int channels) ? channels : 0;
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
	/// <returns>A string containing the crop information, in FFmpeg's format.</returns>
	public static async Task<string> GetCroppingConfig(string path)
	{
		int startTime = await GetDuration(path) < 600 ? 0 : 300;
		BufferedCommandResult ffmpegOutput = await Cli.Wrap("ffmpeg")
			.WithArguments(
				$"-skip_frame nokey -hide_banner -nostats -noaccurate_seek -ss {startTime} -i \"{path}\" -frames:v 20 -vf cropdetect -an -f null -")
			.ExecuteBufferedAsync();
		return CroppingRegex().Match(ffmpegOutput.StandardOutput).Groups[0].Value;
	}

	/// <summary>
	///     The regular expression filter used to parse ffmpeg's crop-detection filter.
	/// </summary>
	/// <returns>A compiled regular expression pattern.</returns>
	[GeneratedRegex("crop=.*", RegexOptions.RightToLeft)]
	private static partial Regex CroppingRegex();
}
