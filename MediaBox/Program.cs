using MediaBox.Utilities;

namespace MediaBox;

internal static class Program
{
	private static void Main(string[] args)
	{
		if (args.Length > 0)
		{
			CommandLine.StartCommandline(args);
		}
	}
}
