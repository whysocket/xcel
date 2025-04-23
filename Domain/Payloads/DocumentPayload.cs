using Microsoft.AspNetCore.Http;

namespace Domain.Payloads;

public record DocumentPayload(
    string FileName,
    string ContentType,
    byte[] Content)
{
    public static async Task<DocumentPayload> FromFileAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return null!;
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileBytes = memoryStream.ToArray();

        return new DocumentPayload(file.FileName, file.ContentType, fileBytes);
    }
}
