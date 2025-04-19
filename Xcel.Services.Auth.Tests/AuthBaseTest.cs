using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.Authentication;
using Xcel.Services.Auth.Interfaces.Services.Authentication.Facade;
using Xcel.Services.Auth.Interfaces.Services.Jwt;
using Xcel.Services.Auth.Interfaces.Services.Jwt.Facade;
using Xcel.Services.Auth.Interfaces.Services.Otp;
using Xcel.Services.Auth.Interfaces.Services.Otp.Facade;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles.Facade;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;

namespace Xcel.Services.Auth.Tests;

public class AuthBaseTest : BaseTest
{
    #region Role Service

    internal ICreateRoleCommand CreateRoleCommand => GetService<ICreateRoleCommand>();
    internal IGetAllRolesQuery GetAllRolesQuery => GetService<IGetAllRolesQuery>();
    internal IGetRoleByNameQuery GetRoleByNameQuery => GetService<IGetRoleByNameQuery>();
    internal IUpdateRoleCommand UpdateRoleCommand => GetService<IUpdateRoleCommand>();
    internal IDeleteRoleByNameCommand DeleteRoleByNameCommand => GetService<IDeleteRoleByNameCommand>();

    #endregion

    #region PersonRole Service

    internal IAssignRoleToPersonCommand AssignRoleToPersonCommand => GetService<IAssignRoleToPersonCommand>();
    internal IGetRolesForPersonQuery GetRolesForPersonQuery => GetService<IGetRolesForPersonQuery>();
    internal IGetPersonRolesByRoleIdQuery GetPersonRolesByRoleIdQuery => GetService<IGetPersonRolesByRoleIdQuery>();
    internal IUnassignRoleFromPersonCommand UnassignRoleFromPersonCommand => GetService<IUnassignRoleFromPersonCommand>();
    internal IPersonRoleService PersonRoleService => GetService<IPersonRoleService>();

    #endregion

    #region RefreshToken Service

    internal IGenerateRefreshTokenCommand GenerateRefreshTokenCommand => GetService<IGenerateRefreshTokenCommand>();
    internal IValidateRefreshTokenCommand ValidateRefreshTokenCommand => GetService<IValidateRefreshTokenCommand>();
    internal IRevokeRefreshTokenCommand RevokeRefreshTokenCommand => GetService<IRevokeRefreshTokenCommand>();
    internal IRefreshTokenService RefreshTokenService => GetService<IRefreshTokenService>();

    #endregion

    #region JwtToken Service

    internal IGenerateJwtTokenCommand GenerateJwtTokenCommand => GetService<IGenerateJwtTokenCommand>();
    internal IJwtTokenService JwtTokenService => GetService<IJwtTokenService>();

    #endregion

    #region Otp Service

    internal IGenerateOtpCommand GenerateOtpCommand => GetService<IGenerateOtpCommand>();
    internal IValidateOtpCommand ValidateOtpCommand => GetService<IValidateOtpCommand>();
    internal IOtpTokenService OtpTokenService => GetService<IOtpTokenService>();

    #endregion
    
    #region Authentication Flow Services

    internal IRequestOtpByEmailCommand RequestOtpByEmailCommand => GetService<IRequestOtpByEmailCommand>();
    internal ILoginWithOtpCommand LoginWithOtpCommand => GetService<ILoginWithOtpCommand>();
    internal IRefreshTokenExchangeCommand RefreshTokenExchangeCommand => GetService<IRefreshTokenExchangeCommand>();
    internal IAuthenticationService AuthenticationService => GetService<IAuthenticationService>();

    #endregion

    internal IUserService UserService => GetService<IUserService>();
    internal IRolesRepository RolesRepository => GetService<IRolesRepository>();
    internal IOtpRepository OtpRepository => GetService<IOtpRepository>();
    internal IPersonRoleRepository PersonRoleRepository => GetService<IPersonRoleRepository>();
    internal IRefreshTokensRepository RefreshTokensRepository => GetService<IRefreshTokensRepository>();

    protected async Task<Person> CreatePersonAsync()
    {
        var random = new Random().Next(1, 1000);
        var person = new Person
        {
            EmailAddress = $"test{random}@test.com",
            FirstName = "John",
            LastName = "Doe",
        };

        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        FakeClientInfoService.WithPerson(person);

        return person;
    }
}