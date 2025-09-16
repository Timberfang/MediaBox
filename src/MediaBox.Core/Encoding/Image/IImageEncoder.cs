using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Encoding.Image;

public interface IImageEncoder : IEncoder
{
	/// <summary>
	///     The image codec to use for encoding.
	/// </summary>
	ImageCodec ImageCodec { get; set; }
}
