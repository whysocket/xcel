using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Entities;
using Domain.Results;
using Microsoft.IdentityModel.Tokens;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Options;

namespace Xcel.Services.Auth.Implementations.Services;

public class JwtService(
    AuthOptions authOptions) : IJwtService
{
    private readonly JwtOptions _jwtOptions = authOptions.Jwt;

    public Result<string> Generate(Person person)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, person.Id.ToString())
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_jwtOptions.SecretKeyEncoded),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Result.Ok(tokenString);
    }
}