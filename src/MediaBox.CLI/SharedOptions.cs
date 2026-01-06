using System.CommandLine;

namespace MediaBox.CLI;

public class SharedOptions
{
	internal static readonly Argument<string> s_pathArgument = new("path")
	{
		Description = "Path to the input file or directory",
		Validators =
		{
			result =>
			{
				string path = result.Tokens[0].Value;
				if (!Path.Exists(path))
				{
					result.AddError($"Path at \"{path}\" does not exist");
				}
			}
		}
	};
	internal static readonly Argument<string> s_destinationArgument = new("destination")
	{
		Description = "Path to the output file or directory",
		Validators =
		{
			result =>
			{
				if (result.Tokens.Count <= 0)
				{
					return;
				}

				char[] invalidPathChars = Path.GetInvalidPathChars();
				char[] invalidChars = Path.GetInvalidFileNameChars();
				string path = result.Tokens[0].Value;
				if (path.Length == 0
					|| path.Any(c => invalidPathChars.Contains(c))
					|| Path.GetFileName(path).Any(c => invalidChars.Contains(c)))
				{
					result.AddError($"Path at \"{path}\" is invalid");
				}
			}
		},
		DefaultValueFactory = _ => Directory.GetCurrentDirectory()
	};
	internal static readonly Option<bool> s_forceOption = new("--force")
	{
		Description = "Process files even if they would normally be excluded from processing."
	};
}
