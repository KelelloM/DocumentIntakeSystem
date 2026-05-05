using DocumentIntake.Api.Services;
using Xunit;

namespace DocumentIntake.Tests;

public sealed class DocumentProcessorTests
{
    [Fact]
    public void CreatePreview_TrimsWhitespace_AndLimitsLength()
    {
        var text = "This   is\n\n a document with   lots of spacing and enough content to trim.";

        var preview = DocumentProcessor.CreatePreview(text, 24);

        Assert.Equal("This is a document with...", preview);
        Assert.True(preview.Length <= 27);
    }
}