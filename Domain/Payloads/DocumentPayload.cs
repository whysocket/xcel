using FluentValidation;

namespace Domain.Payloads;

public record DocumentPayload(
    string FileName,
    string ContentType,
    byte[] Content);

public class DocumentPayloadValidator : AbstractValidator<DocumentPayload>
{
    public DocumentPayloadValidator()
    {
        RuleFor(x => x.Content).NotNull().Must(c => c.Length > 0).WithMessage("Document content is required.");
        RuleFor(x => x.ContentType).NotEmpty().WithMessage("Content type is required.");
        RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required.");
        RuleFor(x => x.ContentType).Must(ct => ct == "application/pdf" || ct == "application/msword" || ct == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .WithMessage("Document must be a PDF or Word document.");
        RuleFor(x => x.Content.Length).LessThanOrEqualTo(10 * 1024 * 1024).WithMessage("Document size must be less than 10MB.");
    }
}