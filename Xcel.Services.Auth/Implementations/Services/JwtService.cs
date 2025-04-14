using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Entities;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Options;

namespace Xcel.Services.Auth.Implementations.Services;

internal sealed class JwtService(
    AuthOptions authOptions,
    TimeProvider timeProvider,
    IPersonRoleService personRoleService,
    ILogger<JwtService> logger) : IJwtService
{
    private readonly JwtOptions _jwtOptions = authOptions.Jwt;

    public async Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, person.Id.ToString())
        };

        logger.LogInformation("Generating JWT for person {ApplicantId}", person.Id);

        var rolesResult = await personRoleService.GetRolesForPersonAsync(person.Id, cancellationToken);

        if (rolesResult.IsSuccess)
        {
            foreach (var role in rolesResult.Value)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            logger.LogDebug("Roles added to JWT for person {ApplicantId}: {Roles}", person.Id, string.Join(", ", rolesResult.Value.Select(r => r.Name)));
        }
        else
        {
            logger.LogError("Error retrieving roles for person {ApplicantId}: {Error}", person.Id, string.Join(", ", rolesResult.Errors.Select(e => e.Message)));
            
            return Result<string>.Fail(rolesResult.Errors);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = timeProvider.GetUtcNow().AddMinutes(_jwtOptions.ExpireInMinutes).UtcDateTime,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new(
                new SymmetricSecurityKey(_jwtOptions.SecretKeyEncoded),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        logger.LogDebug("JWT generated: {Token}", tokenString);

        return Result.Ok(tokenString);
    }
}