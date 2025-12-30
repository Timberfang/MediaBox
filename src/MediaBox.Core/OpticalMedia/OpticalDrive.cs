using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MediaBox.Core.ExternalProcess;

namespace MediaBox.Core.OpticalMedia;

/// <inheritdoc cref="IOpticalDrive" />
public partial class OpticalDrive : IOpticalDrive
{
	/// <inheritdoc />
	public event EventHandler<string>? DiskBackupStarted;

	/// <summary>
	/// 	Sends a command string to the Windows Media Control Interface (MCI).
	/// </summary>
	/// <param name="command">Command string to send to the control interface.</param>
	/// <param name="returnString">Buffer that receives return information. If no return information is needed, this parameter can be null.</param>
	/// <param name="returnLength">Size, in characters, of the return buffer specified by the lpszReturnString parameter.</param>
	/// <param name="callback">Handle to a callback window if the "notify" flag was specified in the command string.</param>
	/// <returns>0 if successful and any other number if not successful.</returns>
	/// <remarks>
	/// 	This documentation was quoted from Microsoft's official documentation
	/// 	<see href="https://learn.microsoft.com/en-us/previous-versions/dd757161(v=vs.85)">here.</see>
	/// 	Although this API is considered legacy, the replacement (MediaPlayer) does not support optical drives.
	/// </remarks>
	[SupportedOSPlatform("windows")]
	[LibraryImport("winmm.dll", EntryPoint = "mciSendString", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int mciSendString(string command, string? returnString = null, uint returnLength = 0, IntPtr callback = 0);

	/// <inheritdoc />
	public DriveInfo? DriveInfo => DriveInfo.GetDrives().FirstOrDefault(d => d.DriveType == DriveType.CDRom);

	/// <inheritdoc />
	public bool Exists => DriveInfo != null;

	/// <inheritdoc />
	public bool IsReady => DriveInfo != null && DriveInfo.IsReady;

	/// <inheritdoc />
	public void Close()
	{
		if (DriveInfo != null)
		{
			// MacOS and Linux versions are completely untested, and I don't
			// have the hardware to test them.
			if (OperatingSystem.IsWindows())
			{
				_ = mciSendString("set cdaudio door closed");
			}
			else if (OperatingSystem.IsMacOS())
			{
				ProcessManager.Start("/usr/bin/drutil", ["tray", "close"]);
			}
			else if (OperatingSystem.IsLinux())
			{
				ProcessManager.Start("/usr/bin/eject", ["-t"]);
			}
		}
	}

	/// <inheritdoc />
	public void Open()
	{
		if (DriveInfo != null)
		{

			// MacOS and Linux versions are completely untested, and I don't
			// have the hardware to test them.
			if (OperatingSystem.IsWindows())
			{
				_ = mciSendString("set cdaudio door open");
			}
			else if (OperatingSystem.IsMacOS())
			{
				ProcessManager.Start("/usr/bin/drutil", ["tray", "eject"]);
			}
			else if (OperatingSystem.IsLinux())
			{
				ProcessManager.Start("/usr/bin/eject", []);
			}
		}
	}

	/// <inheritdoc />
	public void WaitForDisk()
	{
		while (!IsReady) { Thread.Sleep(1000); }
	}

	/// <inheritdoc cref="BackupAsync()" />
	/// <param name="Destination">The path to save the backup files.</param>
	/// <param name="minLength">Video and audio files below the minimum length will be skipped.</param>
	/// <param name="cts">Token to cancel the backup.</param>
	public async Task BackupAsync(string Destination = "", int minLength = 30, CancellationToken cts = default)
	{
		if (DriveInfo == null) { return; }

		// Prepare output directory - the testDir is used because multiple discs can have same label
		if (Destination.Length == 0) { Destination = Directory.GetCurrentDirectory(); }
		Destination = Path.Join(Destination, DriveInfo.VolumeLabel);
		string testDir = Destination;
		int i = 0;
		while (Directory.Exists(testDir))
		{
			testDir = Destination + '-' + i;
			i++;
		}
		Destination = testDir;
		if (!Directory.Exists(Destination)) { Directory.CreateDirectory(Destination); }

		// Backup
		string[] args =
		[
		  "mkv",
		  "disc:0",
		  "all",
		  $"--minlength={minLength}",
		  Destination
		];
		DiskBackupStarted?.Invoke(this, Path.GetFileName(DriveInfo.VolumeLabel));
		await MakeMKV.RunAsync(Destination, args, cts);
	}

	/// <inheritdoc />
	public async Task BackupAsync() => await BackupAsync();
}
