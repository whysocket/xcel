using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Results;
using Xcel.Services.Auth.Interfaces;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.WelcomeEmail;

namespace Xcel.Services.Auth.Implementations;

internal class AccountService(
    IPersonsRepository personRepository,
    IEmailService emailService,
    IOtpService otpService) : IAccountService
{
    public async Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default)
    {
        var existingPersonEmail = await personRepository.FindByEmailAsync(person.EmailAddress, cancellationToken);
        if (existingPersonEmail is not null)
        {
            return Result<Person>.Fail(new Error(ErrorType.Conflict, $"A person with the email address '{person.EmailAddress}' already exists."));
        }

        await personRepository.AddAsync(person, cancellationToken);
        await personRepository.SaveChangesAsync(cancellationToken);

        await SendNewPersonEmailAsync(person, cancellationToken);

        await otpService.GenerateOtpAsync(person, cancellationToken);

        return Result.Ok(person);
    }

    public async Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken)
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

    private async Task SendNewPersonEmailAsync(Person person, CancellationToken cancellationToken)
    {
        var emailPayload = new EmailPayload<WelcomeEmailData>(
            "Welcome to Our Platform!",
            person.EmailAddress,
            new WelcomeEmailData(
                person.FirstName,
                person.LastName));

        await emailService.SendEmailAsync(emailPayload, cancellationToken);
    }
}