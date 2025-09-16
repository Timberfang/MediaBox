using MediaBox.Core.Encoding.Audio;
using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Encoding.Video;

public interface IVideoEncoder : IAudioEncoder
{
	/// <summary>
	///     The video codec to use for encoding.
	/// </summary>
	VideoCodec VideoCodec { get; set; }

	/// <summary>
	///     The subtitle codec to use for encoding.
	/// </summary>
	SubtitleCodec SubtitleCodec { get; set; }
}
