using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;
using Xcel.Services.Auth.Features.Otp.Commands.Interfaces;

namespace Xcel.Services.Auth.Features.Authentication.Commands.Implementations;

internal static class RequestOtpByEmailServiceErrors
{
    internal static Error PersonNotFound(string email) =>
        new(ErrorType.Unauthorized, $"The person with email address '{email}' was not found.");
}

internal sealed class RequestOtpByEmailCommand(
    IPersonsRepository personRepository,
    IGenerateOtpCommand generateOtpCommand,
    ILogger<RequestOtpByEmailCommand> logger
) : IRequestOtpByEmailCommand
{
    private const string ServiceName = "[RequestOtpByEmailCommand]";

    public async Task<Result> ExecuteAsync(
        string emailAddress,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation($"{ServiceName} - Looking up person by email: {emailAddress}");

        var existingPerson = await personRepository.GetByEmailAsync(
            emailAddress,
            cancellationToken
        );
        if (existingPerson is null)
        {
            logger.LogWarning($"{ServiceName} - Person not found: {emailAddress}");
            return Result.Fail(RequestOtpByEmailServiceErrors.PersonNotFound(emailAddress));
        }

        var result = await generateOtpCommand.ExecuteAsync(existingPerson, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - OTP generation failed: {emailAddress}");
            return Result.Fail(result.Errors);
        }

        logger.LogInformation($"{ServiceName} - OTP successfully sent to: {emailAddress}");
        return Result.Ok();
    }
}
