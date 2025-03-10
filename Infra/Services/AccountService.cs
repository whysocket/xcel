using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Payloads;

namespace Infra.Services;

public class AccountService(
    IPersonsRepository personRepository,
    IEmailService emailService) : IAccountService
{
    public async Task<Person> CreateAccountAsync(Person person, CancellationToken cancellationToken = default)
    {
        await personRepository.AddAsync(person, cancellationToken);
        await personRepository.SaveChangesAsync(cancellationToken);

        var emailPayload = new EmailPayload<WelcomeEmailTemplateData>(
            To: person.EmailAddress,
            Subject: "Welcome to Our Platform!",
            TemplateData: new WelcomeEmailTemplateData
            {
                FirstName = person.FirstName,
                LastName = person.LastName
            });

        await emailService.SendEmailAsync(emailPayload);

        return person;
    }

    public async Task<bool> CheckAccountExistanceByEmailAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        return await personRepository.ExistsAsync(p => p.EmailAddress == emailAddress, cancellationToken);
    }
}
