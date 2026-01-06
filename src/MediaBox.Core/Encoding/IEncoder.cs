namespace MediaBox.Core.Encoding;

/// <summary>
/// 	Interface for media encoders.
/// </summary>
public interface IEncoder
{
	/// <summary>Path to the media file or directory.</summary>
	string InPath { get; set; }

	/// <summary>Path where the transcoded media will be saved.</summary>
	string OutPath { get; set; }

	/// <summary><see cref="EncoderPreset"/>: favor quality, speed, file size, use a balanced approach.</summary>
	EncoderPreset Preset { get; set; }

	/// <summary>Encode files even if automatic filtering would normally exclude them.</summary>
	/// <remarks>Files already using the target codec(s) and file extension are normally excluded.</remarks>
	public bool Force { get; set; }

	/// <summary>An event that's raised whenever encoding starts on a new file.</summary>
	event EventHandler<string>? FileEncodingStarted;

	/// <summary>An event that's raised whenever a non-terminating error occurs.</summary>
	event EventHandler<string>? Error;

	/// <summary>Encodes all valid files in the input path to the output path.</summary>
	Task EncodeAsync();
}
