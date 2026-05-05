using DocumentIntake.Api.Models;
using DocumentIntake.Api.Services;
using FluentAssertions;

namespace DocumentIntake.Tests;

public sealed class DocumentRepositoryTests
{
    [Fact]
    public async Task UpsertSubmissionAsync_ReusesExistingRecord_ForSameProviderAndSourceDocumentId()
    {
        var repo = new InMemoryDocumentRepository();
        var request = NewRequest("DOC-1", "ProviderA", "First title");

        var first = await repo.UpsertSubmissionAsync(request, "key-1.txt", 5, CancellationToken.None);
        var second = await repo.UpsertSubmissionAsync(request with { Title = "Updated title" }, "key-2.txt", 7, CancellationToken.None);

        second.IsDuplicate.Should().BeTrue();
        second.Record.DocumentId.Should().Be(first.Record.DocumentId);
        second.Record.Title.Should().Be("Updated title");
        second.Record.ContentLength.Should().Be(7);
        second.Record.AuditTrail.Select(x => x.Event).Should().Contain("received");
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
