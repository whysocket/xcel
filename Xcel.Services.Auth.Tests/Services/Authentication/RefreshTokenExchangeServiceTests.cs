namespace Xcel.Services.Auth.Tests.Services.Authentication;

public class RefreshTokenExchangeServiceTests : AuthBaseTest
{
    [Fact]
    public async Task RefreshTokenAsync_WhenValid_ShouldReturnNewTokens()
    {
        // Arrange
        var person = await CreatePersonAsync();
        FakeClientInfoService.WithPerson(person);

        var refresh = await RefreshTokenService.GenerateRefreshTokenAsync();

        // Act
        var result = await RefreshTokenExchangeService.RefreshTokenAsync(refresh.Value.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.JwtToken);
        Assert.NotNull(result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenInvalidToken_ShouldReturnFailure()
    {
        // Act
        var result = await RefreshTokenExchangeService.RefreshTokenAsync("invalid-token");

        // Assert
        Assert.True(result.IsFailure);
    }
}