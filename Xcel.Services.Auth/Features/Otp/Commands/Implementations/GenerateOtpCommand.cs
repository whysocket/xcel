using System.Security.Cryptography;
using Domain.Entities;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Otp.Commands.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;

namespace Xcel.Services.Auth.Features.Otp.Commands.Implementations;

internal sealed class GenerateOtpCommand(
    IEmailService emailService,
    IOtpRepository otpRepository,
    TimeProvider timeProvider,
    ILogger<GenerateOtpCommand> logger) : IGenerateOtpCommand
{
    private const string ServiceName = "[GenerateOtpCommand]";
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);

    public async Task<Result<string>> ExecuteAsync(Person person, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"{ServiceName} - Generating OTP for PersonId: {person.Id}");

        await otpRepository.DeletePreviousOtpsByPersonIdAsync(person.Id, cancellationToken);

        var otpCode = GenerateSecureOtp();
        var expiration = timeProvider.GetUtcNow().Add(_otpExpiration).UtcDateTime;

        var otpEntity = new OtpEntity
        {
            OtpCode = otpCode,
            PersonId = person.Id,
            Expiration = expiration
        };

        await otpRepository.AddAsync(otpEntity, cancellationToken);
        await otpRepository.SaveChangesAsync(cancellationToken);

        var emailPayload = new EmailPayload<OtpEmail>(
            person.EmailAddress,
            new OtpEmail(otpCode, expiration, person.FullName));

        await emailService.SendEmailAsync(emailPayload, cancellationToken);

        logger.LogInformation($"{ServiceName} - OTP sent for PersonId: {person.Id}");
        return Result.Ok(otpCode);
    }

    private static string GenerateSecureOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToInt32(bytes, 0) % 1_000_000;
        return Math.Abs(number).ToString("D6");
    }
}