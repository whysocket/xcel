using Xcel.Services.Auth.Implementations.Services.Authentication;

namespace Xcel.Services.Auth.Tests.Services.Authentication;

public class RequestOtpByEmailServiceTests : AuthBaseTest
{
    [Fact]
    public async Task RequestOtpByEmailAsync_WhenPersonExists_ShouldSendOtp()
    {
        // Arrange
        var person = await CreatePersonAsync();

        // Act
        var result = await RequestOtpByEmailService.RequestOtpByEmailAsync(person.EmailAddress);

        // Assert
        Assert.True(result.IsSuccess);
        var otp = await OtpRepository.GetOtpByPersonIdAsync(person.Id);
        Assert.NotNull(otp);
    }

    [Fact]
    public async Task RequestOtpByEmailAsync_WhenPersonNotFound_ShouldFail()
    {
        // Arrange
        var unknownEmail = "unknown@email.com";

        // Act
        var result = await RequestOtpByEmailService.RequestOtpByEmailAsync(unknownEmail);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RequestOtpByEmailServiceErrors.PersonNotFound(unknownEmail), error);
    }
}