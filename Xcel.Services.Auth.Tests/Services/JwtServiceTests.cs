using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;

namespace Xcel.Services.Auth.Tests.Services;

public class JwtServiceTests : BaseTest
{
    private IJwtService _jwtService = null!;

    private readonly Person _person = new()
    {
        Id = Guid.NewGuid(),
        EmailAddress = "test@test.com",
        FirstName = "John",
        LastName = "Doe",
    };

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _jwtService = new JwtService(InfraOptions.Auth, FakeTimeProvider);
    }

    [Fact]
    public void Generate_ValidPerson_ReturnsOkResultWithToken()
    {
        // Arrange
        // Act
        var result = _jwtService.Generate(_person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(
            result.Value,
            AuthOptions.Jwt.TokenValidationParameters,
            out var validatedToken);

        Assert.NotNull(claimsPrincipal);
        Assert.True(validatedToken is JwtSecurityToken);

        var personId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(personId);
        Assert.Equal(_person.Id, Guid.Parse(personId.Value));
    }

    [Fact]
    public void Generate_TokenHasCorrectIssuerAudienceAndExpiration()
    {
        // Arrange
        // Act
        var result = _jwtService.Generate(_person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);

        Assert.Equal(AuthOptions.Jwt.Issuer, token.Issuer);
        var audience = Assert.Single(token.Audiences);
        Assert.Equal(AuthOptions.Jwt.Audience, audience);

        ValidateDateWithTolerance(
            token.ValidTo,
            FakeTimeProvider.GetUtcNow().AddMinutes(AuthOptions.Jwt.ExpireInMinutes));
    }

    private static void ValidateDateWithTolerance(
        DateTimeOffset actualDate,
        DateTimeOffset expectedDate)
    {
        var tolerance = TimeSpan.FromSeconds(1);
        var lowerBound = expectedDate - tolerance;
        var upperBound = expectedDate + tolerance;

        Assert.InRange(actualDate, lowerBound, upperBound);
    }
}