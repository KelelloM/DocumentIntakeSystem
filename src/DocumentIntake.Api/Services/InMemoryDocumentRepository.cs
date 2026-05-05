using System.Collections.Concurrent;
using DocumentIntake.Api.Abstractions;
using DocumentIntake.Api.Models;

namespace DocumentIntake.Api.Services;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, DocumentRecord> _documents = new();
    private readonly ConcurrentDictionary<string, Guid> _dedupeIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<(DocumentRecord Record, bool IsDuplicate)> UpsertSubmissionAsync(
        DocumentSubmissionRequest request,
        string storageKey,
        long contentLength,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var dedupeKey = BuildDedupeKey(request.Provider, request.SourceDocumentId);

        await _gate.WaitAsync(ct);
        try
        {
            if (_dedupeIndex.TryGetValue(dedupeKey, out var existingId) && _documents.TryGetValue(existingId, out var existing))
            {
                existing.Title = request.Title;
                existing.Jurisdiction = request.Jurisdiction;
                existing.Categories = request.Categories ?? [];
                existing.Tags = request.Tags ?? [];
                existing.ContentType = request.ContentType;
                existing.FileName = request.FileName;
                existing.StorageKey = storageKey;
                existing.ContentLength = contentLength;
                existing.LastSubmittedAt = now;
                existing.Status = DocumentProcessingStatus.Stored;
                existing.FailureReason = null;
                existing.AuditTrail.Add(new AuditEntry(now, "received", "Duplicate external document received; existing internal document reused."));
                existing.AuditTrail.Add(new AuditEntry(now, "stored", $"Raw content overwritten at {storageKey}."));
                return (existing, true);
            }

            var record = new DocumentRecord
            {
                SourceDocumentId = request.SourceDocumentId,
                Provider = request.Provider,
                Title = request.Title,
                Jurisdiction = request.Jurisdiction,
                Categories = request.Categories ?? [],
                Tags = request.Tags ?? [],
                ContentType = request.ContentType,
                FileName = request.FileName,
                StorageKey = storageKey,
                ContentLength = contentLength,
                ReceivedAt = request.ReceivedAt ?? now,
                LastSubmittedAt = now,
                Status = DocumentProcessingStatus.Stored
            };

            record.AuditTrail.Add(new AuditEntry(now, "received", "New external document received."));
            record.AuditTrail.Add(new AuditEntry(now, "stored", $"Raw content stored at {storageKey}."));

            _documents[record.DocumentId] = record;
            _dedupeIndex[dedupeKey] = record.DocumentId;
            return (record, false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<DocumentRecord?> GetAsync(Guid documentId, CancellationToken ct)
        => Task.FromResult(_documents.TryGetValue(documentId, out var record) ? record : null);

    public Task<IReadOnlyList<DocumentRecord>> ListAsync(string? provider, string? tag, CancellationToken ct)
    {
        IEnumerable<DocumentRecord> query = _documents.Values;
        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(x => string.Equals(x.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(x => x.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));
        return Task.FromResult<IReadOnlyList<DocumentRecord>>(query.OrderByDescending(x => x.LastSubmittedAt).ToList());
    }

    public Task UpdateAsync(DocumentRecord record, CancellationToken ct)
    {
        _documents[record.DocumentId] = record;
        return Task.CompletedTask;
    }

    private static string BuildDedupeKey(string provider, string sourceDocumentId)
        => $"{provider.Trim().ToLowerInvariant()}::{sourceDocumentId.Trim().ToLowerInvariant()}";
}
