namespace MediaBox.Core.OpticalMedia;

/// <summary>
/// 	Manage optical (CD, DVD, Blu-ray, etc.) drives.
/// </summary>
public interface IOpticalDrive
{
	/// <summary>
	///     If an optical drive is present, get the drive's <see cref="System.IO.DriveInfo"/> object. Otherwise, returns null.
	/// </summary>
	public DriveInfo? DriveInfo { get; }
	/// <summary>
	///     Test if an optical drive is available.
	/// </summary>
	public bool Exists { get; }
	/// <summary>
	///     Test if an optical drive is available and currently contains a disk.
	/// </summary>
	public bool IsReady { get; }
	/// <summary>
	///     An event that's raised whenever a disk backup starts.
	/// </summary>
	event EventHandler<string>? DiskBackupStarted;
	/// <summary>
	///     Open the optical disk tray.
	/// </summary>
	public void Open();
	/// <summary>
	///     Close the optical disk tray.
	/// </summary>
	public void Close();
	/// <summary>
	///     Wait for a disk to be inserted.
	/// </summary>
	public Task WaitForDiskAsync();

	/// <summary>
	///     Backup all valid files from the optical disk to the output path.
	/// </summary>
	/// <returns>A Task object.</returns>
	public Task BackupAsync();
}
