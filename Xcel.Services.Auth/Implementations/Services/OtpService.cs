using System.Security.Cryptography;
using Domain.Entities;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.OtpEmail;

namespace Xcel.Services.Auth.Implementations.Services;

internal class OtpService(
    IEmailService emailService,
    IOtpRepository otpRepository,
    TimeProvider timeProvider,
    ILogger<OtpService> logger) : IOtpService
{
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);

    public async Task<Result<string>> GenerateOtpAsync(Person person, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"[OTP Service] Generating OTP for PersonId: {person.Id}");

        await otpRepository.DeletePreviousOtpsByPersonIdAsync(person.Id, cancellationToken);

        var otpCode = GetGenerateSecureRandomOtp();
        var expiration = timeProvider.GetUtcNow().Add(_otpExpiration).UtcDateTime;

        var otpEntity = new OtpEntity
        {
            OtpCode = otpCode,
            PersonId = person.Id,
            Expiration = expiration
        };

        await otpRepository.AddAsync(otpEntity, cancellationToken);
        await otpRepository.SaveChangesAsync(cancellationToken);

        await SendOtpEmailAsync(person, otpEntity, cancellationToken);

        logger.LogInformation($"[OTP Service] OTP generated and sent successfully for PersonId: {person.Id}");

        return Result.Ok(otpEntity.OtpCode);
    }

    public async Task<Result> ValidateOtpAsync(Person person, string otpCode, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"[OTP Service] Validating OTP for PersonId: {person.Id}");

        var existingOtpEntity = await otpRepository.GetOtpByPersonIdAsync(person.Id, cancellationToken);
        if (existingOtpEntity is null)
        {
            logger.LogWarning($"[OTP Service] OTP expired or not found for PersonId: {person.Id}");
            return Result.Fail(new Error(ErrorType.Unauthorized, "OTP expired or not found."));
        }

        if (!existingOtpEntity.OtpCode.Equals(otpCode))
        {
            logger.LogWarning($"[OTP Service] Invalid OTP code for PersonId: {person.Id}");
            return Result.Fail(new Error(ErrorType.Unauthorized, "OTP expired or not found."));
        }

        await otpRepository.DeletePreviousOtpsByPersonIdAsync(person.Id, cancellationToken);
        await otpRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"[OTP Service] OTP validated successfully for PersonId: {person.Id}");

        return Result.Ok();
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
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToInt32(bytes, 0) % 1000000;
        return Math.Abs(number).ToString("D6");
    }
}