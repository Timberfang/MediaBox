using MediaBox.Utilities;

namespace MediaBox;

internal static class Program
{
	private static async Task<int> Main(string[] args)
	{
		if (args.Length > 0)
		{
			return await CommandLine.StartCommandline(args);
		}

		return 0;
	}
}
