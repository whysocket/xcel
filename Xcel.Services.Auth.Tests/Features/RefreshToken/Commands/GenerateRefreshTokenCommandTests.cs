namespace Xcel.Services.Auth.Tests.Features.RefreshToken.Commands;

public class GenerateRefreshTokenCommandTests : AuthBaseTest
{
    [Fact]
    public async Task GenerateRefreshTokenAsync_WhenValidInput_ShouldGenerateToken()
    {
        // Arrange
        var person = await CreatePersonAsync();

        // Act
        var result = await GenerateRefreshTokenCommand.ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Token);
        Assert.Equal(person.Id, result.Value.PersonId);
        Assert.Equal(FakeClientInfoService.IpAddress, (string?)result.Value.CreatedByIp);
        Assert.False(result.Value.IsRevoked);
        Assert.Null(result.Value.ReplacedByToken);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
    }
}