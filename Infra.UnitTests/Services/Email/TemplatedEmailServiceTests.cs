using Domain.Payloads.Email.Shared;
using Domain.Payloads.Email.Templates;
using Infra.Interfaces.Services.Email;
using Infra.Services.Email;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net.Mail;

namespace Infra.UnitTests.Services.Email;

public class TemplatedEmailServiceTests
{
    private record InvalidTemplateData();

    private readonly IEmailSender _emailSenderSubstitute = Substitute.For<IEmailSender>();
    private readonly ILogger<TemplatedEmailService> _loggerSubstitute = Substitute.For<ILogger<TemplatedEmailService>>();
    private readonly TemplatedEmailService _emailService;

    public TemplatedEmailServiceTests()
    {
        _emailService = new TemplatedEmailService(_emailSenderSubstitute, _loggerSubstitute);
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
        var result = await _emailService.SendEmailAsync(payload);

        // Assert
        Assert.True(result);

        // Verify SendMailAsync was called exactly once with the correct arguments
        await _emailSenderSubstitute.Received(1).SendEmailAsync(
            Arg.Is<EmailPayload<WelcomeEmailData>>(p =>
                p.To == payload.To &&
                p.Subject == payload.Subject),
            Arg.Is<string>(body => body.Contains("<h1>Welcome, John Doe!</h1>")), // Verify body content
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_InvalidTemplateName_ThrowsFileNotFoundException()
    {
        // Arrange
        var payload = new EmailPayload<InvalidTemplateData>(
            "Test",
            "test@example.com",
            new());

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _emailService.SendEmailAsync(payload));
    }

    [Fact]
    public async Task SendEmailAsync_SmtpException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var payload = new EmailPayload<WelcomeEmailData>(
            "Test",
            "test@example.com",
            new("John", "Doe"));

        _emailSenderSubstitute
            .SendEmailAsync(Arg.Any<EmailPayload<WelcomeEmailData>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<SmtpException>();

        // Act
        var result = await _emailService.SendEmailAsync(payload);

        // Assert
        Assert.False(result);
    }
}
