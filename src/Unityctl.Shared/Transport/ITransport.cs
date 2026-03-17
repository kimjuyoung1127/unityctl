using Unityctl.Shared.Protocol;

namespace Unityctl.Shared.Transport;

public interface ITransport : IAsyncDisposable
{
    string Name { get; }
    TransportCapability Capabilities { get; }
    Task<CommandResponse> SendAsync(CommandRequest request, CancellationToken ct = default);
    IAsyncEnumerable<EventEnvelope>? SubscribeAsync(string channel, CancellationToken ct = default);
    Task<bool> ProbeAsync(CancellationToken ct = default);
}
