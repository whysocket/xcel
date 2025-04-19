using Xcel.Services.Auth.Implementations.Services.Authentication;

namespace Xcel.Services.Auth.Tests.Services.Authentication;

public class LoginWithOtpServiceTests : AuthBaseTest
{
    [Fact]
    public async Task LoginWithOtpAsync_WhenValid_ShouldReturnAuthTokens()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var otpResult = await GenerateOtpService.GenerateOtpAsync(person);

        // Act
        var result = await LoginWithOtpService.LoginWithOtpAsync(person.EmailAddress, otpResult.Value);

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
        var result = await LoginWithOtpService.LoginWithOtpAsync(email, "123456");

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
        await GenerateOtpService.GenerateOtpAsync(person);

        // Act
        var result = await LoginWithOtpService.LoginWithOtpAsync(person.EmailAddress, "wrongcode");

        // Assert
        Assert.True(result.IsFailure);
    }
}