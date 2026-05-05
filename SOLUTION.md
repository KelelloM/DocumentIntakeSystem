# SOLUTION.md

## 1. Overview

This project implements a **Document Intake System** using **.NET 8 Minimal APIs**.

The system is responsible for:

- Accepting document submissions from upstream providers  
- Storing raw document content locally  
- Deduplicating repeated submissions  
- Scheduling background processing  
- Generating a preview/summary  
- Exposing endpoints for metadata, content, status, and preview retrieval  

The solution is designed to **run fully locally without any cloud dependencies**, while maintaining a structure that mirrors real-world cloud-based systems.

---

## 2. Architectural Approach

The system follows a **layered architecture with separation of concerns**:


Minimal API (Program.cs)
↓
Repository (Metadata + Deduplication)
↓
Storage (Local File System)
↓
Queue (In-Memory)
↓
Background Worker
↓
Processor (Preview Generation)


Each layer is abstracted using interfaces to allow easy replacement of implementations.

---

## 3. Key Design Decisions

### 3.1 Minimal API Instead of Controllers

The project uses **.NET 8 Minimal APIs**, with endpoints defined in:


Program.cs


**Reason:**
- Less boilerplate
- Faster to build and read
- Keeps focus on core logic

---

### 3.2 Local-Only Implementation

To meet the requirement of running without cloud services:

| Concern | Implementation |
|--------|--------|
| Storage | Local file system |
| Queue | In-memory queue |
| Worker | Hosted background service |
| Metadata | In-memory repository |

**Benefits:**
- No setup required
- Runs instantly
- Easy to test and debug

---

## 4. Core Components

### 4.1 Abstractions Layer

Located in:


Abstractions/


Defines interfaces:

- `IDocumentRepository`
- `IStorage`
- `IQueue`
- `IProcessor`

**Purpose:**
- Decouple logic from implementation
- Enable easy swapping of components

---

### 4.2 Repository (Metadata + Deduplication)

File:


Services/InMemoryDocumentRepository.cs


**Responsibilities:**

- Store document metadata
- Track status and preview
- Maintain audit trail
- Handle deduplication

#### Deduplication Strategy


provider + sourceDocumentId


If the same document is submitted again:
- Existing record is reused
- Audit entry is added
- Processing is triggered again

---

### 4.3 Storage (Local File System)

File:


Storage/LocalStorage.cs


**Responsibilities:**

- Save raw document content
- Retrieve stored content

Files are stored under:


data/


---

### 4.4 Queue (In-Memory)

File:


Queue/InMemoryQueue.cs


**Responsibilities:**

- Accept processing messages
- Hold messages until processed
- Enable asynchronous processing

Uses:


System.Threading.Channels


---

### 4.5 Background Worker

File:


Workers/ProcessingWorker.cs


**Responsibilities:**

- Listen for queued messages
- Process documents asynchronously
- Run continuously in the background

---

### 4.6 Document Processor

File:


Services/DocumentProcessor.cs


**Responsibilities:**

- Read stored content
- Generate preview
- Update status
- Add audit entries

#### Preview Strategy


First N characters of document content


---

### 4.7 API Layer

Defined in:


Program.cs


**Endpoints:**


POST /documents
GET /documents/{documentId}
GET /documents/{documentId}/content
GET /documents/{documentId}/status
GET /documents/{documentId}/preview
GET /documents


---

## 5. Processing Flow

### Document Submission


Client → POST /documents
↓
Repository (deduplication)
↓
Storage (save file)
↓
Queue (enqueue message)
↓
Response returned


---

### Background Processing


Worker → Dequeue message
↓
Processor reads content
↓
Generate preview
↓
Update repository


---

## 6. Audit Trail

Each document stores lifecycle events:


Received
Stored
Queued
Processing
Processed
Failed


Each event includes a timestamp.

---

## 7. Status Flow


Received → Processing → Processed
↘ Failed


---

## 8. Testing Approach

Minimal unit tests are included.

### Repository Tests
- Validate deduplication logic

### Processor Tests
- Validate preview generation
- Validate status updates

---

## 9. CI Pipeline

Located in:


.github/workflows/build-test.yml


Runs:

- Restore
- Build
- Test

---

## 10. Trade-offs

### In-Memory Repository

**Pros:**
- Simple
- Fast

**Cons:**
- Data lost on restart

---

### Local File Storage

**Pros:**
- Easy to use
- No setup

**Cons:**
- Not scalable

---

### In-Memory Queue

**Pros:**
- Lightweight
- Fast

**Cons:**
- No durability
- No retries

---

### Single Process (API + Worker)

**Pros:**
- Simple setup

**Cons:**
- Cannot scale independently

---

## 11. Future Improvements

- Replace repository with database
- Add retry logic
- Add logging
- Add validation
- Add authentication
- Replace queue with message broker
- Add pagination and filters

---

## 12. Summary

This solution provides:

- Document intake API  
- Local storage  
- Deduplication  
- Background processing  
- Preview generation  
- Status tracking  
- Audit trail  
- Unit tests  
- CI pipeline  

The system is **simple, extensible, and aligned with real-world architecture patterns**, while remaining full
