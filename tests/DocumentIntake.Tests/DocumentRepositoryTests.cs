using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentIntake.Api.Models;
using DocumentIntake.Api.Services;
using Xunit;

namespace DocumentIntake.Tests;

public sealed class DocumentRepositoryTests
{
    [Fact]
    public async Task UpsertSubmissionAsync_ReusesExistingRecord_ForSameProviderAndSourceDocumentId()
    {
        var repo = new InMemoryDocumentRepository();
        var request = NewRequest("DOC-1", "ProviderA", "First title");

        var first = await repo.UpsertSubmissionAsync(request, "key-1.txt", 5, CancellationToken.None);
        var second = await repo.UpsertSubmissionAsync(
            request with { Title = "Updated title" },
            "key-2.txt",
            7,
            CancellationToken.None);

        Assert.True(second.IsDuplicate);
        Assert.Equal(first.Record.DocumentId, second.Record.DocumentId);
        Assert.Equal("Updated title", second.Record.Title);
        Assert.Equal(7, second.Record.ContentLength);
        Assert.Contains(second.Record.AuditTrail, x => x.Event == "received");
    }

    private static DocumentSubmissionRequest NewRequest(string sourceId, string provider, string title) => new(
        sourceId,
        provider,
        title,
        "ZA",
        ["legal"],
        ["tax"],
        DateTimeOffset.UtcNow,
        "text/plain",
        "doc.txt",
        Convert.ToBase64String("hello"u8.ToArray()));
}