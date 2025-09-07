using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Encoding.Image;

public interface IImageEncoder : IEncoder
{
	/// <summary>
	///     An integer between 1 and 100 (inclusive) that approximates image quality.
	/// </summary>
	int ImageQuality { get; }

	/// <summary>
	///     The image codec to use for encoding.
	/// </summary>
	ImageCodec ImageCodec { get; set; }
}
