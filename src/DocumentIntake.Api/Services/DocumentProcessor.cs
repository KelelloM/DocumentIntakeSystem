using System.Text;
using DocumentIntake.Api.Abstractions;
using DocumentIntake.Api.Models;

namespace DocumentIntake.Api.Services;

public sealed class DocumentProcessor(IDocumentRepository repository, IObjectStorage storage, ILogger<DocumentProcessor> logger) : IDocumentProcessor
{
    public async Task ProcessAsync(ProcessingMessage message, CancellationToken ct)
    {
        var record = await repository.GetAsync(message.DocumentId, ct);
        if (record is null) return;

        var now = DateTimeOffset.UtcNow;
        record.Status = DocumentProcessingStatus.Processing;
        record.AuditTrail.Add(new AuditEntry(now, "processing", "Preview generation started."));
        await repository.UpdateAsync(record, ct);

        try
        {
            await using var stream = await storage.OpenReadAsync(record.StorageKey, ct)
                ?? throw new FileNotFoundException($"Stored content not found for key {record.StorageKey}.");

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var text = await reader.ReadToEndAsync(ct);
            record.Preview = CreatePreview(text, 300);
            record.Status = DocumentProcessingStatus.Processed;
            record.FailureReason = null;
            record.AuditTrail.Add(new AuditEntry(DateTimeOffset.UtcNow, "processed", $"Preview generated with {record.Preview.Length} characters."));
            await repository.UpdateAsync(record, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId}", record.DocumentId);
            record.Status = DocumentProcessingStatus.Failed;
            record.FailureReason = ex.Message;
            record.AuditTrail.Add(new AuditEntry(DateTimeOffset.UtcNow, "failed", ex.Message));
            await repository.UpdateAsync(record, ct);
        }
    }

    public static string CreatePreview(string text, int maxCharacters)
    {
        var normalized = string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length <= maxCharacters) return normalized;
        return normalized[..maxCharacters].TrimEnd() + "...";
    }
}
