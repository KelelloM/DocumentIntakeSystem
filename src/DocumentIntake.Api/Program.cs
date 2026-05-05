using System.Text;
using DocumentIntake.Api.Abstractions;
using DocumentIntake.Api.Models;
using DocumentIntake.Api.Queue;
using DocumentIntake.Api.Services;
using DocumentIntake.Api.Storage;
using DocumentIntake.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddSingleton<IObjectStorage, LocalFileObjectStorage>();
builder.Services.AddSingleton<IProcessingQueue, InMemoryProcessingQueue>();
builder.Services.AddSingleton<IDocumentProcessor, DocumentProcessor>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/documents", async (
    DocumentSubmissionRequest request,
    IDocumentRepository repository,
    IObjectStorage storage,
    IProcessingQueue queue,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.SourceDocumentId))
        return Results.BadRequest("provider and sourceDocumentId are required.");

    byte[] bytes;
    try
    {
        bytes = Convert.FromBase64String(request.ContentBase64);
    }
    catch
    {
        return Results.BadRequest("contentBase64 must be valid Base64.");
    }

    if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024)
        return Results.BadRequest("content must be between 1 byte and 5 MB.");

    var storageKey = $"{Sanitize(request.Provider)}/{Sanitize(request.SourceDocumentId)}/{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Sanitize(request.FileName)}";
    await storage.SaveAsync(storageKey, new MemoryStream(bytes), request.ContentType, ct);

    var (record, isDuplicate) = await repository.UpsertSubmissionAsync(request, storageKey, bytes.Length, ct);

    record.Status = DocumentProcessingStatus.Queued;
    record.AuditTrail.Add(new AuditEntry(DateTimeOffset.UtcNow, "queued", "Preview generation queued."));
    await repository.UpdateAsync(record, ct);

    await queue.EnqueueAsync(new ProcessingMessage(record.DocumentId, record.SourceDocumentId, record.Provider, "GeneratePreview", DateTimeOffset.UtcNow), ct);

    return Results.Accepted($"/documents/{record.DocumentId}", new DocumentSubmissionResponse(
        record.DocumentId,
        record.SourceDocumentId,
        record.Provider,
        record.Status,
        isDuplicate,
        record.LastSubmittedAt));
});

app.MapGet("/documents/{documentId:guid}", async (Guid documentId, IDocumentRepository repository, CancellationToken ct) =>
{
    var record = await repository.GetAsync(documentId, ct);
    return record is null ? Results.NotFound() : Results.Ok(ToMetadata(record));
});

app.MapGet("/documents", async (string? provider, string? tag, IDocumentRepository repository, CancellationToken ct) =>
{
    var records = await repository.ListAsync(provider, tag, ct);
    return Results.Ok(records.Select(ToMetadata));
});

app.MapGet("/documents/{documentId:guid}/content", async (Guid documentId, IDocumentRepository repository, IObjectStorage storage, CancellationToken ct) =>
{
    var record = await repository.GetAsync(documentId, ct);
    if (record is null) return Results.NotFound();

    var stream = await storage.OpenReadAsync(record.StorageKey, ct);
    return stream is null ? Results.NotFound("Stored content not found.") : Results.File(stream, record.ContentType, record.FileName);
});

app.MapGet("/documents/{documentId:guid}/status", async (Guid documentId, IDocumentRepository repository, CancellationToken ct) =>
{
    var record = await repository.GetAsync(documentId, ct);
    return record is null
        ? Results.NotFound()
        : Results.Ok(new DocumentStatusResponse(record.DocumentId, record.SourceDocumentId, record.Status, DateTimeOffset.UtcNow, record.Preview?.Length ?? 0, record.FailureReason));
});

app.MapGet("/documents/{documentId:guid}/preview", async (Guid documentId, IDocumentRepository repository, CancellationToken ct) =>
{
    var record = await repository.GetAsync(documentId, ct);
    if (record is null) return Results.NotFound();
    if (record.Preview is null) return Results.NotFound("Preview is not available yet.");
    return Results.Ok(new DocumentPreviewResponse(record.DocumentId, record.SourceDocumentId, record.Preview));
});

app.Run();

static DocumentMetadataResponse ToMetadata(DocumentRecord record) => new(
    record.DocumentId,
    record.SourceDocumentId,
    record.Provider,
    record.Title,
    record.Jurisdiction,
    record.Categories,
    record.Tags,
    record.ContentType,
    record.FileName,
    record.ContentLength,
    record.ReceivedAt,
    record.LastSubmittedAt,
    record.Status,
    record.Preview?.Length ?? 0,
    record.AuditTrail);

static string Sanitize(string value)
{
    var invalid = Path.GetInvalidFileNameChars();
    var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    return string.IsNullOrWhiteSpace(cleaned) ? "unnamed" : cleaned.Trim();
}

public partial class Program { }
