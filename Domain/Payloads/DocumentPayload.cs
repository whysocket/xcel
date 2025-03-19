using System.Net.Mime;
using FluentValidation;
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

public class DocumentPayloadValidator : AbstractValidator<DocumentPayload>
{
    public DocumentPayloadValidator()
    {
        RuleFor(x => x.Content).NotNull().Must(c => c.Length > 0).WithMessage("Document content is required.");
        RuleFor(x => x.ContentType).NotEmpty().WithMessage("Content type is required.");
        RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required.");
        RuleFor(x => x.ContentType).Must(ct => ct is MediaTypeNames.Application.Pdf)
            .WithMessage("Document must be a PDF document.");
        RuleFor(x => x.Content.Length).LessThanOrEqualTo(5 * 1024 * 1024).WithMessage("Document size must be less than 5MB.");
    }
}