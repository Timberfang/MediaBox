namespace MediaBox.CLI;

internal static class Program
{
	private static async Task<int> Main(string[] args)
	{
		CancellationTokenSource cts = new();
		Console.CancelKeyPress += (_, _) => cts.Cancel();
		AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

		if (args.Length > 0)
		{
			return await CommandLine.StartCommandline(args, cts.Token);
		}

		return await CommandLine.StartCommandline(["--help"], cts.Token);
	}
}
