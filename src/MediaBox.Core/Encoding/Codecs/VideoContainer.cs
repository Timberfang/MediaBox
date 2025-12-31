namespace MediaBox.Core.Encoding.Codecs;

/// <summary>
///		Supported video containers.
/// </summary>
/// <remarks>
///     A container is not the same as the 'format' used for the video.
/// 	See the codec classes to adjust video/audio/subtitle formats.
/// </remarks>
public enum VideoContainer
{
	/// <summary>Use the <see href="https://en.wikipedia.org/wiki/MP4_file_format">MP4</see> container.</summary>
	MP4,
	/// <summary>Use the <see href="https://en.wikipedia.org/wiki/Matroska">Matroska</see> container.</summary>
	MKV,
	/// <summary>Use the <see href="https://en.wikipedia.org/wiki/WebM">WebM</see> container.</summary>
	WEBM
}
