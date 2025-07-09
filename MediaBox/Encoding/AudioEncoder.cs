using Cysharp.Diagnostics;

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
	private readonly string[] _filter = [".mp3", ".wav", ".flac", ".ogg", ".opus"];

	/// <summary>
	///     The bitrate FFmpeg is targeting for the audio stream.
	/// </summary>
	public int AudioBitrate => _audioBitrate[Preset];

	/// <summary>
	///     The path to a file or directory containing input video.
	/// </summary>
	public string InPath { get; set; } = inPath;

	/// <summary>
	///     The path to a file or directory where the encoded video will be saved.
	/// </summary>
	/// <remarks>
	///     If more than one file is provided, outPath must be a directory.
	/// </remarks>
	public string OutPath { get; set; } = outPath;

	/// <summary>
	///     The encoding preset to use: "Quality", "Normal", or "Fast".
	/// </summary>
	public EncoderPreset Preset { get; set; } = preset;

	/// <summary>
	///     Encodes all valid files in the input path to the output path using FFmpeg.
	/// </summary>
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
			string target = Path.ChangeExtension(GetTargetPath(file), ".opus");
			string? targetParent = Directory.GetParent(target)?.FullName;
			if (Path.Exists(target)) { return; }
			if (!Directory.Exists(targetParent) && targetParent != null) { Directory.CreateDirectory(targetParent); }

			// Encode
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
	///     Builds an array of arguments to pass to FFmpeg.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <returns>The path to the file in the output directory.</returns>
	private async Task<string[]> GetArgs(string path)
	{
		// Start most expensive operations in background tasks
		Task<int> channelCountTask = GetChannelCount(path);

		// Build basic arguments
		List<string> args =
		[
			"-c:a", AudioCodec,
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
		: Path.ChangeExtension(OutPath, ".opus");
}
