using Xcel.Services.Auth.Implementations.Services.Otp;

namespace Xcel.Services.Auth.Tests.Services.Otp;

public class ValidateOtpCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ValidateOtpAsync_WhenOtpIsCorrect_ShouldSucceed()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var generatedOtp = await GenerateOtpCommand.GenerateOtpAsync(person);

        // Act
        var result = await ValidateOtpCommand.ValidateOtpAsync(person, generatedOtp.Value);

        // Assert
        Assert.True(result.IsSuccess);

        var storedOtp = await OtpRepository.GetOtpByPersonIdAsync(person.Id);
        Assert.Null(storedOtp); // OTP should be deleted after validation
    }

    [Fact]
    public async Task ValidateOtpAsync_WhenOtpIsWrong_ShouldFail()
    {
        // Arrange
        var person = await CreatePersonAsync();
        await GenerateOtpCommand.GenerateOtpAsync(person);

        // Act
        var result = await ValidateOtpCommand.ValidateOtpAsync(person, "000000");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidateOtpServiceErrors.InvalidOrExpiredOtp(), error);
    }

    [Fact]
    public async Task ValidateOtpAsync_WhenOtpIsMissing_ShouldFail()
    {
        // Arrange
        var person = await CreatePersonAsync();

        // Act
        var result = await ValidateOtpCommand.ValidateOtpAsync(person, "123456");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidateOtpServiceErrors.InvalidOrExpiredOtp(), error);
    }
}