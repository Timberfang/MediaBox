using MediaBox.Core.Encoding.Codecs;

namespace MediaBox.Core.Utility;

internal static class FileManager
{
	/// <summary>
	///		Get the correct extension for the target path.
	/// </summary>
	/// <param name="codec">The audio codec.</param>
	/// <param name="path">The path to get an extension from if the codec is 'Copy'.</param>
	/// <returns>The correct extension for the audio codec.</returns>
	internal static string GetExtension(AudioCodec codec, string path) => codec switch
	{
		AudioCodec.Copy => Path.GetExtension(path),
		AudioCodec.MP3 => ".mp3",
		AudioCodec.AAC => ".aac",
		AudioCodec.OPUS => ".opus",
		_ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unsupported codec.")
	};

	/// <summary>
	///		Get the correct extension for the target path.
	/// </summary>
	/// <param name="codec">The image codec.</param>
	/// <returns>The correct extension for the image codec.</returns>
	internal static string GetExtension(ImageCodec codec) => codec switch
	{
		ImageCodec.JPEG => ".jpg",
		ImageCodec.PNG => ".png",
		ImageCodec.WEBP => ".webp",
		_ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unsupported codec.")
	};

	/// <summary>
	///		Get the correct extension for the target path.
	/// </summary>
	/// <param name="container">The video container.</param>
	/// <returns>The correct extension for the video container.</returns>
	internal static string GetExtension(VideoContainer container) => container switch
	{
		VideoContainer.MP4 => ".mp4",
		VideoContainer.MKV => ".mkv",
		VideoContainer.WEBM => ".webm",
		_ => throw new ArgumentOutOfRangeException(nameof(container), container, "Unsupported container.")
	};

	/// <summary>
	///     Replicates the directory structure of the input path in the output path.
	/// </summary>
	/// <param name="path">The path to the file to be processed.</param>
	/// <param name="rootPath">The portion of the path to be excluded from replication.</param>
	/// <param name="targetPath">The destination to replicate the path at.</param>
	/// <returns>The path to the file in the output directory.</returns>
	internal static string GetTargetPath(string path, string rootPath, string targetPath) =>
		Path.GetExtension(targetPath).Length == 0
			? Path.Join(targetPath, path.Replace(rootPath, string.Empty))
			: targetPath;
}
