namespace MediaBox.Encoding;

public interface IEncoder
{
	string InPath { get; set; }
	string OutPath { get; set; }
	EncoderPreset Preset { get; set; }

	Task EncodeAsync();
}
