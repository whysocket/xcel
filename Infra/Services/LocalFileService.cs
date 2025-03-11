using Domain.Interfaces.Services;
using Domain.Payloads;
using Microsoft.Extensions.Logging;

namespace Infra.Services;

public class LocalFileService(ILogger<LocalFileService> logger) : IFileService
{
    public async Task<string?> UploadAsync(DocumentPayload file, CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadDirectory = "uploads";
            // Ensure the upload directory exists
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            // Generate a unique file name
            var fileName = $"{Guid.NewGuid()}-{file.FileName}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            // Write the file content to the file system
            await File.WriteAllBytesAsync(filePath, file.Content, cancellationToken);

            logger.LogInformation("File uploaded successfully: {FilePath}", filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            return null;
        }
    }
}
