using Xcel.Services.Auth.Features.Otp.Commands.Implementations;

namespace Xcel.Services.Auth.Tests.Features.Otp.Commands;

public class ValidateOtpCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ValidateOtpAsync_WhenOtpIsCorrect_ShouldSucceed()
    {
        // Arrange
        var person = await CreateUserAsync();
        var generatedOtp = await GenerateOtpCommand.ExecuteAsync(person);

        // Act
        var result = await ValidateOtpCommand.ExecuteAsync(person, generatedOtp.Value);

        // Assert
        Assert.True(result.IsSuccess);

        var storedOtp = await OtpRepository.GetOtpByPersonIdAsync(person.Id);
        Assert.Null(storedOtp); // OTP should be deleted after validation
    }

    [Fact]
    public async Task ValidateOtpAsync_WhenOtpIsWrong_ShouldFail()
    {
        // Arrange
        var person = await CreateUserAsync();
        await GenerateOtpCommand.ExecuteAsync(person);

        // Act
        var result = await ValidateOtpCommand.ExecuteAsync(person, "000000");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidateOtpServiceErrors.InvalidOrExpiredOtp(), error);
    }

    [Fact]
    public async Task ValidateOtpAsync_WhenOtpIsMissing_ShouldFail()
    {
        // Arrange
        var person = await CreateUserAsync();

        // Act
        var result = await ValidateOtpCommand.ExecuteAsync(person, "123456");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidateOtpServiceErrors.InvalidOrExpiredOtp(), error);
    }
}