using System.CommandLine;
using MediaBox.Core.Encoding;

namespace MediaBox.CLI.Transcoding;

public class TranscodeCommand
{
	protected static readonly Option<EncoderPreset> s_presetOption = new("--preset", "-p")
	{
		Description = "Quality preset for the media",
		DefaultValueFactory = _ => EncoderPreset.Normal
	};
}
