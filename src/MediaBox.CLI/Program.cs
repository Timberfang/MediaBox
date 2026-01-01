using System.CommandLine;
using MediaBox.CLI.Transcoding;
using MediaBox.Core.Utility;

namespace MediaBox.CLI;

/// <summary>
/// 	Entrypoint into the MediaBox CLI program.
/// </summary>
internal static class Program
{
	private static readonly Option<bool> s_aboutOption = new("--about")
	{
		Description = "Get copyright information for MediaBox"
	};
	private static readonly Option<bool> s_thirdPartyOption = new("--third-party-notices")
	{
		Description = "Get copyright information for bundled third-party software"
	};

	private static async Task<int> Main(string[] args)
	{
		CancellationTokenSource cts = new();
		Console.CancelKeyPress += (_, _) => cts.Cancel();
		AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

		if (args.Length > 0)
		{
			return await StartCommandline(args, cts.Token);
		}

		return await StartCommandline(["--help"], cts.Token);
	}

	/// <summary>
	///     Parses command-line arguments.
	/// </summary>
	/// <param name="args">Arguments passed to the program.</param>
	/// <param name="ct">Cancellation token to cancel the process.</param>
	/// <returns>0 if the program was successful, and 1 if it was not.</returns>
	private static Task<int> StartCommandline(string[] args, CancellationToken ct = default)
	{
		// Transcoding command
		Command transcodeCommand = new("transcode", "Transcode media to a different format")
		{
			new VideoCommand().Command, new AudioCommand().Command, new ImageCommand().Command
		};

		// Root command
		RootCommand rootCommand = new()
		{
			Description = "Manage your digital media.",
			Subcommands = { transcodeCommand },
			Options = { s_aboutOption, s_thirdPartyOption }
		};
		rootCommand.SetAction(parseResult =>
			{
				if (parseResult.GetValue(s_aboutOption))
				{
					Console.WriteLine(License.Copyright);
				}
				else if (parseResult.GetValue(s_thirdPartyOption))
				{
					string separator = Environment.NewLine +
									   "---------------------------------------------------------" +
									   Environment.NewLine;
					Console.WriteLine(string.Join(separator, parseResult.GetValue(s_aboutOption)));
				}
			}
		);

		// Parse arguments
		return rootCommand.Parse(args).InvokeAsync(cancellationToken: ct);
	}
}
