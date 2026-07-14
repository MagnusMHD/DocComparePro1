using System.Text;

namespace DocComparePro.Core;

/// <summary>
/// Persists technical errors without exposing stack traces in the user interface.
/// </summary>
public interface IFileLogger
{
    /// <summary>Writes an error and its exception details to the application log.</summary>
    Task LogErrorAsync(string message, Exception exception, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores logs below the current user's local application data directory.
/// </summary>
public sealed class FileLogger : IFileLogger
{
    private readonly string logFilePath;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    /// <summary>Creates the log directory when it does not exist.</summary>
    public FileLogger()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DocComparePro",
            "Logs");
        Directory.CreateDirectory(directory);
        logFilePath = Path.Combine(directory, "application.log");
    }

    /// <inheritdoc />
    public async Task LogErrorAsync(
        string message,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entry = new StringBuilder()
            .Append('[').Append(DateTimeOffset.Now.ToString("O")).AppendLine("] ERROR")
            .AppendLine(message)
            .AppendLine(exception.ToString())
            .AppendLine(new string('-', 80))
            .ToString();

        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(logFilePath, entry, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }
}