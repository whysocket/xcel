using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using Xcel.Services.Email.Tests.Mocks;
using Xcel.Services.Exceptions;
using Xcel.Services.Implementations;
using Xcel.Services.Models;
using Xcel.Services.Templates.WelcomeEmail;

namespace Xcel.Services.Email.Tests;

public class TemplatedEmailServiceTests
{
    private record InvalidTemplateData();

    private readonly ILogger<TemplatedEmailService> _loggerSubstitute;
    private readonly InMemoryEmailSender _inMemoryEmailSender = new();
    private readonly TemplatedEmailService _emailService;

    public TemplatedEmailServiceTests()
    {
        _loggerSubstitute = Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplatedEmailService>.Instance;
        _emailService = new TemplatedEmailService(_inMemoryEmailSender, _loggerSubstitute);
    }

    [Fact]
    public async Task SendEmailAsync_ValidPayload_SendsEmailWithCorrectContent()
    {
        // Arrange
        var payload = new EmailPayload<WelcomeEmailData>(
            "Welcome to Our Platform!",
            "test@example.com",
            new("John", "Doe"));

        // Act
        await _emailService.SendEmailAsync(payload);

        // Assert
        var sentEmails = _inMemoryEmailSender.GetSentEmails();
        Assert.Single(sentEmails);
        var sentEmail = sentEmails.First();
        Assert.Equal(payload, sentEmail.Payload);
        Assert.Contains("<h1>Welcome, John Doe!</h1>", sentEmail.Body);
    }

    [Fact]
    public async Task SendEmailAsync_InvalidTemplateDirectory_ThrowsEmailServiceExceptionWithDirectoryNotFoundReason()
    {
        // Arrange
        var payload = new EmailPayload<InvalidTemplateData>(
            "Test",
            "test@example.com",
            new());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailServiceException>(() => _emailService.SendEmailAsync(payload));
        Assert.Equal(EmailServiceFailureReason.TemplateDirectoryNotFound, exception.Reason);
        Assert.IsType<DirectoryNotFoundException>(exception.InnerException);
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