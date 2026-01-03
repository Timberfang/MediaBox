using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MediaBox.CLI;

public class SignalHandler(CancellationTokenSource cts)
{
	private readonly CancellationTokenSource _cts = cts;

	public void RegisterSignalHandlers()
	{
		Action handler = Cancel;
		Console.CancelKeyPress += (_, _) => handler.Invoke();
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { SetConsoleCtrlHandler(handler, true); }
		else { PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => Cancel()); }
	}

	private void Cancel()
	{
		_cts.Cancel();
		Environment.Exit(0);
	}

	// Justification: does not support delegates
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
	[SupportedOSPlatform("windows")]
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetConsoleCtrlHandler(Action handler, bool add);
#pragma warning restore SYSLIB1054
}
