using System.Threading.Channels;
using DocumentIntake.Api.Abstractions;
using DocumentIntake.Api.Models;

namespace DocumentIntake.Api.Queue;

public sealed class InMemoryProcessingQueue : IProcessingQueue
{
    private readonly Channel<ProcessingMessage> _channel = Channel.CreateUnbounded<ProcessingMessage>();

    public ValueTask EnqueueAsync(ProcessingMessage message, CancellationToken ct)
        => _channel.Writer.WriteAsync(message, ct);

    public ValueTask<ProcessingMessage> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAsync(ct);
}
