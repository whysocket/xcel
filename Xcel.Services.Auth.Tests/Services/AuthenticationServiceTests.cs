using NSubstitute;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;
using Xcel.Services.Auth.Models;
using Xcel.Services.Email.Templates;

namespace Xcel.Services.Auth.Tests.Services;

public class AuthenticationServiceTests : AuthBaseTest
{
    private Person _person = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        _person = await CreatePersonAsync();
    }

    [Fact]
    public async Task LoginWithOtpAsync_WhenPersonExistsAndOtpIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var otpResult = await OtpService.GenerateOtpAsync(_person);
        
        // Act
        var result = await AuthenticationService.LoginWithOtpAsync(_person.EmailAddress, otpResult.Value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        var expectedJwtResult = await JwtService.GenerateAsync(_person);
        Assert.Equal(expectedJwtResult.Value, result.Value.JwtToken);
        
        var expectedRefreshTokenResult = await RefreshTokensRepository.GetByTokenAsync(result.Value.RefreshToken);
        Assert.NotNull(expectedRefreshTokenResult);
        Assert.Equal(expectedRefreshTokenResult.Token, result.Value.RefreshToken);
        
        var sentEmail = InMemoryEmailService.GetSentEmail<OtpEmail>();
        Assert.Equal(_person.EmailAddress, sentEmail.Payload.To.First());
    }
    
    [Fact]
    public async Task LoginWithOtpAsync_WhenPersonDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        // Act
        var result = await AuthenticationService.LoginWithOtpAsync("nonexistent@example.com", "otp");
    
        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal("The person with email address 'nonexistent@example.com' was not found.", error.Message);
    }
    
    [Fact]
    public async Task LoginWithOtpAsync_WhenOtpIsInvalid_ShouldReturnFailure()
    {
        // Arrange
        var invalidOtpCode = "3FXA1C";

        // Act
        var result = await AuthenticationService.LoginWithOtpAsync(_person.EmailAddress, invalidOtpCode);
    
        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal("OTP expired or not found.", error.Message);
    }
    
    [Fact]
    public async Task RefreshTokenAsync_WhenRefreshTokenIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var refreshTokenResult = await RefreshTokenService.GenerateRefreshTokenAsync(_person);
        
        // Act
        var result = await AuthenticationService.RefreshTokenAsync(refreshTokenResult.Value.Token);
    
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        var expectedJwtResult = await JwtService.GenerateAsync(_person);
        Assert.Equal(expectedJwtResult.Value, result.Value.JwtToken);
        
        var expectedRefreshTokenResult = await RefreshTokensRepository.GetByTokenAsync(result.Value.RefreshToken);
        Assert.NotNull(expectedRefreshTokenResult);
        Assert.Equal(expectedRefreshTokenResult.Token, result.Value.RefreshToken);
    }
    
    
    [Fact]
    public async Task RefreshTokenAsync_WhenRefreshTokenIsInvalid_ShouldReturnFailure()
    {
        // Arrange
        var refreshTokenResult = await RefreshTokenService.GenerateRefreshTokenAsync(_person);
        
        refreshTokenResult.Value.RevokedAt = FakeTimeProvider.GetUtcNow().AddDays(-1).UtcDateTime;
        RefreshTokensRepository.Update(refreshTokenResult.Value);
        await RefreshTokensRepository.SaveChangesAsync();
        
        // Act
        var result = await AuthenticationService.RefreshTokenAsync(refreshTokenResult.Value.Token);
    
        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal("Invalid refresh token.", error.Message);
    }
    
    [Fact]
    public async Task RefreshTokenAsync_WhenPersonDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var refreshTokenResult = await RefreshTokenService.GenerateRefreshTokenAsync(_person);

        await UserService.DeleteAccountAsync(_person.Id);

        // Act
        var result = await AuthenticationService.RefreshTokenAsync(refreshTokenResult.Value.Token);
    
        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("The person associated with this token was not found.", error.Message);
    }
    
    [Fact]
    public async Task RefreshTokenAsync_WhenJwtGenerationFails_ShouldReturnFailure()
    {
        // Arrange
        var refreshTokenResult = await RefreshTokenService.GenerateRefreshTokenAsync(_person);

        var mockJwtService = Substitute.For<IJwtService>();
        mockJwtService.GenerateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<string>(new Error(ErrorType.Unexpected, "JWT generation failed"))));
    
        var authService = new AuthenticationService(
            PersonsRepository, 
            mockJwtService,
            RefreshTokenService, 
            ClientInfoService,
            OtpService);
    
        // Act
        var result = await authService.RefreshTokenAsync(refreshTokenResult.Value.Token);
    
        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unexpected, error.Type);
        Assert.Equal("JWT generation failed", error.Message);
    }
    
    [Fact]
    public async Task RefreshTokenAsync_WhenRefreshTokenGenerationFails_ShouldReturnFailure()
    {
        // Arrange
        var mockRefreshTokenService = Substitute.For<IRefreshTokenService>();
        mockRefreshTokenService.ValidateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(new RefreshTokenEntity
            {
                Token = "invalidToken",
                PersonId = _person.Id
            })));
        
        mockRefreshTokenService.GenerateRefreshTokenAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<RefreshTokenEntity>(new Error(ErrorType.Unexpected, "RefreshToken generation failed"))));
    
        var authService = new AuthenticationService(
            PersonsRepository,
            JwtService,
            mockRefreshTokenService,
            ClientInfoService,
            OtpService);
    
        // Act
        var result = await authService.RefreshTokenAsync("validRefreshToken");
    
        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unexpected, error.Type);
        Assert.Equal("RefreshToken generation failed", error.Message);
    }
}