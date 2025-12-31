using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Encoding.Audio;

/// <summary>
/// 	Interface for audio encoders.
/// </summary>
public interface IAudioEncoder : IEncoder
{
	/// <summary>
	///     The audio codec to use for encoding.
	/// </summary>
	AudioCodec AudioCodec { get; set; }
}
