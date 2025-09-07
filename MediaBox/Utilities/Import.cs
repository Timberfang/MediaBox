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

	public static async Task<MediaInfo> ImportMetadataAsync(string path)
	{
		if (!File.Exists(path)) { throw new FileNotFoundException($"File at '{path}' does not exist"); }

		string json = await File.ReadAllTextAsync(path);
		return JsonSerializer.Deserialize(json, MediaInfo.SourceGenerationContext.Default.MediaInfo)
		       ?? throw new NullReferenceException("File at '{path}' returned null");
	}

	public static Task<MediaInfo> ImportMetadataAsync(FileInfo file)
	{
		return ImportMetadataAsync(file.FullName);
	}
}
