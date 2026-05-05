# Document Intake System

Small .NET 8 backend for accepting upstream document submissions, storing raw content, queueing background preview generation, and exposing metadata/content/status endpoints.

## Cloud path chosen

Azure-compatible design:

- Object storage abstraction: `IObjectStorage`
- Queue abstraction: `IProcessingQueue`
- Local implementation used by default: file-system object storage + in-memory queue
- Azure SDK packages are referenced so Azure Blob Storage / Service Bus adapters can be added without changing the API or processing flow.

This runs locally without an Azure account.

## Run locally

```bash
./run-local.sh
```

Swagger is available at:

```text
http://localhost:5000/swagger
```

If Kestrel chooses another port, use the URL printed by `dotnet run`.

## Submit a document

```bash
CONTENT=$(printf 'This is a short legal document. It has enough text to generate a preview.' | base64)

curl -X POST http://localhost:5000/documents \
  -H "Content-Type: application/json" \
  -d "{
    \"sourceDocumentId\": \"EXT-123\",
    \"provider\": \"ProviderA\",
    \"title\": \"Example Document\",
    \"jurisdiction\": \"ZA\",
    \"categories\": [\"legal\"],
    \"tags\": [\"tax\", \"draft\"],
    \"receivedAt\": \"2026-04-28T08:00:00Z\",
    \"contentType\": \"text/plain\",
    \"fileName\": \"example.txt\",
    \"contentBase64\": \"$CONTENT\"
  }"
```

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/documents` | Submit metadata + Base64 content. Returns accepted document id. |
| GET | `/documents/{documentId}` | Retrieve metadata and audit trail. |
| GET | `/documents/{documentId}/content` | Download raw stored content. |
| GET | `/documents/{documentId}/status` | Retrieve processing status. |
| GET | `/documents/{documentId}/preview` | Retrieve generated preview. |
| GET | `/documents?provider=ProviderA&tag=tax` | Optional simple listing/filter endpoint. |

## Deduplication

Deduplication key:

```text
provider + sourceDocumentId
```

Repeated submissions reuse the same internal `documentId`, overwrite the latest raw content, re-queue preview generation, and add meaningful audit entries.

## Audit trail

Each document keeps a minimal in-memory audit trail:

- `received`
- `stored`
- `queued`
- `processing`
- `processed`
- `failed`

## Tests

```bash
dotnet test
```

Included tests:

1. Repeated submissions reuse the same document record.
2. Preview generation normalizes whitespace and limits length.

## Notes

- Metadata is in-memory for this exercise.
- Raw documents are stored under `./data/raw-documents`.
- Document size limit is 5 MB.
- Background work runs in-process via `BackgroundService` and an in-memory channel.
