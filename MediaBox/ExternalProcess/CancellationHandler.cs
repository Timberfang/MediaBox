namespace MediaBox.ExternalProcess;

public class CancellationHandler : IDisposable
{
	private readonly CancellationTokenSource _tokenSource;
	private bool stopping;

	public CancellationHandler()
	{
		_tokenSource = new CancellationTokenSource();
		stopping = false;
		Console.CancelKeyPress += (_, _) => Cancel();
		AppDomain.CurrentDomain.DomainUnload += (_, _) => Cancel();
	}

	public CancellationToken Token => _tokenSource.Token;

	public void Dispose()
	{
		_tokenSource.Dispose();
		GC.SuppressFinalize(this);
	}

	public void Cancel()
	{
		if (stopping) { return; }
		Console.WriteLine("Canceling...");
		File.WriteAllText("log.txt", "Canceling...");
		_tokenSource.Cancel();
		stopping = true;
	}
}
