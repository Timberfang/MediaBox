namespace MediaBox.Core.Encoding;

/// <summary>
///		Presets for encoder settings.
/// </summary>
public enum EncoderPreset
{
	/// <summary>Aim for quality over speed and file size when transcoding.</summary>
	Quality,
	/// <summary>Aim for a balance of quality, speed, and file size when transcoding.</summary>
	Normal
}
