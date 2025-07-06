namespace MediaBox;

public interface IEncoder
{
	string InPath { get; set; }
	string OutPath { get; set; }
	EncoderPreset Preset { get; set; }

	void Encode();
	void Encode(string path);
	void EncodeDirectory();
}
