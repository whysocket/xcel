using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;

namespace Xcel.Services.Auth.Implementations.Services;

internal sealed class UserService(
    IPersonsRepository personRepository,
    IEmailService emailService) : IUserService
{
    public async Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default)
    {
        var existingPersonEmail = await personRepository.GetByEmailAsync(person.EmailAddress, cancellationToken);
        if (existingPersonEmail is not null)
        {
            return Result<Person>.Fail(new Error(ErrorType.Conflict,
                $"A person with the email address '{person.EmailAddress}' already exists."));
        }

        await personRepository.AddAsync(person, cancellationToken);
        await personRepository.SaveChangesAsync(cancellationToken);

        var emailPayload = new EmailPayload<WelcomeEmail>(
            "Welcome to Our Platform!",
            person.EmailAddress,
            new WelcomeEmail(person.FullName));

        await emailService.SendEmailAsync(emailPayload, cancellationToken);

        return Result.Ok(person);
    }

    public async Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var existingPerson = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (existingPerson is null)
        {
            return Result.Fail(new Error(ErrorType.NotFound, $"The person with ID '{personId}' does not exist."));
        }

        existingPerson.IsDeleted = true;

        personRepository.Update(existingPerson);
        await personRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
