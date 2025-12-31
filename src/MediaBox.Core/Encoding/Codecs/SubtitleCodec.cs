namespace MediaBox.Core.Encoding.Codecs;

/// <summary>
/// 	Supported subtitle codecs.
/// </summary>
public enum SubtitleCodec
{
	/// <summary>Losslessly copy existing subtitles.</summary>
	Copy,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/SubRip">SubRip</see> codec.</summary>
	SRT,
	/// <summary>Transcode to the <see href="http://www.tcax.org/docs/ass-specs.htm">Sub Station Alpha</see> codec.</summary>
	SSA,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/MPEG-4_Part_17">MPEG-4 Timed Text</see> (A.K.A. 'movtext' or MPEG-4 Part 17) codec.</summary>
	MOVTEXT
}
