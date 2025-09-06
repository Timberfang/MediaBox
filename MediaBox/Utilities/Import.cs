using System.Text.Json;

using MediaBox.Metadata;

namespace MediaBox.Utilities;

public static class Import
{
	public static MediaInfo ImportMetadata(string path)
	{
		if (!File.Exists(path)) { throw new FileNotFoundException($"File at '{path}' does not exist"); }
		string json = File.ReadAllText(path);
		return JsonSerializer.Deserialize(json, MediaInfo.SourceGenerationContext.Default.MediaInfo)
		       ?? throw new NullReferenceException("File at '{path}' returned null");
	}

	public static MediaInfo ImportMetadata(FileInfo file)
	{
		return ImportMetadata(file.FullName);
	}
}
