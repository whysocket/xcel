using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Entities;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xcel.Services.Auth.Interfaces.Services.Jwt;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles.Facade;
using Xcel.Services.Auth.Options;

namespace Xcel.Services.Auth.Implementations.Services.Jwt;

internal sealed class GenerateJwtTokenService(
    AuthOptions authOptions,
    TimeProvider timeProvider,
    IPersonRoleService personRoleService,
    ILogger<GenerateJwtTokenService> logger) : IGenerateJwtTokenService
{
    private const string ServiceName = "[GenerateJwtTokenService]";
    private readonly JwtOptions _jwtOptions = authOptions.Jwt;

    public async Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"{ServiceName} - Generating JWT for PersonId: {person.Id}");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, person.Id.ToString())
        };

        var rolesResult = await personRoleService.GetRolesForPersonAsync(person.Id, cancellationToken);

        if (rolesResult.IsSuccess)
        {
            foreach (var role in rolesResult.Value)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Role.Name));
            }

            logger.LogDebug($"{ServiceName} - Roles added for PersonId: {person.Id} - [{string.Join(", ", rolesResult.Value.Select(r => r.Role.Name))}]");
        }
        else
        {
            logger.LogError($"{ServiceName} - Failed to retrieve roles for PersonId: {person.Id}. Errors: [{string.Join(", ", rolesResult.Errors.Select(e => e.Message))}]");
            return Result<string>.Fail(rolesResult.Errors);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = timeProvider.GetUtcNow().AddMinutes(_jwtOptions.ExpireInMinutes).UtcDateTime,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_jwtOptions.SecretKeyEncoded),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        logger.LogDebug($"{ServiceName} - JWT successfully generated for PersonId: {person.Id}");

        return Result.Ok(tokenString);
    }
}