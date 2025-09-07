using MediaBox.Core.Encoding.Audio;
using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Encoding.Video;

public interface IVideoEncoder : IAudioEncoder
{
	/// <summary>
	///     The encoder 'preset' used by FFmpeg.
	/// </summary>
	/// <remarks>
	///     The preset defines the tradeoff between encoding speed and the size of the output file.
	///     Higher preset values result in smaller files but take longer to encode.
	///     Lower preset values result in larger files but encode faster.
	///     Quality is not affected by the preset value.
	/// </remarks>
	int VideoPreset { get; }

	/// <summary>
	///     The CRF (Constant Rate Factor) value used by FFmpeg.
	/// </summary>
	/// <remarks>
	///     CRF is a quality setting that controls the level of compression applied to the video.
	///     Lower CRF values result in higher quality but larger files.
	///     Higher CRF values result in lower quality but smaller files.
	///     CRF is a logarithmic scale, so the difference in quality between two CRF values is not linear.
	/// </remarks>
	int VideoQuality { get; }

	/// <summary>
	///     The video codec to use for encoding.
	/// </summary>
	VideoCodec VideoCodec { get; set; }

	/// <summary>
	///     The subtitle codec to use for encoding.
	/// </summary>
	SubtitleCodec SubtitleCodec { get; set; }
}
