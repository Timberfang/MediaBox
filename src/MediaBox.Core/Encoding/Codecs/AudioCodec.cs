namespace MediaBox.Core.Encoding.Codecs;

/// <summary>
/// 	Supported audio codecs.
/// </summary>
public enum AudioCodec
{
	/// <summary>Losslessly copy existing audio.</summary>
	Copy,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/MP3">MP3</see> codec.</summary>
	MP3,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/Advanced_Audio_Coding">AAC</see> codec.</summary>
	AAC,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/Opus_(audio_format)">Opus</see> codec.</summary>
	OPUS
}
