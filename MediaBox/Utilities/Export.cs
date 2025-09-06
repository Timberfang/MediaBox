using System.Text.Json;

using MediaBox.Metadata;

namespace MediaBox.Utilities;

public static class Export
{
	public static void ExportMetadata(string path, MediaInfo mediaInfo)
	{
		if (File.Exists(path)) { throw new IOException($"File at '{path}' already exists"); }
		string json = JsonSerializer.Serialize(mediaInfo, MediaInfo.SourceGenerationContext.Default.MediaInfo);
		File.WriteAllText(path, json);
	}

	public static void ExportMetadata(FileInfo path, MediaInfo mediaInfo)
	{
		ExportMetadata(path.FullName, mediaInfo);
	}
}
