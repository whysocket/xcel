using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services;

public class RefreshTokenServiceTests : AuthBaseTest
{
    private RefreshTokenService _refreshTokenService = null!;
    private Person _person = null!;
    private const string IpAddress = "192.168.1.1";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _refreshTokenService = new RefreshTokenService(FakeTimeProvider, RefreshTokensRepository);
        _person = await CreatePersonAsync();
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldGenerateAndStoreToken()
    {
        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync(_person, IpAddress);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Token);
        Assert.Equal(_person.Id, result.Value.PersonId);
        Assert.Equal(IpAddress, result.Value.CreatedByIp);

        // Verify the token is in the database
        var tokenFromDb = await RefreshTokensRepository.GetByTokenAsync(result.Value.Token);
        Assert.NotNull(tokenFromDb);
        Assert.Equal(result.Value.Token, tokenFromDb.Token);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsValid_ShouldReturnToken()
    {
        // Arrange
        var refreshToken = new RefreshTokenEntity
        {
            Token = "validToken",
            ExpiresAt = FakeTimeProvider.GetUtcNow().AddDays(7).UtcDateTime,
            PersonId = _person.Id,
            CreatedByIp = IpAddress
        };

        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync("validToken", IpAddress);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("validToken", result.Value.Token);

        //Verify that the token was updated.
        var updatedToken = await RefreshTokensRepository.GetByTokenAsync("validToken");
        Assert.NotNull(updatedToken);
        Assert.NotNull(updatedToken.ReplacedByToken);
        Assert.NotNull(updatedToken.RevokedAt);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsExpired_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = new RefreshTokenEntity
        {
            Token = "expiredToken",
            ExpiresAt = FakeTimeProvider.GetUtcNow().AddDays(-1).UtcDateTime,
            PersonId = _person.Id,
            CreatedByIp = IpAddress
        };

        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync("expiredToken", IpAddress);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Type == ErrorType.Unauthorized);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsRevoked_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = new RefreshTokenEntity
        {
            Token = "revokedToken",
            ExpiresAt = FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime,
            RevokedAt = FakeTimeProvider.GetUtcNow().UtcDateTime,
            PersonId = _person.Id,
            CreatedByIp = IpAddress
        };

        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync("revokedToken", IpAddress);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Type == ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldRevokeToken()
    {
        // Arrange
        var refreshToken = new RefreshTokenEntity { Token = "tokenToRevoke", PersonId = _person.Id };
        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync("tokenToRevoke", IpAddress);

        // Assert
        Assert.True(result.IsSuccess);

        //Verify that the token was revoked.
        var revokedToken = await RefreshTokensRepository.GetByTokenAsync("tokenToRevoke");
        Assert.NotNull(revokedToken);
        Assert.NotNull(revokedToken.RevokedAt);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync("nonExistentToken", IpAddress);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Type == ErrorType.NotFound);
    }
}