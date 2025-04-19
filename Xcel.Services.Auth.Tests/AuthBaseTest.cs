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

    internal ICreateRoleService CreateRoleService => GetService<ICreateRoleService>();
    internal IGetAllRolesService GetAllRolesService => GetService<IGetAllRolesService>();
    internal IGetRoleByNameService GetRoleByNameService => GetService<IGetRoleByNameService>();
    internal IUpdateRoleService UpdateRoleService => GetService<IUpdateRoleService>();
    internal IDeleteRoleByNameService DeleteRoleByNameService => GetService<IDeleteRoleByNameService>();

    #endregion

    #region PersonRole Service

    internal IAssignRoleToPersonService AssignRoleToPersonService => GetService<IAssignRoleToPersonService>();
    internal IGetRolesForPersonService GetRolesForPersonService => GetService<IGetRolesForPersonService>();
    internal IGetPersonRolesByRoleIdService GetPersonRolesByRoleIdService => GetService<IGetPersonRolesByRoleIdService>();
    internal IUnassignRoleFromPersonService UnassignRoleFromPersonService => GetService<IUnassignRoleFromPersonService>();
    internal IPersonRoleService PersonRoleService => GetService<IPersonRoleService>();

    #endregion

    #region RefreshToken Service

    internal IGenerateRefreshTokenService GenerateRefreshTokenService => GetService<IGenerateRefreshTokenService>();
    internal IValidateRefreshTokenService ValidateRefreshTokenService => GetService<IValidateRefreshTokenService>();
    internal IRevokeRefreshTokenService RevokeRefreshTokenService => GetService<IRevokeRefreshTokenService>();
    internal IRefreshTokenService RefreshTokenService => GetService<IRefreshTokenService>();

    #endregion

    #region JwtToken Service

    internal IGenerateJwtTokenService GenerateJwtTokenService => GetService<IGenerateJwtTokenService>();
    internal IJwtTokenService JwtTokenService => GetService<IJwtTokenService>();

    #endregion

    #region Otp Service

    internal IGenerateOtpService GenerateOtpService => GetService<IGenerateOtpService>();
    internal IValidateOtpService ValidateOtpService => GetService<IValidateOtpService>();
    internal IOtpTokenService OtpTokenService => GetService<IOtpTokenService>();

    #endregion
    
    #region Authentication Flow Services

    internal IRequestOtpByEmailService RequestOtpByEmailService => GetService<IRequestOtpByEmailService>();
    internal ILoginWithOtpService LoginWithOtpService => GetService<ILoginWithOtpService>();
    internal IRefreshTokenExchangeService RefreshTokenExchangeService => GetService<IRefreshTokenExchangeService>();
    internal IAuthenticationFlowService AuthenticationFlowService => GetService<IAuthenticationFlowService>();

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