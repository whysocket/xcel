namespace Xcel.Services.Exceptions;

public enum EmailServiceFailureReason
{
    SmtpConnectionFailed,
    TemplateCompilationError,
    TemplateFileNotFound,
    TemplateDirectoryNotFound,
    UnknownError
}

public class EmailServiceException(string message, EmailServiceFailureReason reason, Exception? innerException = null) : Exception(message, innerException)
{
    public EmailServiceFailureReason Reason { get; } = reason;
}