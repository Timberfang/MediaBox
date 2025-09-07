namespace MediaBox.Core.External;

/// <summary>
///     A configuration object for FFmpeg.
/// </summary>
/// <param name="InPath">The path to the input file.</param>
/// <param name="OutPath">The path where the output file will be written.</param>
/// <param name="Arguments">Arguments to be passed to FFmpeg.</param>
public record FFmpegConfig(
	string InPath,
	string OutPath,
	IEnumerable<string> Arguments,
	CancellationToken CancellationToken);
