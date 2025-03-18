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
            return Result<Person>.Failure($"A person with the {person.EmailAddress} already exists");
        }

        await personRepository.AddAsync(person, cancellationToken);
        await personRepository.SaveChangesAsync(cancellationToken);

        await SendNewPersonEmailAsync(person, cancellationToken);

        await otpService.GenerateOtpAsync(person, cancellationToken);

        return Result<Person>.Success(person);
    }

    public async Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken)
    {
        var existingPerson = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (existingPerson is null)
        {
            return Result.Failure($"The person with id {personId} does not exist");
        }

        existingPerson.IsDeleted = true;

        personRepository.Update(existingPerson);
        await personRepository.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
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