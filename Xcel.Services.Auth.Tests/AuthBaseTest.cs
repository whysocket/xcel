using Xcel.Services.Auth.Features.Account.Commands.Interfaces;
using Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;
using Xcel.Services.Auth.Features.Jwt.Commands.Interfaces;
using Xcel.Services.Auth.Features.Otp.Commands.Interfaces;
using Xcel.Services.Auth.Features.PersonRoles.Commands.Interfaces;
using Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;
using Xcel.Services.Auth.Features.Roles.Commands.Interfaces;
using Xcel.Services.Auth.Features.Roles.Queries.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;

namespace Xcel.Services.Auth.Tests;

public class AuthBaseTest : BaseTest
{
    #region Role

    internal IGetAllRolesQuery GetAllRolesQuery => GetService<IGetAllRolesQuery>();
    internal IGetRoleByNameQuery GetRoleByNameQuery => GetService<IGetRoleByNameQuery>();
    internal ICreateRoleCommand CreateRoleCommand => GetService<ICreateRoleCommand>();
    internal IUpdateRoleCommand UpdateRoleCommand => GetService<IUpdateRoleCommand>();
    internal IDeleteRoleByNameCommand DeleteRoleByNameCommand => GetService<IDeleteRoleByNameCommand>();

    #endregion

    #region PersonRole

    internal IGetRolesForPersonQuery GetRolesForPersonQuery => GetService<IGetRolesForPersonQuery>();
    internal IGetPersonRolesByRoleIdQuery GetPersonRolesByRoleIdQuery => GetService<IGetPersonRolesByRoleIdQuery>();
    internal IAssignRoleToPersonCommand AssignRoleToPersonCommand => GetService<IAssignRoleToPersonCommand>();
    internal IUnassignRoleFromPersonCommand UnassignRoleFromPersonCommand => GetService<IUnassignRoleFromPersonCommand>();

    #endregion

    #region RefreshToken

    internal IGenerateRefreshTokenCommand GenerateRefreshTokenCommand => GetService<IGenerateRefreshTokenCommand>();
    internal IValidateRefreshTokenCommand ValidateRefreshTokenCommand => GetService<IValidateRefreshTokenCommand>();
    internal IRevokeRefreshTokenCommand RevokeRefreshTokenCommand => GetService<IRevokeRefreshTokenCommand>();

    #endregion

    #region Jwt

    internal IGenerateJwtTokenCommand GenerateJwtTokenCommand => GetService<IGenerateJwtTokenCommand>();

    #endregion

    #region Otp

    internal IGenerateOtpCommand GenerateOtpCommand => GetService<IGenerateOtpCommand>();
    internal IValidateOtpCommand ValidateOtpCommand => GetService<IValidateOtpCommand>();

    #endregion
    
    #region Authentication

    internal IRequestOtpByEmailCommand RequestOtpByEmailCommand => GetService<IRequestOtpByEmailCommand>();
    internal ILoginWithOtpCommand LoginWithOtpCommand => GetService<ILoginWithOtpCommand>();
    internal IExchangeRefreshTokenCommand ExchangeRefreshTokenCommand => GetService<IExchangeRefreshTokenCommand>();

    #endregion

    #region Account

    internal ICreateAccountCommand CreateAccountCommand => GetService<ICreateAccountCommand>();
    internal IDeleteAccountCommand DeleteAccountCommand => GetService<IDeleteAccountCommand>();

    #endregion
    
    internal IRolesRepository RolesRepository => GetService<IRolesRepository>();
    internal IOtpRepository OtpRepository => GetService<IOtpRepository>();
    internal IPersonRoleRepository PersonRoleRepository => GetService<IPersonRoleRepository>();
    internal IRefreshTokensRepository RefreshTokensRepository => GetService<IRefreshTokensRepository>();

    protected async Task<Person> CreateUserAsync()
    {
        var random = new Random().Next(1, 1000);
        var user = new Person
        {
            EmailAddress = $"test{random}@test.com",
            FirstName = "John",
            LastName = "Doe",
        };

        await PersonsRepository.AddAsync(user);
        await PersonsRepository.SaveChangesAsync();

        FakeClientInfoService.WithUser(user);

        return user;
    }
}