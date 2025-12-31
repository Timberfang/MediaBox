namespace MediaBox.Core.Metadata;

/// <summary>
/// 	High-level categories of supported media files.
/// </summary>
public enum MediaType
{
	/// <summary>Video files; electronic moving images which may also contain audio and/or subtitles.</summary>
	Video,
	/// <summary>Audio files; electronic recordings of sound.</summary>
	Audio,
	/// <summary>Image files; electronic representations of still pictures.</summary>
	Image,
	/// <summary>Miscellaneous files; supported media files which do not match a more specific category.</summary>
	Other,
	/// <summary>Unknown files; unrecognized and unsupported files.</summary>
	Unknown
}
