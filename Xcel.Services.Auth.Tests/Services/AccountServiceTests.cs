using NSubstitute;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Email.Templates;

namespace Xcel.Services.Auth.Tests.Services;

public class AccountServiceTests : AuthBaseTest
{
    private Person _person = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _person = await CreatePersonAsync();
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
        Assert.Equal($"The person with email address '{nonExistentEmail}' was not found.", error.Message);
    }

    [Fact]
    public async Task RequestOtpByEmailAsync_WhenOtpGenerationFails_ShouldReturnFailure()
    {
        //Arrange
        var mockError = new Error(ErrorType.Unexpected, "Failed to generate OTP");

        var mockOtpService = Substitute.For<IOtpService>();
        mockOtpService.GenerateOtpAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<string>(mockError)));

        var accountService = new AccountService(PersonsRepository, mockOtpService);

        //Act
        var result = await accountService.RequestOtpByEmailAsync(_person.EmailAddress);

        //Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(mockError.Type, error.Type);
        Assert.Equal(mockError.Message, error.Message);
        Assert.Throws<InvalidOperationException>(() => InMemoryEmailService.GetSentEmail<OtpEmail>());
    }
}