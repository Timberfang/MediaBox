using System.Runtime.InteropServices;

namespace MediaBox;

public static partial class OpticalDisc
{
	[LibraryImport("winmm.dll", EntryPoint = "mciSendStringW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial void MciSendStringA(string lpstrCommand, string lpstrReturnString, int uReturnLength,
		int hwndCallback);

	public static void OpenTray(char driveLetter)
	{
		MciSendStringA($"open {driveLetter}: type CDaudio alias drive{driveLetter}", "", 0, 0);
		MciSendStringA($"set drive{driveLetter} door open", "", 0, 0);
	}

	public static void CloseTray(char driveLetter)
	{
		MciSendStringA($"open {driveLetter}: type CDaudio alias drive{driveLetter}", "", 0, 0);
		MciSendStringA($"set drive{driveLetter} door closed", "", 0, 0);
	}
}
