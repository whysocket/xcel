using System.Collections.Concurrent;
using Domain.Interfaces.Services;
using Domain.Payloads;
using Domain.Results;

namespace Xcel.TestUtils.Mocks.XcelServices;

public class InMemoryFileService : IFileService
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public Task<Result<string>> UploadAsync(DocumentPayload file, CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid()}-{file.FileName}";
        _files.TryAdd(fileName, file.Content);

        return Task.FromResult(Result.Ok(fileName));
    }

    public byte[]? GetFile(string fileName)
    {
        _files.TryGetValue(fileName, out var content);
        return content;
    }
}