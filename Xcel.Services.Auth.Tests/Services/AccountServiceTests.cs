using NSubstitute;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Email.Templates.WelcomeEmail;

namespace Xcel.Services.Auth.Tests.Services;

public class AccountServiceTests : BaseTest
{
    private readonly Person _person = new()
    {
        Id = Guid.NewGuid(),
        EmailAddress = "test@test.com",
        FirstName = "John",
        LastName = "Doe",
    };

    [Fact]
    public async Task CreateAccountAsync_WhenValidPerson_ShouldCreateAccountAndSendWelcomeEmail()
    {
        // Arrange
        // Act
        var result = await AccountService.CreateAccountAsync(_person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_person.Id, result.Value.Id);

        var sentEmail = InMemoryEmailSender.GetSentEmail<WelcomeEmailData>();
        Assert.Equal(_person.EmailAddress, sentEmail.Payload.To);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenPersonWithExistingEmail_ShouldReturnFailure()
    {
        // Arrange 
        await PersonsRepository.AddAsync(_person);
        await PersonsRepository.SaveChangesAsync();

        // Act
        var result = await AccountService.CreateAccountAsync(_person);

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
        var result = await AccountService.DeleteAccountAsync(_person.Id);

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
        var result = await AccountService.DeleteAccountAsync(nonExistentPersonId);

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
        var result = await AccountService.LoginWithOtpAsync(_person.EmailAddress, otp.Value);

        // Assert
        var jwtResult = JwtService.Generate(_person);

        Assert.True(result.IsSuccess);
        Assert.True(jwtResult.IsSuccess);
        Assert.Equal(jwtResult.Value, result.Value);
    }

    [Fact]
    public async Task LoginWithOtpAsync_WhenPersonDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@example.com";
        var otp = "X1ABCD1";

        // Act
        var result = await AccountService.LoginWithOtpAsync(nonExistentEmail, otp);

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
        var result = await AccountService.LoginWithOtpAsync(_person.EmailAddress, invalidOtp);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal("OTP expired or not found.", error.Message);
    }

    [Fact]
    public async Task RequestOtpByEmailAsync_WhenPersonExists_ShouldReturnSuccess()
    {
        // Arrange
        await PersonsRepository.AddAsync(_person);
        await PersonsRepository.SaveChangesAsync();

        // Act
        var result = await AccountService.RequestOtpByEmailAsync(_person.EmailAddress);

        // Assert
        Assert.True(result.IsSuccess);

        var otp = await OtpRepository.GetOtpByPersonIdAsync(_person.Id);
        Assert.NotNull(otp);
    }

    [Fact]
    public async Task RequestOtpByEmailAsync_WhenPersonDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@example.com";

        // Act
        var result = await AccountService.RequestOtpByEmailAsync(nonExistentEmail);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal($"The person with email address '{nonExistentEmail}' is not found.", error.Message);
    }

    [Fact]
    public async Task RequestOtpByEmailAsync_WhenOtpGenerationFails_ShouldReturnFailure()
    {
        //Arrange
        await PersonsRepository.AddAsync(_person);
        await PersonsRepository.SaveChangesAsync();

        var mockError = new Error(ErrorType.Unexpected, "Failed to generate OTP");

        var mockOtpService = Substitute.For<IOtpService>();
        mockOtpService.GenerateOtpAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<string>(mockError)));

        var accountService = new AccountService(PersonsRepository, EmailService, JwtService, mockOtpService);

        //Act
        var result = await accountService.RequestOtpByEmailAsync(_person.EmailAddress);

        //Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(mockError.Type, error.Type);
        Assert.Equal(mockError.Message, error.Message);
    }
}