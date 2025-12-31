using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Encoding.Image;

/// <summary>
/// 	Interface for image encoders.
/// </summary>
public interface IImageEncoder : IEncoder
{
	/// <summary>
	///     The image codec to use for encoding.
	/// </summary>
	ImageCodec ImageCodec { get; set; }
}
