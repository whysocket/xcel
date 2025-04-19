using Xcel.Services.Auth.Implementations.Services.Authentication;

namespace Xcel.Services.Auth.Tests.Services.Authentication;

public class LoginWithOtpCommandTests : AuthBaseTest
{
    [Fact]
    public async Task LoginWithOtpAsync_WhenValid_ShouldReturnAuthTokens()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var otpResult = await GenerateOtpCommand.GenerateOtpAsync(person);

        // Act
        var result = await LoginWithOtpCommand.LoginWithOtpAsync(person.EmailAddress, otpResult.Value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.JwtToken);
        Assert.NotNull(result.Value.RefreshToken);
    }

    [Fact]
    public async Task LoginWithOtpAsync_WhenPersonNotFound_ShouldReturnFailure()
    {
        // Arrange
        var email = "noone@test.com";

        // Act
        var result = await LoginWithOtpCommand.LoginWithOtpAsync(email, "123456");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(LoginWithOtpServiceErrors.PersonNotFound(email), error);
    }

    [Fact]
    public async Task LoginWithOtpAsync_WhenOtpInvalid_ShouldReturnFailure()
    {
        // Arrange
        var person = await CreatePersonAsync();
        await GenerateOtpCommand.GenerateOtpAsync(person);

        // Act
        var result = await LoginWithOtpCommand.LoginWithOtpAsync(person.EmailAddress, "wrongcode");

        // Assert
        Assert.True(result.IsFailure);
    }
}