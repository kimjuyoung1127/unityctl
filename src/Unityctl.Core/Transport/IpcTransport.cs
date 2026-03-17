using Unityctl.Shared.Protocol;
using Unityctl.Shared.Transport;

namespace Unityctl.Core.Transport;

/// <summary>
/// IPC transport: communicates with running Unity Editor via named pipe.
/// Phase 2B: full implementation.
/// </summary>
public sealed class IpcTransport : ITransport
{
    private readonly string _projectPath;

    public string Name => "ipc";
    public TransportCapability Capabilities =>
        TransportCapability.Command | TransportCapability.Streaming |
        TransportCapability.Bidirectional | TransportCapability.LowLatency;

    public IpcTransport(string projectPath)
    {
        _projectPath = projectPath;
    }

    public Task<CommandResponse> SendAsync(CommandRequest request, CancellationToken ct = default)
    {
        // Phase 2B: Named pipe client implementation
        return Task.FromResult(CommandResponse.Fail(StatusCode.PluginNotInstalled,
            "IPC transport not yet implemented. Use batch transport."));
    }

    public IAsyncEnumerable<EventEnvelope>? SubscribeAsync(string channel, CancellationToken ct = default)
    {
        // Phase 3C: streaming implementation
        return null;
    }

    public async Task<bool> ProbeAsync(CancellationToken ct = default)
    {
        // Phase 2B: try connecting to the named pipe
        await Task.CompletedTask;
        return false;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
