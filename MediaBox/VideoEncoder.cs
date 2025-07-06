using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MediaBox;

public partial class VideoEncoder(string inPath, string outPath, EncoderPreset preset = EncoderPreset.Normal)
	: IEncoder
{
	private const string VideoCodec = "libsvtav1";
	private const string AudioCodec = "libopus";

	private readonly Dictionary<EncoderPreset, int> _audioBitrate =
		new() { { EncoderPreset.Quality, 128000 }, { EncoderPreset.Normal, 96000 } };

	private readonly string[] _filter = [".mkv", ".webm", ".mp4", ".m4v", ".m4a", ".avi", ".mov", ".qt", ".ogv"];

	private readonly Dictionary<EncoderPreset, int> _videoPreset =
		new() { { EncoderPreset.Quality, 6 }, { EncoderPreset.Normal, 10 } };

	private readonly Dictionary<EncoderPreset, int> _videoQuality =
		new() { { EncoderPreset.Quality, 27 }, { EncoderPreset.Normal, 33 } };

	public int VideoPreset => _videoPreset[Preset];

	public int VideoQuality => _videoQuality[Preset];

	public int AudioBitrate => _audioBitrate[Preset];

	public string InPath { get; set; } = inPath;
	public string OutPath { get; set; } = outPath;
	public EncoderPreset Preset { get; set; } = preset;
	public void Encode() => Encode(false, false);

	public void Encode(string path) => Encode(path, false, false);

	// ReSharper disable once MemberCanBePrivate.Global
	public void Encode(bool noSurround, bool noCrop) => Encode(InPath, noSurround, noCrop);

	// ReSharper disable once MemberCanBePrivate.Global
	public void Encode(string path, bool noSurround, bool noCrop)
	{
		string target = GetTargetPath(path);
		string targetParent = Directory.GetParent(target)?.FullName ?? throw new InvalidOperationException();
		if (Path.Exists(target))
		{
			return;
		}

		if (!Directory.Exists(targetParent))
		{
			Directory.CreateDirectory(targetParent);
		}

		using Process ffmpeg = new();
		ffmpeg.StartInfo.FileName = "ffmpeg.exe";
		ffmpeg.StartInfo.Arguments =
			$"-i \"{path}\" {string.Join(" ", GetArgs(path, noSurround, noCrop))} \"{target}\" -nostdin";
		ffmpeg.Start();
		ffmpeg.WaitForExit();

		// ReSharper disable once InvertIf
		if (ffmpeg.ExitCode != 0)
		{
			if (File.Exists(target))
			{
				File.Delete(target);
			}

			throw new InvalidOperationException("ffmpeg crashed, deleting target file");
		}
	}

	public void EncodeDirectory() => EncodeDirectory(false, false);

	// ReSharper disable once MemberCanBePrivate.Global
	public void EncodeDirectory(bool noSurround, bool noCrop)
	{
		IEnumerable<string> files = Directory.EnumerateFiles(InPath, "*", SearchOption.AllDirectories)
			.Where(f => _filter.Contains(Path.GetExtension(f)));
		foreach (string file in files)
		{
			Encode(file, noSurround, noCrop);
		}
	}

	private static int GetChannelCount(string path)
	{
		using Process ffprobe = new();
		ffprobe.StartInfo.FileName = "ffprobe.exe";
		ffprobe.StartInfo.Arguments =
			$"-select_streams a:0 -show_entries stream=channels -of compact=p=0:nk=1 -loglevel error \"{path}\"";
		ffprobe.StartInfo.RedirectStandardOutput = true;
		ffprobe.Start();
		string output = ffprobe.StandardOutput.ReadToEnd();
		ffprobe.WaitForExit();
		if (ffprobe.ExitCode != 0)
		{
			throw new InvalidOperationException(output);
		}

		return int.TryParse(output, out int channels) ? channels : 0;
	}

	private static string GetCroppingConfig(string path)
	{
		using Process ffmpeg = new();
		ffmpeg.StartInfo.FileName = "ffmpeg.exe";
		ffmpeg.StartInfo.Arguments =
			$"-skip_frame nokey -y -hide_banner -nostats -t 10:00 -i \"{path}\" -vf cropdetect -an -f null -";
		ffmpeg.StartInfo.RedirectStandardError = true;
		ffmpeg.Start();
		string output = ffmpeg.StandardError.ReadToEnd();
		ffmpeg.WaitForExit();
		if (ffmpeg.ExitCode != 0)
		{
			throw new InvalidOperationException(output);
		}

		return CroppingRegex().Match(output).Groups[0].Value;
	}

	private string[] GetArgs(string path, bool noSurround, bool noCrop)
	{
		int channels = noSurround ? 2 : GetChannelCount(path);
		int targetAudioBitrate = channels switch
		{
			>= 7 => Convert.ToInt32(AudioBitrate * 2.5),
			>= 5 => Convert.ToInt32(AudioBitrate) * 2,
			_ => AudioBitrate
		};

		List<string> args =
		[
			"-c:v", VideoCodec,
			"-crf", VideoQuality.ToString(),
			"-preset", VideoPreset.ToString(),
			"-c:a", AudioCodec,
			"-b:a", targetAudioBitrate.ToString(),
			"-c:s", "copy",
			"-af",
			"aformat=channel_layouts=7.1|5.1|stereo" // Workaround for a bug with opus in ffmpeg, see https://trac.ffmpeg.org/ticket/5718
		];

		// ReSharper disable once InvertIf
		if (!noCrop)
		{
			string croppingConfig = GetCroppingConfig(path);
			// ReSharper disable once InvertIf
			if (!string.IsNullOrEmpty(croppingConfig))
			{
				args.Add("-vf");
				args.Add(croppingConfig);
			}
		}

		return args.ToArray();
	}

	private string GetTargetPath(string path) => Path.GetExtension(OutPath).Length == 0
		? Path.Join(OutPath, path.Replace(InPath, string.Empty))
		: Path.ChangeExtension(OutPath, ".mkv");

	[GeneratedRegex("crop=.*", RegexOptions.RightToLeft)]
	private static partial Regex CroppingRegex();
}
