using Domain.Entities;
using System.Security.Cryptography;
using Xcel.Services.Auth.Interfaces;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.OtpEmail;

namespace Xcel.Services.Auth.Implementations;

internal class OtpService(
    IEmailService emailService,
    IOtpRepository otpRepository,
    TimeProvider timeProvider) : IOtpService
{
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);

    public async Task<string> GenerateOtpAsync(Person person, CancellationToken cancellationToken = default)
    {
        var otpEntity = new OtpEntity
        {
            OtpCode = GetGenerateSecureRandomOtp(),
            PersonId = person.Id,
            Expiration = timeProvider.GetUtcNow().Add(_otpExpiration).UtcDateTime
        };

        await otpRepository.UpsertOtpAsync(otpEntity, cancellationToken);

        await SendOtpEmailAsync(person, otpEntity, cancellationToken);

        return otpEntity.OtpCode;
    }

    private async Task SendOtpEmailAsync(Person person, OtpEntity otpEntity, CancellationToken cancellationToken = default)
    {
        var emailPayload = new EmailPayload<OtpEmailData>(
            "Your One-Time Password",
            person.EmailAddress,
            new OtpEmailData(
                otpEntity.OtpCode,
                otpEntity.Expiration,
                person.FullName));

        await emailService.SendEmailAsync(emailPayload, cancellationToken);
    }

    private static string GetGenerateSecureRandomOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[4];
        rng.GetBytes(bytes);
        int number = BitConverter.ToInt32(bytes, 0) % 1000000;
        return Math.Abs(number).ToString("D6");
    }
}