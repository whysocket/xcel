using Xcel.Services.Auth.Features.Authentication.Commands.Implementations;

namespace Xcel.Services.Auth.Tests.Features.Authentication.Commands;

public class RequestOtpByEmailCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ExecuteAsync_WhenPersonExists_ShouldSendOtp()
    {
        // Arrange
        var person = await CreateUserAsync();

        // Act
        var result = await RequestOtpByEmailCommand.ExecuteAsync(person.EmailAddress);

        // Assert
        Assert.True(result.IsSuccess);
        var otp = await OtpRepository.GetOtpByPersonIdAsync(person.Id);
        Assert.NotNull(otp);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersonNotFound_ShouldFail()
    {
        // Arrange
        var unknownEmail = "unknown@email.com";

        // Act
        var result = await RequestOtpByEmailCommand.ExecuteAsync(unknownEmail);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RequestOtpByEmailServiceErrors.PersonNotFound(unknownEmail), error);
    }
}
