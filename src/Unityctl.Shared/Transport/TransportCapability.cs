namespace Unityctl.Shared.Transport;

[Flags]
public enum TransportCapability
{
    None = 0,
    Command = 1,
    Streaming = 2,
    Bidirectional = 4,
    LowLatency = 8
}
