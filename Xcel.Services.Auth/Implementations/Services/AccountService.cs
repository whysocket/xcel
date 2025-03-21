using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.WelcomeEmail;

namespace Xcel.Services.Auth.Implementations.Services;

internal class AccountService(
    IPersonsRepository personRepository,
    IEmailService emailService,
    IJwtService jwtService,
    IOtpService otpService) : IAccountService
{
    public async Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default)
    {
        var existingPersonEmail = await personRepository.FindByEmailAsync(person.EmailAddress, cancellationToken);
        if (existingPersonEmail is not null)
        {
            return Result<Person>.Fail(new Error(ErrorType.Conflict,
                $"A person with the email address '{person.EmailAddress}' already exists."));
        }

        await personRepository.AddAsync(person, cancellationToken);
        await personRepository.SaveChangesAsync(cancellationToken);

        var emailPayload = new EmailPayload<WelcomeEmailData>(
            "Welcome to Our Platform!",
            person.EmailAddress,
            new WelcomeEmailData(person.FirstName, person.LastName));

        await emailService.SendEmailAsync(emailPayload, cancellationToken);

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

    public async Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var existingPerson = await personRepository.FindByEmailAsync(emailAddress, cancellationToken);
        if (existingPerson is null)
        {
            return Result.Fail(new Error(
                ErrorType.Unauthorized,
                $"The person with email address '{emailAddress}' is not found."));
        }

        var otpResult = await otpService.GenerateOtpAsync(existingPerson, cancellationToken);
        if (otpResult.IsFailure)
        {
            return Result.Fail(otpResult.Errors);
        }

        return Result.Ok();
    }

    public async Task<Result<string>> LoginWithOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var existingPerson = await personRepository.FindByEmailAsync(email, cancellationToken);
        if (existingPerson is null)
        {
            return Result.Fail<string>(new Error(
                ErrorType.Unauthorized,
                $"The person with email address '{email}' is not found."));
        }

        var existingOtpResult = await otpService.ValidateOtpAsync(
            existingPerson,
            otp,
            cancellationToken);

        if (existingOtpResult.IsFailure)
        {
            return Result.Fail<string>(existingOtpResult.Errors);
        }

        var jwtResult = jwtService.Generate(existingPerson);
        if (jwtResult.IsFailure)
        {
            return Result.Fail<string>(jwtResult.Errors);
        }

        return Result.Ok(jwtResult.Value);
    }
}