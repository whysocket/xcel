using Xcel.Services.Auth.Models;
using Xcel.Services.Auth.Implementations.Services.RefreshTokens;

namespace Xcel.Services.Auth.Tests.Services.RefreshTokens;

public class RevokeRefreshTokenCommandTests : AuthBaseTest
{
    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenExists_ShouldRevokeToken()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var refreshToken = new RefreshTokenEntity
        {
            Token = "TestToken",
            PersonId = person.Id
        };
        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await RevokeRefreshTokenCommand.RevokeRefreshTokenAsync("TestToken");

        // Assert
        Assert.True(result.IsSuccess);

        var revokedToken = await RefreshTokensRepository.GetByTokenAsync("TestToken");
        Assert.NotNull(revokedToken);
        Assert.NotNull(revokedToken.RevokedAt);
        Assert.Equal(FakeClientInfoService.IpAddress, revokedToken.RevokedByIp);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenDoesNotExist_ShouldReturnFailureNotFound()
    {
        // Arrange
        var nonExistentToken = "NonExistentToken";

        // Act
        var result = await RevokeRefreshTokenCommand.RevokeRefreshTokenAsync(nonExistentToken);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(RevokeRefreshTokenServiceErrors.RefreshTokenNotFound(), resultError);
    }
}