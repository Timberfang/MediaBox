namespace MediaBox.Metadata;

public interface IMediaInfo
{
	string Title { get; set; }
	string Description { get; set; }

	string ToString();
	string ToJson();
}
