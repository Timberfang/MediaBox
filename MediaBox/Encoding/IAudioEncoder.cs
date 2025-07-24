namespace MediaBox.Encoding;

public interface IAudioEncoder : IEncoder
{
	/// <summary>
	///     The target bitrate for the audio stream.
	/// </summary>
	int AudioBitrate { get; }

	/// <summary>
	///     The audio codec to use for encoding.
	/// </summary>
	AudioCodec AudioCodec { get; set; }
}
