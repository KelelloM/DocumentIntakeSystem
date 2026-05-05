namespace DocumentIntake.Api.Models;

public enum DocumentProcessingStatus
{
    Received,
    Stored,
    Queued,
    Processing,
    Processed,
    Failed
}

public sealed record DocumentSubmissionRequest(
    string SourceDocumentId,
    string Provider,
    string Title,
    string? Jurisdiction,
    string[]? Categories,
    string[]? Tags,
    DateTimeOffset? ReceivedAt,
    string ContentType,
    string FileName,
    string ContentBase64);

public sealed record DocumentSubmissionResponse(
    Guid DocumentId,
    string SourceDocumentId,
    string Provider,
    DocumentProcessingStatus Status,
    bool IsDuplicate,
    DateTimeOffset SubmittedAt);

public sealed record ProcessingMessage(
    Guid DocumentId,
    string SourceDocumentId,
    string Provider,
    string Action,
    DateTimeOffset SubmittedAt);

public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    string Event,
    string? Detail = null);

public sealed class DocumentRecord
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();
    public required string SourceDocumentId { get; init; }
    public required string Provider { get; init; }
    public required string Title { get; set; }
    public string? Jurisdiction { get; set; }
    public string[] Categories { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public required string ContentType { get; set; }
    public required string FileName { get; set; }
    public required string StorageKey { get; set; }
    public long ContentLength { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset LastSubmittedAt { get; set; }
    public DocumentProcessingStatus Status { get; set; } = DocumentProcessingStatus.Received;
    public string? Preview { get; set; }
    public string? FailureReason { get; set; }
    public List<AuditEntry> AuditTrail { get; } = [];
}

public sealed record DocumentMetadataResponse(
    Guid DocumentId,
    string SourceDocumentId,
    string Provider,
    string Title,
    string? Jurisdiction,
    string[] Categories,
    string[] Tags,
    string ContentType,
    string FileName,
    long ContentLength,
    DateTimeOffset ReceivedAt,
    DateTimeOffset LastSubmittedAt,
    DocumentProcessingStatus Status,
    int PreviewLength,
    IReadOnlyList<AuditEntry> AuditTrail);

public sealed record DocumentStatusResponse(
    Guid DocumentId,
    string SourceDocumentId,
    DocumentProcessingStatus Status,
    DateTimeOffset Timestamp,
    int PreviewLength,
    string? FailureReason);

public sealed record DocumentPreviewResponse(Guid DocumentId, string SourceDocumentId, string Preview);
