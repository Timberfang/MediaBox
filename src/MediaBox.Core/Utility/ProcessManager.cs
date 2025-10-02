using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaBox.Core.Utility;

internal static class ProcessManager
{
	public static string Start(string path, IReadOnlyList<string> arguments, bool ignoreErrors = false,
		Dictionary<string, string>? environmentVariables = null)
	{
		using Process process = new();
		process.StartInfo.FileName = path;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		if (environmentVariables != null)
		{
			foreach (string key in environmentVariables.Keys)
			{
				process.StartInfo.EnvironmentVariables[key] = environmentVariables[key];
			}
		}

		process.StartInfo.Arguments = string.Join(" ", arguments.Select(s => $"\"{s}\""));
		process.Start();
		process.WaitForExit();
		string output = process.StandardOutput.ReadToEnd();
		string errors = process.StandardError.ReadToEnd();
		if (!ignoreErrors && errors.Length > 0)
		{
			throw new ExternalException(errors.Trim());
		}

		return output;
	}

	public static async Task<string> StartAsync(string path, IReadOnlyList<string> arguments, bool ignoreErrors = false,
		Dictionary<string, string>? environmentVariables = null, CancellationToken ct = default)
	{
		using Process process = new();
		process.StartInfo.FileName = path;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		if (environmentVariables != null)
		{
			foreach (string key in environmentVariables.Keys)
			{
				process.StartInfo.EnvironmentVariables[key] = environmentVariables[key];
			}
		}

		process.StartInfo.Arguments = string.Join(" ", arguments.Select(s => $"\"{s}\""));
		process.Start();
		await process.WaitForExitAsync(ct);
		string output = await process.StandardOutput.ReadToEndAsync(ct);
		string errors = await process.StandardError.ReadToEndAsync(ct);
		if (!ignoreErrors && errors.Length > 0)
		{
			throw new ExternalException(errors.Trim());
		}

		return output;
	}
}
