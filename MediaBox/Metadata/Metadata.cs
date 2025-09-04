using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBox.Metadata;

public partial class MediaInfo : IMetadata
{
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	public void Load(string path)
	{
		if (!File.Exists(path)) { throw new FileNotFoundException($"File at '{path}' does not exist"); }
		string json = File.ReadAllText(path);
		MediaInfo metadata = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.MediaInfo) ?? throw new NullReferenceException("File at '{path}' returned null");
		Title = metadata.Title;
		Description = metadata.Description;
	}

	public void Save(string path)
	{
		if (File.Exists(path)) { throw new IOException($"File at '{path}' already exists"); }
		string json = JsonSerializer.Serialize(this, SourceGenerationContext.Default.MediaInfo);
		File.WriteAllText(path, json);
	}

	public override string ToString()
	{
		return Title + Environment.NewLine + Environment.NewLine + Description;
	}

	[JsonSourceGenerationOptions(WriteIndented = true)]
	[JsonSerializable(typeof(MediaInfo))]
	internal partial class SourceGenerationContext : JsonSerializerContext
	{
	}
}
