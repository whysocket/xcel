using Xcel.Services.Auth.Implementations;
using Xcel.Services.Auth.Interfaces;
using Xcel.Services.Email.Templates.WelcomeEmail;

namespace Xcel.Services.Auth.Tests;

public class AccountServiceIntegrationTests : BaseTest
{
    private IAccountService _accountService = null!;
    private readonly Person _person = new()
    {
        Id = Guid.NewGuid(),
        EmailAddress = "test@test.com",
        FirstName = "John",
        LastName = "Doe",
    };

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

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
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(error.Message, $"A person with the email address '{_person.EmailAddress}' already exists.");

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
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.NotFound, error.Type);

        var personFromDb = await PersonsRepository.GetByIdAsync(nonExistentPersonId);
        Assert.Null(personFromDb);
    }
    
    [Fact]
    public async Task LoginWithOtpAsync_WhenPersonExistsAndOtpIsValid_ShouldReturnSuccess()
    {
        // Arrange
        await PersonsRepository.AddAsync(_person);
        await PersonsRepository.SaveChangesAsync();

        var otp = await OtpService.GenerateOtpAsync(_person);
        await OtpRepository.SaveChangesAsync();

        // Act
        var result = await _accountService.LoginWithOtpAsync(_person.EmailAddress, otp.Value);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginWithOtpAsync_WhenPersonDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@example.com";
        var otp = "X1ABCD1";

        // Act
        var result = await _accountService.LoginWithOtpAsync(nonExistentEmail, otp);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal($"The person with email address '{nonExistentEmail}' is not found.", error.Message);
    }

    [Fact]
    public async Task LoginWithOtpAsync_WhenOtpIsInvalid_ShouldReturnFailure()
    {
        // Arrange
        await PersonsRepository.AddAsync(_person);
        await PersonsRepository.SaveChangesAsync();

        await OtpService.GenerateOtpAsync(_person);
        await OtpRepository.SaveChangesAsync();
        var invalidOtp = "X1ABCD1";

        // Act
        var result = await _accountService.LoginWithOtpAsync(_person.EmailAddress, invalidOtp);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal("Invalid or expired OTP code.", error.Message);
    }
}