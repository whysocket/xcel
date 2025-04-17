using Domain.Payloads;
using Domain.Results;

namespace Domain.Interfaces.Services;

public interface IFileService
{
    Task<Result<string>> UploadAsync(DocumentPayload file, CancellationToken cancellationToken = default);
}