using Xcel.Services.Auth.Features.Authentication.Commands.Implementations;

namespace Xcel.Services.Auth.Tests.Features.Authentication.Commands;

public class LoginWithOtpCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ExecuteAsync_WhenValid_ShouldReturnAuthTokens()
    {
        // Arrange
        var person = await CreateUserAsync();
        var otpResult = await GenerateOtpCommand.ExecuteAsync(person);

        // Act
        var result = await LoginWithOtpCommand.ExecuteAsync(person.EmailAddress, otpResult.Value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.JwtToken);
        Assert.NotNull(result.Value.RefreshToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersonNotFound_ShouldReturnFailure()
    {
        // Arrange
        var email = "noone@test.com";

        // Act
        var result = await LoginWithOtpCommand.ExecuteAsync(email, "123456");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(LoginWithOtpServiceErrors.PersonNotFound(email), error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOtpInvalid_ShouldReturnFailure()
    {
        // Arrange
        var person = await CreateUserAsync();
        await GenerateOtpCommand.ExecuteAsync(person);

        // Act
        var result = await LoginWithOtpCommand.ExecuteAsync(person.EmailAddress, "wrongcode");

        // Assert
        Assert.True(result.IsFailure);
    }
}