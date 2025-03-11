using Domain.Interfaces.Services;
using Domain.Payloads;
using System.Collections.Concurrent;

namespace Domain.IntegrationTests.Services;

public class InMemoryFileService : IFileService
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public Task<string?> UploadAsync(DocumentPayload file, CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid()}-{file.FileName}";
        _files.TryAdd(fileName, file.Content);

        return Task.FromResult<string?>(fileName);
    }

    public byte[]? GetFile(string fileName)
    {
        _files.TryGetValue(fileName, out var content);
        return content;
    }
}