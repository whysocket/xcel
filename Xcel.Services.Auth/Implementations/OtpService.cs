using Domain.Entities;
using System.Security.Cryptography;
using Domain.Results;
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

    public async Task<Result<string>> GenerateOtpAsync(Person person, CancellationToken cancellationToken = default)
    {
        var otpEntity = new OtpEntity
        {
            OtpCode = GetGenerateSecureRandomOtp(),
            PersonId = person.Id,
            Expiration = timeProvider.GetUtcNow().Add(_otpExpiration).UtcDateTime
        };

        await otpRepository.AddAsync(otpEntity, cancellationToken);
        await otpRepository.SaveChangesAsync(cancellationToken);

        await SendOtpEmailAsync(person, otpEntity, cancellationToken);

        return Result<string>.Success(otpEntity.OtpCode);
    }

    public async Task<Result> ValidateOtpAsync(Person person, string otpCode, CancellationToken cancellationToken = default)
    {
        var existingOtpEntity = await otpRepository.GetOtpByPersonIdAsync(person.Id, cancellationToken);
        if (existingOtpEntity is null)
        {
            return Result.Failure("Invalid or expired otp code");
        }

        existingOtpEntity.IsAlreadyUsed = true;
        otpRepository.Update(existingOtpEntity);
        await otpRepository.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
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