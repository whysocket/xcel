using Xcel.Services.Auth.Features.RefreshTokens.Commands.Implementations;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.RefreshToken.Commands;

public class RevokeRefreshTokenCommandTests : AuthBaseTest
{
    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenExists_ShouldRevokeToken()
    {
        // Arrange
        var person = await CreateUserAsync();
        var refreshToken = new RefreshTokenEntity
        {
            Token = "TestToken",
            PersonId = person.Id
        };
        await RefreshTokensRepository.AddAsync(refreshToken);
        await RefreshTokensRepository.SaveChangesAsync();

        // Act
        var result = await RevokeRefreshTokenCommand.ExecuteAsync("TestToken");

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
        var result = await RevokeRefreshTokenCommand.ExecuteAsync(nonExistentToken);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(RevokeRefreshTokenServiceErrors.RefreshTokenNotFound(), resultError);
    }
}