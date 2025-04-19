using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;
using Xcel.Services.Auth.Interfaces.Services.Roles;

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

    #endregion
    
    internal IAuthenticationService AuthenticationService => GetService<IAuthenticationService>();
    internal IUserService UserService => GetService<IUserService>();
    internal IRolesRepository RolesRepository => GetService<IRolesRepository>();
    internal IOtpRepository OtpRepository => GetService<IOtpRepository>();
    internal IPersonRoleRepository PersonRoleRepository => GetService<IPersonRoleRepository>();
    internal IRefreshTokensRepository RefreshTokensRepository => GetService<IRefreshTokensRepository>();
    internal IOtpService OtpService => GetService<IOtpService>();
    internal IAccountService AccountService => GetService<IAccountService>();
    internal IJwtService JwtService => GetService<IJwtService>();
    internal IRefreshTokenService RefreshTokenService => GetService<IRefreshTokenService>();

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

        return person;
    }
}