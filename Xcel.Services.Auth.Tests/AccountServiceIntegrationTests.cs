using Domain.Entities;
using Xcel.Services.Auth.Implementations;
using Xcel.Services.Auth.Interfaces;
using Xcel.Services.Email.Templates.WelcomeEmail;
using Xcel.TestUtils;

namespace Xcel.Services.Auth.Tests;

public class AccountServiceIntegrationTests : BaseTest
{
    private readonly IAccountService _accountService;
    private readonly Person _person = new()
    {
        Id = Guid.NewGuid(),
        EmailAddress = "test@test.com",
        FirstName = "John",
        LastName = "Doe",
    };

    public AccountServiceIntegrationTests()
    {
        _accountService = new AccountService(PersonsRepository, EmailService, OtpService);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenValidPerson_ShouldCreateAccountAndSendWelcomeEmailAndGenerateOtp()
    {
        // Arrange
        // Act
        var result = await _accountService.CreateAccountAsync(_person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_person.Id, result.Value.Id);

        var sentEmail = InMemoryEmailSender.GetSentEmail<WelcomeEmailData>();
        Assert.Equal(_person.EmailAddress, sentEmail.Payload.To);

        var otp = await OtpRepository.GetOtpByPersonIdAsync(_person.Id);
        Assert.NotNull(otp);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenPersonWithExistingEmail_ShouldReturnFailure()
    {
        // Arrange 
        await PersonsRepository.AddAsync(_person);
        await PersonsRepository.SaveChangesAsync();
        
        // Act
        var result = await _accountService.CreateAccountAsync(_person);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal($"A person with the {_person.EmailAddress} already exists", result.ErrorMessage);

        Assert.Throws<InvalidOperationException>(() => InMemoryEmailSender.GetSentEmail<WelcomeEmailData>());

        var otp = await OtpRepository.GetOtpByPersonIdAsync(_person.Id);
        Assert.Null(otp);
    }
    
    [Fact]
    public async Task DeleteAccountAsync_WhenPersonExists_ShouldMarkPersonAsDeleted()
    {
        // Arrange
        await PersonsRepository.AddAsync(_person);
        await PersonsRepository.SaveChangesAsync();

        // Act
        var result = await _accountService.DeleteAccountAsync(_person.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var deletedPerson = await PersonsRepository.GetByIdAsync(_person.Id);
        Assert.NotNull(deletedPerson);
        Assert.True(deletedPerson.IsDeleted);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenPersonDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var nonExistentPersonId = Guid.NewGuid();

        // Act
        var result = await _accountService.DeleteAccountAsync(nonExistentPersonId);

        // Assert
        Assert.True(result.IsFailure);

        var personFromDb = await PersonsRepository.GetByIdAsync(nonExistentPersonId);
        Assert.Null(personFromDb);
    }
}