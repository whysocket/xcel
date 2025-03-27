using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;
using Xcel.Services.Email.Templates.OtpEmail;

namespace Xcel.Services.Auth.Tests.Services;

public class OtpServiceTests : AuthBaseTest
{
    private IOtpService _otpService = null!;
    private const int OtpExpirationMinutes = 5;
    private Person _person = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _otpService = new OtpService(
            EmailService,
            OtpRepository,
            FakeTimeProvider,
            CreateLogger<OtpService>());
        _person = await CreatePersonAsync();
    }

    [Fact]
    public async Task GenerateOtpAsync_ValidPerson_ShouldGenerateAndSendOtp()
    {
        // Act
        var otpResult = await _otpService.GenerateOtpAsync(_person);

        // Assert
        Assert.True(otpResult.IsSuccess);
        Assert.Equal(6, otpResult.Value.Length);

        var otpSentEmail = InMemoryEmailSender.GetSentEmail<OtpEmailData>();
        Assert.Equal(otpResult.Value, otpSentEmail.Payload.Data.OtpCode);
        Assert.Equal(
            FakeTimeProvider.GetUtcNow().AddMinutes(OtpExpirationMinutes),
            otpSentEmail.Payload.Data.Expiration);
    }

    [Fact]
    public async Task ValidateOtpAsync_ValidOtp_ShouldReturnSuccess()
    {
        // Arrange
        var otpEntity = new OtpEntity
        {
            OtpCode = "X1A3B1",
            Expiration = FakeTimeProvider.GetUtcNow().AddMinutes(OtpExpirationMinutes).UtcDateTime,
            PersonId = _person.Id,
            Id = Guid.NewGuid(),
        };

        await OtpRepository.AddAsync(otpEntity);
        await OtpRepository.SaveChangesAsync();

        // Act
        var otpResult = await _otpService.ValidateOtpAsync(_person, otpEntity.OtpCode);

        // Assert
        Assert.True(otpResult.IsSuccess);
        var alreadyUsedOtp = await OtpRepository.GetOtpByPersonIdAsync(_person.Id);
        Assert.Null(alreadyUsedOtp);
    }

    [Fact]
    public async Task ValidateOtpAsync_ExpiredOtp_ShouldReturnFailure()
    {
        // Arrange
        var otpEntity = new OtpEntity
        {
            OtpCode = "X1A3B1",
            Expiration = FakeTimeProvider.GetUtcNow().AddMinutes(-OtpExpirationMinutes).UtcDateTime,
            PersonId = _person.Id,
            Id = Guid.NewGuid(),
        };

        await OtpRepository.AddAsync(otpEntity);
        await OtpRepository.SaveChangesAsync();

        // Act
        var result = await _otpService.ValidateOtpAsync(_person, otpEntity.OtpCode);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal("OTP expired or not found.", error.Message);
    }
}