using MediaBox.Core.OpticalMedia;

namespace MediaBox.Core.ExternalProcess;

public static class MakeMKV
{
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
