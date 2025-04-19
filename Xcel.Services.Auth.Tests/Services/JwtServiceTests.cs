using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services;

public class JwtServiceTests : AuthBaseTest
{
    private IJwtService _jwtService = null!;
    private Person _person = null!;
    private IPersonRoleService _personRoleService = null!;
    private ILogger<JwtService> _logger = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _personRoleService = Substitute.For<IPersonRoleService>();
        _logger = CreateLogger<JwtService>();
        _person = await CreatePersonAsync();

        // Setup mock to return empty role list by default
        _personRoleService
            .GetRolesForPersonAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new List<PersonRoleEntity>()));

        _jwtService = new JwtService(
            InfraOptions.Auth,
            FakeTimeProvider,
            _personRoleService,
            _logger);
    }

    [Fact]
    public async Task GenerateAsync_WhenPersonIsValid_ShouldReturnTokenWithPersonId()
    {
        // Arrange
        // Act
        var result = await _jwtService.GenerateAsync(_person);

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
        Assert.Equal(_person.Id.ToString(), personId.Value);
    }

    [Fact]
    public async Task GenerateAsync_WhenPersonIsValid_ShouldReturnTokenWithCorrectMetadata()
    {
        // Arrange
        // Act
        var result = await _jwtService.GenerateAsync(_person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);

        Assert.Equal(AuthOptions.Jwt.Issuer, token.Issuer);
        var audience = Assert.Single(token.Audiences);
        Assert.Equal(AuthOptions.Jwt.Audience, audience);

        ValidateDateWithTolerance(
            token.ValidTo,
            FakeTimeProvider.GetUtcNow().AddMinutes(AuthOptions.Jwt.ExpireInMinutes).UtcDateTime);
    }

    [Fact]
    public async Task GenerateAsync_WhenPersonHasRoles_ShouldIncludeRolesInToken()
    {
        // Arrange
        var roles = new List<PersonRoleEntity>
        {
            new()
            {
                PersonId = _person.Id, Role = new RoleEntity
                {
                    Name = "Role 1"
                }
            },
            new()
            {
                PersonId = _person.Id, Role = new RoleEntity
                {
                    Name = "Role 2"
                }
            },
        };

        _personRoleService
            .GetRolesForPersonAsync(_person.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(roles));

        // Act
        var result = await _jwtService.GenerateAsync(_person);

        // Assert
        Assert.True(result.IsSuccess);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);

        var jwtRoleClaimType = JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[ClaimTypes.Role];
        var roleClaims = token.Claims.Where(c => c.Type == jwtRoleClaimType).ToList();

        Assert.Equal(roles.Count, roleClaims.Count);
        Assert.All(roles,
            expectedRole => Assert.Contains(roleClaims, actualClaim => actualClaim.Value == expectedRole.Role.Name));
    }

    [Fact]
    public async Task GenerateAsync_WhenRoleServiceFails_ShouldReturnFailResult()
    {
        // Arrange
        var error = new Error(ErrorType.Validation, "Failed to retrieve roles");
        _personRoleService
            .GetRolesForPersonAsync(_person.Id, Arg.Any<CancellationToken>())
            .Returns(Result<List<PersonRoleEntity>>.Fail(error));

        // Act
        var result = await _jwtService.GenerateAsync(_person);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == error.Message);
    }

    private static void ValidateDateWithTolerance(
        DateTime actualDate,
        DateTime expectedDate)
    {
        var tolerance = TimeSpan.FromSeconds(1);
        var lowerBound = expectedDate - tolerance;
        var upperBound = expectedDate + tolerance;

        Assert.InRange(actualDate, lowerBound, upperBound);
    }
}