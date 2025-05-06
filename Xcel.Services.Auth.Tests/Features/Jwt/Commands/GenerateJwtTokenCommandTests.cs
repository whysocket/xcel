using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NSubstitute;
using Xcel.Services.Auth.Features.Jwt.Commands.Implementations;
using Xcel.Services.Auth.Features.Jwt.Commands.Interfaces;
using Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.Jwt.Commands;

public class GenerateJwtTokenCommandTests : AuthBaseTest
{
    private IGenerateJwtTokenCommand _command = null!;
    private IGetRolesForPersonQuery _personRoleService = null!;
    private Person _person = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Arrange
        _person = await CreateUserAsync();
        _personRoleService = Substitute.For<IGetRolesForPersonQuery>();

        _personRoleService
            .ExecuteAsync(_person.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new List<PersonRoleEntity>()));

        _command = new GenerateJwtTokenCommand(
            InfraOptions.Auth,
            FakeTimeProvider,
            _personRoleService,
            CreateLogger<GenerateJwtTokenCommand>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersonIsValid_ShouldIncludePersonIdInClaims()
    {
        // Act
        var result = await _command.ExecuteAsync(_person);

        // Assert
        Assert.True(result.IsSuccess);

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            result.Value,
            AuthOptions.Jwt.TokenValidationParameters,
            out var validatedToken
        );

        Assert.NotNull(principal);
        Assert.True(validatedToken is JwtSecurityToken);

        var personIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(personIdClaim);
        Assert.Equal(_person.Id.ToString(), personIdClaim.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersonIsValid_ShouldIncludeJwtMetadata()
    {
        // Act
        var result = await _command.ExecuteAsync(_person);

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);

        Assert.Equal(AuthOptions.Jwt.Issuer, token.Issuer);
        Assert.Equal(AuthOptions.Jwt.Audience, Assert.Single(token.Audiences));

        ValidateDateWithTolerance(
            token.ValidTo,
            FakeTimeProvider.GetUtcNow().AddMinutes(AuthOptions.Jwt.ExpireInMinutes).UtcDateTime
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersonHasRoles_ShouldIncludeRoleClaims()
    {
        // Arrange
        var roles = new List<PersonRoleEntity>
        {
            new()
            {
                PersonId = _person.Id,
                Role = new RoleEntity { Name = "Admin" },
            },
            new()
            {
                PersonId = _person.Id,
                Role = new RoleEntity { Name = "User" },
            },
        };

        _personRoleService
            .ExecuteAsync(_person.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(roles));

        // Act
        var result = await _command.ExecuteAsync(_person);

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);
        var roleClaimType = JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[ClaimTypes.Role];

        var roleClaims = token.Claims.Where(c => c.Type == roleClaimType).ToList();

        Assert.Equal(roles.Count, roleClaims.Count);
        foreach (var role in roles)
        {
            Assert.Contains(roleClaims, c => c.Value == role.Role.Name);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleServiceFails_ShouldReturnFailure()
    {
        // Arrange
        var mockError = new Error(ErrorType.Validation, "Role fetch failed");

        _personRoleService
            .ExecuteAsync(_person.Id, Arg.Any<CancellationToken>())
            .Returns(Result<List<PersonRoleEntity>>.Fail(mockError));

        // Act
        var result = await _command.ExecuteAsync(_person);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message == mockError.Message);
    }

    private static void ValidateDateWithTolerance(DateTime actual, DateTime expected)
    {
        var tolerance = TimeSpan.FromSeconds(1);
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }
}
