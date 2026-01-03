using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MediaBox.CLI;

public partial class SignalHandler(CancellationTokenSource cts)
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

	[SupportedOSPlatform("windows")]
	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetConsoleCtrlHandler(Action handler, [MarshalAs(UnmanagedType.Bool)] bool add);
}
