using Xcel.Services.Auth.Models;
using Xcel.Services.Auth.Implementations.Services.RefreshTokens;

namespace Xcel.Services.Auth.Tests.Services.RefreshTokens;

public class ValidateRefreshTokenCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsValid_ShouldReturnSuccessAndToken()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var validToken = "ValidToken";
        var refreshToken = new RefreshTokenEntity
        {
            Token = validToken,
            ExpiresAt = FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime,
            PersonId = person.Id
        };
        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await ValidateRefreshTokenCommand.ValidateRefreshTokenAsync(validToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenDoesNotExist_ShouldReturnFailureUnauthorized()
    {
        // Arrange
        var nonExistentToken = "NonExistentToken";

        // Act
        var result = await ValidateRefreshTokenCommand.ValidateRefreshTokenAsync(nonExistentToken);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(ValidateRefreshTokenServiceErrors.InvalidRefreshToken(), resultError);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsRevoked_ShouldReturnFailureUnauthorized()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var revokedToken = "RevokedToken";
        var refreshToken = new RefreshTokenEntity
        {
            Token = revokedToken,
            ExpiresAt = FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime,
            PersonId = person.Id,
            RevokedAt = FakeTimeProvider.GetUtcNow().AddDays(-1).UtcDateTime
        };
        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await ValidateRefreshTokenCommand.ValidateRefreshTokenAsync(revokedToken);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(ValidateRefreshTokenServiceErrors.InvalidRefreshToken(), resultError);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsExpired_ShouldReturnFailureUnauthorized()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var expiredToken = "ExpiredToken";
        var refreshToken = new RefreshTokenEntity
        {
            Token = expiredToken,
            ExpiresAt = FakeTimeProvider.GetUtcNow().AddDays(-1).UtcDateTime,
            PersonId = person.Id
        };
        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await ValidateRefreshTokenCommand.ValidateRefreshTokenAsync(expiredToken);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(ValidateRefreshTokenServiceErrors.InvalidRefreshToken(), resultError);
    }
    
    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsReplaced_ShouldReturnFailureUnauthorized()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var replacedToken = "ReplacedToken";
        var refreshToken = new RefreshTokenEntity
        {
            Token = replacedToken,
            ExpiresAt = FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime,
            PersonId = person.Id,
            ReplacedByToken = "NewToken"
        };
        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await ValidateRefreshTokenCommand.ValidateRefreshTokenAsync(replacedToken);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(ValidateRefreshTokenServiceErrors.InvalidRefreshToken(), resultError);
    }
}