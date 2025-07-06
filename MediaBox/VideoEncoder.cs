using System.Diagnostics;

namespace MediaBox;

public class VideoEncoder : IEncoder
{
	private const string VideoCodec = "libsvtav1";
	private const string AudioCodec = "libopus";
	private readonly int _audioBitrate;

	private readonly string[] _filter =
		[".mkv", ".webm", ".mp4", ".m4v", ".m4a", ".avi", ".mov", ".qt", ".ogv", ".gif"];

	private readonly int _videoPreset;
	private readonly int _videoQuality;

	public VideoEncoder(EncoderPreset preset = EncoderPreset.Normal)
	{
		switch (preset)
		{
			case EncoderPreset.Quality:
				_videoQuality = 27;
				_videoPreset = 6;
				_audioBitrate = 128000;
				break;
			case EncoderPreset.Normal:
			default:
				_videoQuality = 33;
				_videoPreset = 10;
				_audioBitrate = 96000;
				break;
		}
	}

	public string[] GetArgs(string inPath)
	{
		int channels = GetChannelCount(inPath);
		int targetAudioBitrate = channels switch
		{
			>= 7 => Convert.ToInt32(_audioBitrate * 2.5),
			>= 5 => Convert.ToInt32(_audioBitrate) * 2,
			_ => _audioBitrate
		};

		string[] args =
		[
			"-c:v", VideoCodec,
			"-crf", _videoQuality.ToString(),
			"-preset", _videoPreset.ToString(),
			"-c:a", AudioCodec,
			"-b:a", targetAudioBitrate.ToString(),
			"-c:s", "copy",
			"-af",
			"aformat=channel_layouts=7.1|5.1|stereo" // Workaround for a bug with opus in ffmpeg, see https://trac.ffmpeg.org/ticket/5718
		];

		return args;
	}

	public void EncodeDirectory(string inPath, string outPath)
	{
		IEnumerable<string> files = Directory.EnumerateFiles(inPath, "*", SearchOption.AllDirectories)
			.Where(f => _filter.Contains(Path.GetExtension(f)));
		foreach (string file in files)
		{
			string target = Path.Combine(outPath, Path.GetFileNameWithoutExtension(file) + ".mkv");
			Encode(file, target);
		}
	}

	public void Encode(string inPath, string outPath)
	{
		using Process ffmpeg = new();
		ffmpeg.StartInfo.FileName = "ffmpeg.exe";
		ffmpeg.StartInfo.Arguments = $"-i \"{inPath}\" {string.Join(" ", GetArgs(inPath))} \"{outPath}\" -nostdin";
		ffmpeg.Start();
		ffmpeg.WaitForExit();
	}

	private static int GetChannelCount(string inPath)
	{
		using Process ffprobe = new();
		ffprobe.StartInfo.FileName = "ffprobe.exe";
		ffprobe.StartInfo.Arguments =
			$"-select_streams a:0 -show_entries stream=channels -of compact=p=0:nk=1 -loglevel error \"{inPath}\"";
		ffprobe.StartInfo.RedirectStandardOutput = true;
		ffprobe.Start();
		ffprobe.WaitForExit();
		int output = int.Parse(ffprobe.StandardOutput.ReadToEnd());
		return output;
	}
}
