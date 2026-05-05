using DocumentIntake.Api.Abstractions;

namespace DocumentIntake.Api.Storage;

public sealed class LocalFileObjectStorage(IConfiguration configuration) : IObjectStorage
{
    private readonly string _rootPath = configuration.GetValue<string>("Storage:LocalRoot") ?? "./data/raw-documents";

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        var fullPath = SafeFullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, ct);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct)
    {
        var fullPath = SafeFullPath(key);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    private string SafeFullPath(string key)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, key.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(_rootPath);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage key.");
        return fullPath;
    }
}
