namespace MediaBox.Core.Encoding.Codecs;

/// <summary>
/// 	Supported video codecs.
/// </summary>
public enum VideoCodec
{
	/// <summary>Losslessly copy existing video.</summary>
	Copy,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/Advanced_Video_Coding">AVC</see> (A.K.A. H.264 or MPEG-4 Part 10) codec.</summary>
	AVC,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/High_Efficiency_Video_Coding">HEVC</see> (A.K.A. H.265 or MPEG-H Part 2) codec.</summary>
	HEVC,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/AV1">AOMedia Video 1</see> (AV1) codec.</summary>
	AV1,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/VP9">VP9</see> (AV1) codec.</summary>
	VP9
}
