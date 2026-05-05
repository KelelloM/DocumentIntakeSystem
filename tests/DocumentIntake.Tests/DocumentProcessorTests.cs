using DocumentIntake.Api.Models;
using DocumentIntake.Api.Services;
using FluentAssertions;

namespace DocumentIntake.Tests;

public sealed class DocumentProcessorTests
{
    [Fact]
    public void CreatePreview_TrimsWhitespace_AndLimitsLength()
    {
        var text = "This   is\n\n a document with   lots of spacing and enough content to trim.";

        var preview = DocumentProcessor.CreatePreview(text, 24);

        preview.Should().Be("This is a document with...");
        preview.Length.Should().BeLessThanOrEqualTo(27);
    }
}
