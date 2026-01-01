namespace MediaBox.Core.ExternalProcess;

/// <summary>
/// 	Manage MakeMKV commands.
/// </summary>
public static class MakeMKV
{
	/// <summary>
	/// 	Run MakeMKV with the given configuration.
	/// </summary>
	/// <param name="outPath">The path where the output file(s) will be written.</param>
	/// <param name="arguments">Arguments to be passed to MakeMKV.</param>
	/// <param name="cts">Cancellation token to cancel MakeMKV operations.</param>
	/// <returns></returns>
	/// <exception cref="FileNotFoundException"></exception>
	/// <exception cref="IOException"></exception>
	public static async Task RunAsync(string outPath, IEnumerable<string> arguments, CancellationToken cts = default)
	{
		// Get path of makemkvcon64/makemkvcon
		string makemkv;
		try
		{
			makemkv = ProcessManager.GetPath("makemkvcon64");
		}
		catch (FileNotFoundException)
		{
			try
			{
				makemkv = ProcessManager.GetPath("makemkvcon");
			}
			catch
			{
				throw new FileNotFoundException("Could not find 'makemkvcon64' or 'makemkvcon'.");
			}
		}

		// Skip output directory if it is *not* empty
		if (Directory.Exists(outPath) && Directory.EnumerateFileSystemEntries(outPath).Any())
		{
			throw new IOException($"Directory at '{outPath}' is not empty.");
		}

		// Backup
		await ProcessManager.StartAsync(makemkv, [.. arguments], ct: cts);
		cts.Register(() =>
		{
			if (Directory.Exists(outPath))
			{
				Directory.Delete(outPath, true);
			}
		});
	}
}
