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

	public static string GetPath(string name)
	{
		name = ConvertPath(name);
		string path;
		if (Exists(name, true))
		{
			path = Path.Join(AppContext.BaseDirectory, name);
		}
		else if (Exists(name))
		{
			path = name;
		}
		else
		{
			throw new FileNotFoundException(name);
		}

		return path;
	}

	public static bool Exists(string name, bool local = false)
	{
		name = ConvertPath(name);
		if (local)
		{
			return File.Exists(Path.Join(AppContext.BaseDirectory, name));
		}

		if (OperatingSystem.IsWindows())
		{
			using Process process = new();
			process.StartInfo.FileName = "where.exe";
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.Arguments = $"/Q {name}";
			process.Start();
			process.WaitForExit();
			return process.ExitCode == 0;
		}
		else
		{
			using Process process = new();
			process.StartInfo.FileName = "which";
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.Arguments = name;
			process.Start();
			process.WaitForExit();
			return process.ExitCode == 0;
		}
	}

	public static string ConvertPath(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				return $"{path}.exe";
			}
		}
		else
		{
			if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				return path[..^4];
			}
		}

		return path;
	}
}
