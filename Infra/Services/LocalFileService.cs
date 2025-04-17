using Domain.Interfaces.Services;
using Domain.Results;
using Domain.Payloads;
using Microsoft.Extensions.Logging;

namespace Infra.Services;

internal class LocalFileService(ILogger<LocalFileService> logger) : IFileService
{
    private static class Errors
    {
        public static Error UploadFailed(string fileName) =>
            new(ErrorType.Unexpected, $"Failed to upload {fileName}");
    }

    public async Task<Result<string>> UploadAsync(DocumentPayload file, CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadDirectory = "uploads";

            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            var fileName = $"{Guid.NewGuid()}-{file.FileName}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, file.Content, cancellationToken);

            logger.LogInformation("[LocalFileService] File uploaded successfully: {FilePath}", filePath);

            return Result.Ok(filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LocalFileService] Error uploading file: {FileName}", file.FileName);
            return Result.Fail<string>(Errors.UploadFailed(file.FileName));
        }
    }
}