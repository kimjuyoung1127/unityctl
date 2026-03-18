using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Unityctl.Core.Transport;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Core.Tests.Transport;

public class IpcTransportWatchTests
{
    // ─── Helper: write a framed JSON message to the server stream ─────────────

    private static async Task WriteFramedAsync(Stream stream, string json, CancellationToken ct = default)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var header = BitConverter.GetBytes(body.Length);
        await stream.WriteAsync(header.AsMemory(0, 4), ct);
        await stream.WriteAsync(body.AsMemory(), ct);
        await stream.FlushAsync(ct);
    }

    private static string SerializeEnvelope(EventEnvelope evt)
        => JsonSerializer.Serialize(evt, UnityctlJsonContext.Default.EventEnvelope);

    private static EventEnvelope MakeEnvelope(string channel, string eventType)
        => new() { Channel = channel, EventType = eventType, Timestamp = 1_000_000 };

    // ─── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_NoServer_EnumerationCompletesWithoutException()
    {
        var transport = new IpcTransport("/nonexistent/project/path");

        var count = 0;
        await foreach (var _ in transport.SubscribeAsync("all")!)
            count++;

        // No server → handshake fails → iterator just ends (no exception)
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SubscribeAsync_NonNullReturn()
    {
        var transport = new IpcTransport("/nonexistent/project/path");
        var enumerable = transport.SubscribeAsync("all");
        Assert.NotNull(enumerable);
    }

    [Fact]
    public async Task SubscribeAsync_ReceivesThreeEvents()
    {
        var pipeName = $"unityctl_watch_test_{Guid.NewGuid():N}"[..25];

        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();

            // Read the watch request
            var headerBuf = new byte[4];
            await ReadExactAsync(server, headerBuf);
            int len = BitConverter.ToInt32(headerBuf);
            var body = new byte[len];
            await ReadExactAsync(server, body);

            // Send handshake OK
            var ok = JsonSerializer.Serialize(
                CommandResponse.Ok("watch session started"),
                UnityctlJsonContext.Default.CommandResponse);
            await WriteFramedAsync(server, ok);

            // Send 3 events
            for (int i = 0; i < 3; i++)
            {
                var evt = MakeEnvelope("console", "Log");
                await WriteFramedAsync(server, SerializeEnvelope(evt));
            }

            // Close the pipe to end the stream
            server.Disconnect();
        });

        await Task.Delay(80); // give server time to start

        var pipePath = $"\\\\.\\pipe\\{pipeName}";
        // Hack the pipe name via Constants path — build a fake path that hashes to our pipe name
        // Instead, use a project path that we can verify works by intercepting _pipeName.
        // Since we can't control the pipe name easily, use the actual Constants path:
        // For this test, spin up the server with the actual pipe name by computing it.
        // Re-approach: call SubscribeAsync directly with full control:

        var received = new List<EventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // We need the transport to use pipeName; we can't inject it directly.
        // So use a real pipe name matching Constants.GetPipeName for a known path,
        // OR use reflection. Let's use a helper that accepts a pipe name directly:
        var transport = CreateTransportWithPipeName(pipeName);

        await foreach (var evt in transport.SubscribeAsync("all", cts.Token)!)
            received.Add(evt);

        await serverTask;
        Assert.Equal(3, received.Count);
        Assert.All(received, e => Assert.Equal("console", e.Channel));
    }

    [Fact]
    public async Task SubscribeAsync_CloseEvent_EndsEnumeration()
    {
        var pipeName = $"unityctl_watch_close_{Guid.NewGuid():N}"[..25];

        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();

            var headerBuf = new byte[4];
            await ReadExactAsync(server, headerBuf);
            int len = BitConverter.ToInt32(headerBuf);
            var body = new byte[len];
            await ReadExactAsync(server, body);

            // Handshake OK
            var ok = JsonSerializer.Serialize(
                CommandResponse.Ok("watch session started"),
                UnityctlJsonContext.Default.CommandResponse);
            await WriteFramedAsync(server, ok);

            // Send 1 normal event then _close
            await WriteFramedAsync(server, SerializeEnvelope(MakeEnvelope("console", "Log")));
            await WriteFramedAsync(server, SerializeEnvelope(MakeEnvelope("_close", "Shutdown")));

            // Keep server alive briefly
            await Task.Delay(500);
        });

        await Task.Delay(80);

        var received = new List<EventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var transport = CreateTransportWithPipeName(pipeName);

        await foreach (var evt in transport.SubscribeAsync("all", cts.Token)!)
            received.Add(evt);

        await serverTask;

        // Should receive 1 normal event; _close ends the enumeration (not forwarded)
        Assert.Single(received);
        Assert.Equal("console", received[0].Channel);
    }

    [Fact]
    public async Task SubscribeAsync_PipeDisconnect_EndsEnumerationGracefully()
    {
        var pipeName = $"unityctl_watch_disc_{Guid.NewGuid():N}"[..25];

        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();

            var headerBuf = new byte[4];
            await ReadExactAsync(server, headerBuf);
            int len = BitConverter.ToInt32(headerBuf);
            var body = new byte[len];
            await ReadExactAsync(server, body);

            var ok = JsonSerializer.Serialize(
                CommandResponse.Ok("watch session started"),
                UnityctlJsonContext.Default.CommandResponse);
            await WriteFramedAsync(server, ok);

            // Abruptly disconnect without sending close
            server.Disconnect();
        });

        await Task.Delay(80);

        var transport = CreateTransportWithPipeName(pipeName);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var count = 0;
        Exception? caught = null;
        try
        {
            await foreach (var _ in transport.SubscribeAsync("all", cts.Token)!)
                count++;
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        await serverTask;

        // Should end gracefully (no exception propagated)
        Assert.Null(caught);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SubscribeAsync_CancellationToken_StopsIteration()
    {
        var pipeName = $"unityctl_watch_ct_{Guid.NewGuid():N}"[..24];

        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();

            var headerBuf = new byte[4];
            await ReadExactAsync(server, headerBuf);
            int len = BitConverter.ToInt32(headerBuf);
            var body = new byte[len];
            await ReadExactAsync(server, body);

            var ok = JsonSerializer.Serialize(
                CommandResponse.Ok("watch session started"),
                UnityctlJsonContext.Default.CommandResponse);
            await WriteFramedAsync(server, ok);

            // Send events slowly
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    await WriteFramedAsync(server, SerializeEnvelope(MakeEnvelope("console", "Log")));
                    await Task.Delay(20);
                }
                catch { break; }
            }
        });

        await Task.Delay(80);

        using var cts = new CancellationTokenSource();
        var transport = CreateTransportWithPipeName(pipeName);
        var received = new List<EventEnvelope>();

        try
        {
            await foreach (var evt in transport.SubscribeAsync("all", cts.Token)!)
            {
                received.Add(evt);
                if (received.Count >= 3) cts.Cancel();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Should have received ~3 events before cancel
        Assert.True(received.Count >= 3);
        Assert.True(received.Count < 10, "Cancellation should have stopped iteration early");

        await Task.WhenAny(serverTask, Task.Delay(2000));
    }

    [Fact]
    public async Task SubscribeAsync_HeartbeatIsPassedThrough()
    {
        var pipeName = $"unityctl_watch_hb_{Guid.NewGuid():N}"[..25];

        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();

            var headerBuf = new byte[4];
            await ReadExactAsync(server, headerBuf);
            int len = BitConverter.ToInt32(headerBuf);
            var body = new byte[len];
            await ReadExactAsync(server, body);

            var ok = JsonSerializer.Serialize(
                CommandResponse.Ok("watch session started"),
                UnityctlJsonContext.Default.CommandResponse);
            await WriteFramedAsync(server, ok);

            // Send a heartbeat event
            await WriteFramedAsync(server, SerializeEnvelope(MakeEnvelope("_heartbeat", "Ping")));
            await WriteFramedAsync(server, SerializeEnvelope(MakeEnvelope("_close", "Shutdown")));

            await Task.Delay(200);
        });

        await Task.Delay(80);

        var received = new List<EventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var transport = CreateTransportWithPipeName(pipeName);

        await foreach (var evt in transport.SubscribeAsync("all", cts.Token)!)
            received.Add(evt);

        await serverTask;

        // Heartbeat events are yielded (not filtered)
        Assert.Single(received);
        Assert.Equal("_heartbeat", received[0].Channel);
        Assert.Equal("Ping", received[0].EventType);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an IpcTransport whose pipe name matches the given value.
    /// Uses a known project path whose SHA256 hash produces a well-known constant,
    /// OR we inject via a workaround: use the pipe name directly by subclassing.
    ///
    /// Since IpcTransport is sealed and takes a projectPath, we need a project path
    /// whose GetPipeName() output equals pipeName. Instead, we create the server with
    /// the hash of a fixed path and pass that path to the transport.
    /// </summary>
    private static IpcTransport CreateTransportWithPipeName(string pipeName)
    {
        // We can't directly inject the pipe name, but we can use Constants.GetPipeName
        // in reverse. Instead, use a test-specific approach:
        // IpcTransport stores _pipeName = Constants.GetPipeName(projectPath).
        // We start the pipe server with pipeName directly.
        // To make the transport use that same name, use a known project path.
        //
        // Simplest: IpcTransport has an internal constructor that accepts a pipe name directly.
        // Since we can't add one without modifying source (which we shouldn't),
        // we use the pipe name derived from a test project path.
        //
        // For test isolation, we create a transport pointed at the same pipe name
        // by using a special test helper constructor (added as internal).
        return new IpcTransport(pipeName, useRawPipeName: true);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(total));
            if (read == 0) throw new EndOfStreamException();
            total += read;
        }
    }
}
