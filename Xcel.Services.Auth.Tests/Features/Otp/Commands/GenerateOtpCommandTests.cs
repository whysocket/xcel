namespace Xcel.Services.Auth.Tests.Features.Otp.Commands;

public class GenerateOtpCommandTests : AuthBaseTest
{
    [Fact]
    public async Task GenerateOtpAsync_ShouldGenerateAndStoreOtp()
    {
        // Arrange
        var person = await CreatePersonAsync();

        // Act
        var result = await GenerateOtpCommand.ExecuteAsync(person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Matches(@"^\d{6}$", result.Value); // OTP is a 6-digit number

        var storedOtp = await OtpRepository.GetOtpByPersonIdAsync(person.Id);
        Assert.NotNull(storedOtp);
        Assert.Equal((string?)result.Value, storedOtp.OtpCode);
        Assert.True(storedOtp.Expiration > FakeTimeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task GenerateOtpAsync_ShouldReplacePreviousOtp()
    {
        // Arrange
        var person = await CreatePersonAsync();

        var firstOtp = await GenerateOtpCommand.ExecuteAsync(person);
        var secondOtp = await GenerateOtpCommand.ExecuteAsync(person);

        // Act
        var storedOtp = await OtpRepository.GetOtpByPersonIdAsync(person.Id);

        // Assert
        Assert.NotNull(storedOtp);
        Assert.Equal((string?)secondOtp.Value, storedOtp.OtpCode);
        Assert.NotEqual(firstOtp.Value, secondOtp.Value);
    }
}