namespace MediaBox.Metadata;

public interface IMetadata
{
	public string Title { get; set; }
	public string Description { get; set; }

	public void Load(string path);
	public void Save(string path);
	public string ToString();
}
