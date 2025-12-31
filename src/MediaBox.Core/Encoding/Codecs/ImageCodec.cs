namespace MediaBox.Core.Encoding.Codecs;

/// <summary>
/// 	Supported image codecs.
/// </summary>
public enum ImageCodec
{
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/JPEG">JPEG</see> codec.</summary>
	JPEG,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/PNG">PNG</see> codec.</summary>
	PNG,
	/// <summary>Transcode to the <see href="https://en.wikipedia.org/wiki/WebP">WebP</see> codec.</summary>
	WEBP
}
