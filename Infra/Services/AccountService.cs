using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Xcel.Services.Interfaces;
using Xcel.Services.Models;
using Xcel.Services.Templates.WelcomeEmail;

namespace Infra.Services;

public class AccountService(
    IPersonsRepository personRepository,
    IEmailService emailService) : IAccountService
{
    public async Task<Person> CreateAccountAsync(Person person, CancellationToken cancellationToken = default)
    {
        await personRepository.AddAsync(person, cancellationToken);
        await personRepository.SaveChangesAsync(cancellationToken);

        var emailPayload = new EmailPayload<WelcomeEmailData>(
            "Welcome to Our Platform!",
            person.EmailAddress,
            new WelcomeEmailData(
                person.FirstName,
                person.LastName));

        await emailService.SendEmailAsync(emailPayload, cancellationToken);

        return person;
    }

    public async Task<bool> CheckAccountExistanceByEmailAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        return await personRepository.ExistsAsync(p => p.EmailAddress == emailAddress, cancellationToken);
    }
}
