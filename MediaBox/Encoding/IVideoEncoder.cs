namespace MediaBox.Encoding;

public interface IVideoEncoder : IEncoder
{
	int VideoPreset { get; }
	int VideoQuality { get; }
	int AudioBitrate { get; }
}
