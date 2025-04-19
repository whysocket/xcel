using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Account.Commands.Interfaces;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;

namespace Xcel.Services.Auth.Features.Account.Commands.Implementations;

internal static class CreateAccountCommandErrors
{
    internal static Error EmailAlreadyExists(string email) =>
        new(ErrorType.Conflict, $"A person with the email address '{email}' already exists.");
}

internal sealed class CreateAccountCommand(
    IPersonsRepository personRepository,
    IEmailService emailService,
    ILogger<CreateAccountCommand> logger) : ICreateAccountCommand
{
    private const string ServiceName = "[CreateAccountCommand]";

    public async Task<Result<Person>> ExecuteAsync(Person person, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"{ServiceName} - Checking if person already exists: {person.EmailAddress}");

        var existing = await personRepository.GetByEmailAsync(person.EmailAddress, cancellationToken);
        if (existing is not null)
        {
            logger.LogWarning($"{ServiceName} - Conflict: Person already exists with email {person.EmailAddress}");
            return Result.Fail<Person>(CreateAccountCommandErrors.EmailAlreadyExists(person.EmailAddress));
        }

        await personRepository.AddAsync(person, cancellationToken);
        await personRepository.SaveChangesAsync(cancellationToken);

        var payload = new EmailPayload<WelcomeEmail>(
            person.EmailAddress,
            new WelcomeEmail(person.FullName));

        await emailService.SendEmailAsync(payload, cancellationToken);

        logger.LogInformation($"{ServiceName} - Person account created and welcome email sent.");
        return Result.Ok(person);
    }
}