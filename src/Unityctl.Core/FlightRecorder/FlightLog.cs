using System.Text.Json;
using Unityctl.Shared;

namespace Unityctl.Core.FlightRecorder;

/// <summary>
/// Append-only NDJSON flight recorder.
/// Phase 3B: full implementation with retention, tail, query.
/// </summary>
public sealed class FlightLog
{
    private readonly string _logDirectory;

    public FlightLog(string? logDirectory = null)
    {
        _logDirectory = logDirectory ??
            Path.Combine(Constants.GetConfigDirectory(), Constants.FlightLogDirectory);
    }

    public void Record(FlightEntry entry)
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            var fileName = $"{DateTimeOffset.UtcNow:yyyy-MM-dd}.ndjson";
            var filePath = Path.Combine(_logDirectory, fileName);
            var json = JsonSerializer.Serialize(entry);
            File.AppendAllText(filePath, json + Environment.NewLine);
        }
        catch
        {
            // Flight recording should never crash the CLI
        }
    }
}
