namespace MediaBox.Encoding;

public interface IAudioEncoder : IEncoder
{
	int AudioBitrate { get; }
}
