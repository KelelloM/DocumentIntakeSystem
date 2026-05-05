using DocumentIntake.Api.Models;

namespace DocumentIntake.Api.Abstractions;

public interface IDocumentRepository
{
    Task<(DocumentRecord Record, bool IsDuplicate)> UpsertSubmissionAsync(DocumentSubmissionRequest request, string storageKey, long contentLength, CancellationToken ct);
    Task<DocumentRecord?> GetAsync(Guid documentId, CancellationToken ct);
    Task<IReadOnlyList<DocumentRecord>> ListAsync(string? provider, string? tag, CancellationToken ct);
    Task UpdateAsync(DocumentRecord record, CancellationToken ct);
}

public interface IObjectStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct);
}

public interface IProcessingQueue
{
    ValueTask EnqueueAsync(ProcessingMessage message, CancellationToken ct);
    ValueTask<ProcessingMessage> DequeueAsync(CancellationToken ct);
}

public interface IDocumentProcessor
{
    Task ProcessAsync(ProcessingMessage message, CancellationToken ct);
}
