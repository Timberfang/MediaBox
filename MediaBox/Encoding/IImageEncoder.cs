namespace MediaBox.Encoding;

public interface IImageEncoder : IEncoder
{
	int ImageQuality { get; }
}
