using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Diagnostics;

namespace MediaBox.Encoding;

/// <summary>
///     Encodes a video file from an input path to an output path using FFmpeg.
/// </summary>
/// <param name="inPath">The path to the input video file.</param>
/// <param name="outPath">The path to save the encoded video file.</param>
/// <param name="preset">The encoding preset to use: "Quality", "Normal", or "Fast".</param>
public partial class VideoEncoder(string inPath, string outPath, EncoderPreset preset = EncoderPreset.Normal)
	: IVideoEncoder
{
	// TODO: Make these configurable
	/// <summary>
	///     The video codec to use for encoding. It must be a valid codec for FFmpeg.
	/// </summary>
	private const string VideoCodec = "libsvtav1";

	/// <summary>
	///     The audio codec to use for encoding. It must be a valid codec for FFmpeg.
	/// </summary>
	private const string AudioCodec = "libopus";

	/// <summary>
	///     The target bitrate for the audio stream.
	/// </summary>
	private readonly Dictionary<EncoderPreset, int> _audioBitrate =
		new() { { EncoderPreset.Quality, 128000 }, { EncoderPreset.Normal, 96000 } };

	/// <summary>
	///     An array of file extensions that will be considered 'video' files.
	/// </summary>
	private readonly string[] _filter = [".mkv", ".webm", ".mp4", ".m4v", ".m4a", ".avi", ".mov", ".qt", ".ogv"];

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
	/// </remarks>
	private readonly Dictionary<EncoderPreset, int> _videoQuality =
		new() { { EncoderPreset.Quality, 27 }, { EncoderPreset.Normal, 33 } };

	/// <summary>
	///     The encoder 'preset' used by FFmpeg.
	/// </summary>
	/// <remarks>
	///     The preset defines the tradeoff between encoding speed and the size of the output file.
	///     Higher preset values result in smaller files but take longer to encode.
	///     Lower preset values result in larger files but encode faster.
	///     Quality is not affected by the preset value.
	/// </remarks>
	public int VideoPreset => _videoPreset[Preset];

	/// <summary>
	///     The CRF (Constant Rate Factor) value used by FFmpeg.
	/// </summary>
	/// <remarks>
	///     CRF is a quality setting that controls the level of compression applied to the video.
	///     Lower CRF values result in higher quality but larger files.
	///     Higher CRF values result in lower quality but smaller files.
	///     CRF is a logarithmic scale, so the difference in quality between two CRF values is not linear.
	/// </remarks>
	public int VideoQuality => _videoQuality[Preset];

	/// <summary>
	///     The bitrate FFmpeg is targeting for the audio stream.
	/// </summary>
	public int AudioBitrate => _audioBitrate[Preset];

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
	/// <exception cref="ProcessErrorException">Thrown when FFmpeg exits with a non-zero exit code.</exception>
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
			string target = Path.ChangeExtension(GetTargetPath(file), ".mkv");
			string? targetParent = Directory.GetParent(target)?.FullName;
			if (Path.Exists(target)) { continue; }
			if (!Directory.Exists(targetParent) && targetParent != null) { Directory.CreateDirectory(targetParent); }

			// Encode
			FileEncodingStarted?.Invoke(this, Path.GetFileName(file));
			try
			{
				string[] args = await GetArgs(file);
				await ProcessX
					.StartAsync($"ffmpeg -loglevel error -i \"{file}\" {string.Join(" ", args)} \"{target}\" -nostdin")
					.WaitAsync();
			}
			catch (ProcessErrorException e)
			{
				// FFmpeg outputs to stderror whenever a file is encoded.
				// Only the exit code indicates if it was successful or not.
				if (e.ExitCode == 0) { continue; }
				if (File.Exists(target)) { File.Delete(target); }
				throw;
			}
		}
	}

	/// <summary>
	///     Detects the duration of a video file.
	/// </summary>
	/// <param name="path">The path of the file to be processed.</param>
	/// <returns>An integer representing the duration of the video in seconds.</returns>
	/// <exception cref="InvalidDataException">Thrown when FFprobe returns an invalid duration.</exception>
	private static async Task<int> GetDuration(string path)
	{
		string ffprobeOutput = await ProcessX
			.StartAsync(
				$"ffprobe -select_streams v:0 -show_entries format=duration -of compact=p=0:nk=1 -loglevel error \"{path}\"")
			.FirstAsync();
		if (!float.TryParse(ffprobeOutput, out float durationFloat))
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
	private static async Task<int> GetChannelCount(string path)
	{
		string ffprobeOutput = await ProcessX
			.StartAsync(
				$"ffprobe -select_streams a:0 -show_entries stream=channels -of compact=p=0:nk=1 -loglevel error \"{path}\"")
			.FirstAsync();
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
	/// <returns>A string containing the crop information, in FFmpeg's format.</returns>
	private static async Task<string> GetCroppingConfig(string path)
	{
		// This is necessary because ffmpeg always uses stderror with this command
		int startTime = await GetDuration(path) < 600 ? 0 : 300;
		(_, _, ProcessAsyncEnumerable stdError) = ProcessX.GetDualAsyncEnumerable(
			$"ffmpeg -skip_frame nokey -hide_banner -nostats -noaccurate_seek -ss {startTime} -i \"{path}\" -frames:v 20 -vf cropdetect -an -f null -");
		StringBuilder ffmpegOutput = new();
		await foreach (string line in stdError) { ffmpegOutput.AppendLine(line); }
		return CroppingRegex().Match(ffmpegOutput.ToString()).Groups[0].Value;
	}

	/// <summary>
	///     Builds an array of arguments to pass to FFmpeg.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private async Task<string[]> GetArgs(string path)
	{
		// Start most expensive operations in background tasks
		Task<int> channelCountTask = GetChannelCount(path);
		Task<string> croppingConfigTask = GetCroppingConfig(path);

		// Build basic arguments
		List<string> args =
		[
			"-c:v", VideoCodec,
			"-crf", VideoQuality.ToString(),
			"-preset", VideoPreset.ToString(),
			"-c:a", AudioCodec,
			"-c:s", "copy",
			"-af",
			"aformat=channel_layouts=7.1|5.1|stereo" // Workaround for a bug with opus in ffmpeg, see https://trac.ffmpeg.org/ticket/5718
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
		string croppingConfig = await croppingConfigTask;
		if (!string.IsNullOrEmpty(croppingConfig))
		{
			args.Add("-vf");
			args.Add(croppingConfig);
		}

		// Return output
		return args.ToArray();
	}

	/// <summary>
	///     Replicates the directory structure of the input path in the output path.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private string GetTargetPath(string path) => Path.GetExtension(OutPath).Length == 0
		? Path.Join(OutPath, path.Replace(InPath, string.Empty))
		: Path.ChangeExtension(OutPath, ".mkv");

	/// <summary>
	///     The regular expression filter used to parse ffmpeg's crop-detection filter.
	/// </summary>
	/// <returns>A compiled regular expression pattern.</returns>
	[GeneratedRegex("crop=.*", RegexOptions.RightToLeft)]
	private static partial Regex CroppingRegex();
}
