1\. Overview



This project implements a Document Intake System using .NET 8 Minimal APIs.



The system is responsible for:



Accepting document submissions from upstream providers

Storing raw document content locally

Deduplicating repeated submissions

Scheduling background processing

Generating a preview/summary

Exposing endpoints for metadata, content, status, and preview retrieval



The solution is intentionally designed to run locally without any cloud dependencies, while maintaining a structure that mirrors real-world cloud-based systems.



2\. Architectural Approach



The system follows a layered architecture with clear separation of concerns:



Minimal API (Program.cs)

&#x20;       ↓

Repository (Metadata + Deduplication)

&#x20;       ↓

Storage (Local File System)

&#x20;       ↓

Queue (In-Memory)

&#x20;       ↓

Background Worker

&#x20;       ↓

Processor (Preview Generation)



Each layer is abstracted using interfaces to ensure flexibility and replaceability.



3\. Key Design Decisions

3.1 Minimal API Instead of Controllers



The project uses .NET 8 Minimal APIs, with all endpoints defined in:



Program.cs



This decision was made to:



Reduce boilerplate

Keep the project concise for the assignment

Focus on core logic instead of framework structure

3.2 Local-Only Implementations



The assignment allows local substitutes instead of real cloud services.



The following decisions were made:



Concern	Implementation

Object Storage	Local file system

Queue	In-memory queue

Background Processing	Hosted service

Metadata Store	In-memory repository



This ensures:



Zero external dependencies

Fast setup and testing

Clear demonstration of system behavior

4\. Core Components

4.1 Abstractions Layer



Located in:



Abstractions/



Defines interfaces such as:



IDocumentRepository

IStorage

IQueue

IProcessor



Purpose:



Decouple business logic from implementation

Allow easy replacement of local implementations with cloud services

4.2 Repository (Metadata + Deduplication)



File:



Services/InMemoryDocumentRepository.cs



Responsibilities:



Store document metadata

Maintain audit trail

Track status and preview

Handle deduplication

Deduplication Strategy



Documents are uniquely identified by:



provider + sourceDocumentId



If the same combination is submitted again:



The existing document record is reused

A new audit entry is added

Processing is triggered again



This prevents duplicate internal records while still tracking repeated submissions.



4.3 Storage (Local File System)



File:



Storage/LocalStorage.cs



Responsibilities:



Store raw document content as files

Retrieve stored content



Documents are stored under:



data/



Each file is saved with a unique name to avoid collisions.



4.4 Queue (In-Memory)



File:



Queue/InMemoryQueue.cs



Responsibilities:



Accept processing messages

Hold messages until processed

Decouple intake from processing



Implementation uses:



System.Threading.Channels



This provides thread-safe, asynchronous message handling.



4.5 Background Worker



File:



Workers/ProcessingWorker.cs



Responsibilities:



Continuously listen to the queue

Process messages asynchronously

Trigger document processing



This runs automatically when the application starts.



4.6 Document Processor



File:



Services/DocumentProcessor.cs



Responsibilities:



Read stored document content

Generate a preview/summary

Update document status

Add audit entries

Preview Strategy



The preview is generated as:



First N characters of the document content



This keeps the implementation simple while fulfilling the requirement.



4.7 API Layer



Defined in:



Program.cs



Key endpoints include:



POST /documents

GET /documents/{documentId}

GET /documents/{documentId}/content

GET /documents/{documentId}/status

GET /documents/{documentId}/preview

GET /documents



Responsibilities:



Accept document submissions

Return metadata

Return raw content

Return processing status

Return generated preview

5\. Processing Flow

Document Submission

Client → POST /documents

&#x20;       ↓

Repository (deduplication)

&#x20;       ↓

Storage (save raw content)

&#x20;       ↓

Queue (enqueue processing message)

&#x20;       ↓

Response returned immediately

Background Processing

Worker → Dequeue message

&#x20;      ↓

Processor reads content

&#x20;      ↓

Generate preview

&#x20;      ↓

Update repository (status + audit)

6\. Audit Trail



Each document maintains a minimal but meaningful audit trail:



Received

Stored

Queued

Processing

Processed

Failed



Each event includes a timestamp.



This provides visibility into document lifecycle without overcomplicating the design.



7\. Status Handling



Document status transitions:



Received → Processing → Processed

&#x20;                 ↘ Failed



Status is updated during processing and exposed via API.



8\. Testing Approach



The solution includes minimal unit tests focusing on core logic:



Repository Tests

Verify deduplication logic

Ensure same document is not duplicated

Processor Tests

Verify preview generation

Ensure status updates correctly



Integration tests were intentionally omitted to keep the scope aligned with the assignment.



9\. CI Pipeline



Located in:



.github/workflows/build-test.yml



Pipeline steps:



Restore dependencies

Build solution

Run unit tests



This ensures code quality and build consistency on every push.



10\. Trade-offs

In-Memory Repository



Pros:



Simple

Fast

Easy to implement



Cons:



Data is lost on restart

Not suitable for production

Local File Storage



Pros:



No external dependency

Easy debugging



Cons:



Not scalable

No redundancy

In-Memory Queue



Pros:



Simple and fast

No infrastructure required



Cons:



Messages lost on crash

No retry mechanism

Single Process (API + Worker)



Pros:



Easy to run locally

Minimal setup



Cons:



No independent scaling

Tight coupling of workloads

11\. Future Improvements



If extended beyond the assignment:



Replace in-memory repository with a database (e.g., SQLite/PostgreSQL)

Add retry logic for failed processing

Add structured logging

Add validation and error handling

Introduce authentication/authorization

Replace local queue with a durable message broker

Add pagination and advanced filtering

Support larger file sizes and streaming

12\. Summary



This solution delivers:



Document intake API

Local raw content storage

Deduplication strategy

Background processing

Preview generation

Status tracking

Audit trail

Minimal unit tests

CI pipeline

Fully local execution



The design prioritizes clarity, simplicity, and extensibility, while aligning with real-world backend architecture patterns.

