using Microsoft.Extensions.Logging;
using System.Net.Mail;
using HandlebarsDotNet;
using Xcel.Services.Email.Tests.Mocks;
using Xcel.Services.Email.Exceptions;
using Xcel.Services.Email.Implementations;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.WelcomeEmail;

namespace Xcel.Services.Email.Tests;

public class TemplatedEmailServiceExceptionTests
{
    private record InvalidTemplateData;

    private readonly ILogger<TemplatedEmailService> _loggerSubstitute;
    private readonly InMemoryEmailSender _inMemoryEmailSender = new();
    private readonly TemplatedEmailService _emailService;

    public TemplatedEmailServiceExceptionTests()
    {
        _loggerSubstitute = Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplatedEmailService>.Instance;
        _emailService = new TemplatedEmailService(_inMemoryEmailSender, _loggerSubstitute);
    }

    [Fact]
    public async Task SendEmailAsync_InvalidTemplateFile_ThrowsEmailServiceExceptionWithFileNotFoundReason()
    {
        // Arrange
        var payload = new EmailPayload<InvalidTemplateData>(
            "Test",
            "test@example.com",
            new());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailServiceException>(() => _emailService.SendEmailAsync(payload));
        Assert.Equal(EmailServiceFailureReason.TemplateFileNotFound, exception.Reason);
        Assert.IsType<FileNotFoundException>(exception.InnerException);
    }

    [Fact]
    public async Task SendEmailAsync_SmtpException_ThrowsEmailServiceExceptionWithSmtpConnectionFailedReason()
    {
        // Arrange
        var payload = new EmailPayload<WelcomeEmailData>(
            "Test",
            "test@example.com",
            new("John", "Doe"));

        _inMemoryEmailSender.Simulation = InMemoryEmailSender.SimulationType.SmtpException;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailServiceException>(() => _emailService.SendEmailAsync(payload));
        Assert.Equal(EmailServiceFailureReason.SmtpConnectionFailed, exception.Reason);
        Assert.IsType<SmtpException>(exception.InnerException);
    }

    [Fact]
    public async Task SendEmailAsync_HandlebarsCompilerException_ThrowsEmailServiceExceptionWithTemplateCompilationErrorReason()
    {
        // Arrange
        var payload = new EmailPayload<WelcomeEmailData>(
            "Test",
            "test@example.com",
            new("John", "Doe"));

        _inMemoryEmailSender.Simulation = InMemoryEmailSender.SimulationType.HandlebarsCompilerException;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailServiceException>(() => _emailService.SendEmailAsync(payload));
        Assert.Equal(EmailServiceFailureReason.TemplateCompilationError, exception.Reason);
        Assert.IsType<HandlebarsCompilerException>(exception.InnerException);
    }

    [Fact]
    public async Task SendEmailAsync_GenericException_ThrowsEmailServiceExceptionWithUnknownErrorReason()
    {
        // Arrange
        var payload = new EmailPayload<WelcomeEmailData>(
            "Test",
            "test@example.com",
            new("John", "Doe"));

        _inMemoryEmailSender.Simulation = InMemoryEmailSender.SimulationType.GenericException;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailServiceException>(() => _emailService.SendEmailAsync(payload));
        Assert.Equal(EmailServiceFailureReason.UnknownError, exception.Reason);
        Assert.IsType<Exception>(exception.InnerException);
    }
}