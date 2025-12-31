namespace MediaBox.Core.Encoding;

/// <summary>
/// 	Interface for media encoders.
/// </summary>
public interface IEncoder
{
	/// <summary>
	///     The path to a file or directory containing input media.
	/// </summary>
	string InPath { get; set; }

	/// <summary>
	///     The path to a file or directory where the encoded media will be saved.
	/// </summary>
	/// <remarks>
	///     If more than one file is provided, outPath must be a directory.
	/// </remarks>
	string OutPath { get; set; }

	/// <summary>
	///     The encoding preset to use: "Quality", "Normal", or "Fast".
	/// </summary>
	EncoderPreset Preset { get; set; }

	/// <summary>
	///     When true, re-encode files that are known to already match the target codec or container.
	/// </summary>
	bool Force { get; set; }

	/// <summary>
	///     An event that's raised whenever encoding starts on a new file.
	/// </summary>
	event EventHandler<string>? FileEncodingStarted;

	/// <summary>
	///     Encodes all valid files in the input path to the output path.
	/// </summary>
	Task EncodeAsync();
}
