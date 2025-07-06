namespace MediaBox;

public interface IEncoder
{
	string[] GetArgs(string inPath);
	void Encode(string inPath, string outPath);
	void EncodeDirectory(string inPath, string outPath);
}
