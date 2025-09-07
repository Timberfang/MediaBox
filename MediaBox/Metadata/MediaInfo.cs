using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBox.Metadata;

public partial class MediaInfo : IMediaInfo
{
	public MediaInfo(string? title = null, string? description = null)
	{
		Title = title ?? "{ no title provided }";
		Description = description ?? "{ no description provided }";
	}

	public string Title { get; set; }
	public string Description { get; set; }

	public override string ToString()
	{
		return Title + Environment.NewLine + Environment.NewLine + Description;
	}

	public string ToJson()
	{
		return JsonSerializer.Serialize(this, SourceGenerationContext.Default.MediaInfo);
	}

	[JsonSourceGenerationOptions(WriteIndented = true)]
	[JsonSerializable(typeof(MediaInfo))]
	internal partial class SourceGenerationContext : JsonSerializerContext;
}
