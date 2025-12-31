using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaBox.Core.Utility;

/// <summary>
/// 	Manage external programs.
/// </summary>
internal static class ProcessManager
{
	/// <summary>
	/// 	Launch an external program.
	/// </summary>
	/// <param name="path">Path to the external program or its name if its available on the user's PATH.</param>
	/// <param name="arguments">Arguments to pass directly to the external program.</param>
	/// <param name="ignoreErrors">If set to true, ignore non-zero exit codes and standard error (STDERR) output.</param>
	/// <param name="environmentVariables">Environment variables to be set for the external program.</param>
	/// <returns>Standard output (STDOUT) of the external program.</returns>
	/// <exception cref="ExternalException">Thrown if the external program returns errors and IgnoreErrors is not set to true.</exception>
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


	/// <inheritdoc cref="Start"/>
	/// <summary>
	/// 	Launch an external program asyncrhonously.
	/// </summary>
	/// <param name="path">Path to the external program or its name if its available on the user's PATH.</param>
	/// <param name="arguments">Arguments to pass directly to the external program.</param>
	/// <param name="ignoreErrors">If set to true, ignore non-zero exit codes and standard error (STDERR) output.</param>
	/// <param name="environmentVariables">Environment variables to be set for the external program.</param>
	/// <param name="ct"><see cref="CancellationToken">, which, when triggered, will stop the launched program.</see></param>
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
		ct.Register(() => Stop(process));
		await process.WaitForExitAsync(ct);
		string output = await process.StandardOutput.ReadToEndAsync(ct);
		string errors = await process.StandardError.ReadToEndAsync(ct);
		if (!ignoreErrors && errors.Length > 0)
		{
			throw new ExternalException(errors.Trim());
		}

		return output;
	}

	/// <summary>
	/// 	Get the absolute path to an external program from its name.
	/// </summary>
	/// <param name="name">Name of the external program.</param>
	/// <returns>Absolute path of the external program.</returns>
	/// <exception cref="FileNotFoundException">Thrown when the external program could not be found.</exception>
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

	/// <summary>
	/// 	Test if an external program exists.
	/// </summary>
	/// <param name="name">Name of the external program.</param>
	/// <param name="local">If false, search the user's PATH in addition to the parent program's directory.</param>
	/// <returns>True if the program was found and false if it was not.</returns>
	/// <remarks>
	/// 	If local is false, this requires 'where.exe' to be available on Windows or 'which' to be available on MacOS or Linux.
	/// </remarks>
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

	/// <summary>
	/// 	Add or remove the '.exe' file extension as needed.
	/// </summary>
	/// <param name="path">Path to be converted.</param>
	/// <returns>The converted path.</returns>
	/// <remarks>
	/// 	On Windows, this adds the '.exe' suffix if it's missing; on all other platforms, it removes it if it's present.
	/// </remarks>
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

	/// <summary>
	/// 	Forcefully stop a process.
	/// </summary>
	/// <param name="process">The <see cref="Process"/> object of the process.</param>
	/// <remarks>
	/// 	This is a forceful, non-graceful method of stopping a process. Use with caution.
	/// </remarks>
	private static void Stop(Process process)
	{
		if (!process.HasExited)
		{
			process.Kill();
		}
	}
}
