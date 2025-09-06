namespace MediaBox.Metadata;

public interface IMediaInfo
{
	public string Title { get; set; }
	public string Description { get; set; }
	
	public string ToString();
	public string ToJson();
}
