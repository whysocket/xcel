namespace Xcel.Services.Auth.Tests.Features.Authentication.Commands;

public class ExchangeRefreshTokenCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ExecuteAsync_WhenValid_ShouldReturnNewTokens()
    {
        // Arrange
        var user = await CreateUserAsync();
        FakeClientInfoService.WithUser(user);

        var refresh = await GenerateRefreshTokenCommand.ExecuteAsync(user.Id);

        // Act
        var result = await ExchangeRefreshTokenCommand.ExecuteAsync(refresh.Value.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.JwtToken);
        Assert.NotNull(result.Value.RefreshToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidToken_ShouldReturnFailure()
    {
        // Act
        var result = await ExchangeRefreshTokenCommand.ExecuteAsync("invalid-token");

        // Assert
        Assert.True(result.IsFailure);
    }
}
