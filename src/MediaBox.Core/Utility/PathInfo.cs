namespace MediaBox.Core.Utility;

public class PathInfo(string path)
{
	public string Path { get; set; } = path;
	public bool IsFile => File.Exists(Path);
	public bool IsDirectory => Directory.Exists(Path);
	public bool IsValid => _isValid();
	public bool IsWritable => _isWritable();
	public bool Exists => System.IO.Path.Exists(Path);

	private bool _isValid()
	{
		char[] invalidPathChars = System.IO.Path.GetInvalidPathChars();
		char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
		return Path.Length > 0
			   && !Path.Any(c => invalidPathChars.Contains(c))
			   && !System.IO.Path.GetFileName(Path).Any(c => invalidChars.Contains(c));
	}

	private bool _isWritable()
	{
		// Best way I know of to test if a path is writable is to actually *write* to it
		if (!IsValid)
		{
			return false;
		}

		// On the rare chance that the randomly-generated number already exists, re-roll it
		Random generator = new();
		string testPath = System.IO.Path.Join(Path, "_tmp_mediabox", generator.Next(0, 99999).ToString("D5"));
		for (int i = 0; i <= 9 && File.Exists(testPath); i++)
		{
			testPath = System.IO.Path.Join(Path, "_tmp_mediabox", generator.Next(0, 99999).ToString("D5"));
		}

		if (File.Exists(testPath))
		{
			throw new InvalidOperationException(
				$"Failed to resolve conflict with file at '{testPath}' after 10 attempts");
		}

		try
		{
			File.WriteAllText(testPath, string.Empty);
			File.Delete(testPath);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return false;
		}

		return true;
	}
}
