using Domain.Payloads;

namespace Domain.Interfaces.Services;

public interface IFileService
{
    Task<string> UploadAsync(DocumentPayload file, CancellationToken cancellationToken = default);
}