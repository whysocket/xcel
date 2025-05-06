using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Extensiosn;
using Xcel.Services.Auth.Features.Account.Commands.Interfaces;
using Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;
using Xcel.Services.Auth.Features.PersonRoles.Commands.Interfaces;
using Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;
using Xcel.Services.Auth.Features.Roles.Commands.Interfaces;
using Xcel.Services.Auth.Features.Roles.Queries.Interfaces;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Public;

internal sealed class AuthServiceSdk(
    // Account
    ICreateAccountCommand createAccountCommand,
    IDeleteAccountCommand deleteAccountCommand,
    // Auth
    IRequestOtpByEmailCommand requestOtpByEmailCommand,
    ILoginWithOtpCommand loginWithOtpCommand,
    IExchangeRefreshTokenCommand exchangeRefreshTokenCommand,
    // Roles
    IGetRoleByNameQuery getRoleByNameQuery,
    IGetAllRolesQuery getAllRolesQuery,
    ICreateRoleCommand createRoleCommand,
    IUpdateRoleCommand updateRoleCommand,
    IDeleteRoleByNameCommand deleteRoleByNameCommand,
    IGetRolesForPersonQuery getRolesForPersonQuery,
    IGetPersonRolesByRoleIdQuery getPersonRolesByRoleIdQuery,
    IAssignRoleToPersonCommand assignRoleToPersonCommand,
    IUnassignRoleFromPersonCommand unassignRoleFromPersonCommand
) : IAuthServiceSdk
{
    #region Account

    public Task<Result<Person>> CreateAccountAsync(
        Person person,
        CancellationToken cancellationToken = default
    ) => createAccountCommand.ExecuteAsync(person, cancellationToken);

    public Task<Result> DeleteAccountAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    ) => deleteAccountCommand.ExecuteAsync(personId, cancellationToken);

    #endregion

    #region Authentication

    public Task<Result> RequestOtpByEmailAsync(
        string emailAddress,
        CancellationToken cancellationToken = default
    ) => requestOtpByEmailCommand.ExecuteAsync(emailAddress, cancellationToken);

    public Task<Result<AuthTokens>> LoginWithOtpAsync(
        string email,
        string otpCode,
        CancellationToken cancellationToken = default
    ) => loginWithOtpCommand.ExecuteAsync(email, otpCode, cancellationToken);

    public Task<Result<AuthTokens>> ExchangeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default
    ) => exchangeRefreshTokenCommand.ExecuteAsync(refreshToken, cancellationToken);

    #endregion

    #region Roles

    public async Task<Result<Role>> GetRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default
    )
    {
        var result = await getRoleByNameQuery.ExecuteAsync(roleName, cancellationToken);
        return result.IsFailure ? Result.Fail<Role>(result.Errors) : Result.Ok(result.Value.Map());
    }

    public async Task<Result<PageResult<Role>>> GetAllRolesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = await getAllRolesQuery.ExecuteAsync(
            new PageRequest(1, 100),
            cancellationToken
        );
        return result.IsFailure
            ? Result.Fail<PageResult<Role>>(result.Errors)
            : Result.Ok(result.Value.Map(r => r.Map()));
    }

    public async Task<Result<Role>> CreateRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default
    )
    {
        var result = await createRoleCommand.ExecuteAsync(roleName, cancellationToken);
        return result.IsFailure ? Result.Fail<Role>(result.Errors) : Result.Ok(result.Value.Map());
    }

    public async Task<Result> UpdateRoleAsync(
        Guid roleId,
        string roleName,
        CancellationToken cancellationToken = default
    )
    {
        var result = await updateRoleCommand.ExecuteAsync(roleId, roleName, cancellationToken);
        return result.IsFailure ? Result.Fail(result.Errors) : Result.Ok();
    }

    public async Task<Result> DeleteRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default
    )
    {
        var result = await deleteRoleByNameCommand.ExecuteAsync(roleName, cancellationToken);
        return result.IsFailure ? Result.Fail(result.Errors) : Result.Ok();
    }

    #endregion

    #region Person-Role Assignment

    public Task<Result> AddRoleToPersonAsync(
        Guid personId,
        Guid roleId,
        CancellationToken cancellationToken = default
    ) => assignRoleToPersonCommand.ExecuteAsync(personId, roleId, cancellationToken);

    public Task<Result> RemoveRoleFromPersonAsync(
        Guid personId,
        Guid roleId,
        CancellationToken cancellationToken = default
    ) => unassignRoleFromPersonCommand.ExecuteAsync(personId, roleId, cancellationToken);

    public async Task<Result<List<Role>>> GetRolesByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await getRolesForPersonQuery.ExecuteAsync(personId, cancellationToken);
        return result.IsFailure
            ? Result.Fail<List<Role>>(result.Errors)
            : Result.Ok(result.Value.Select(r => r.Role.Map()).ToList());
    }

    public async Task<Result<PageResult<Person>>> GetAllPersonsByRoleIdAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default
    )
    {
        var result = await getPersonRolesByRoleIdQuery.ExecuteAsync(
            roleId,
            pageRequest,
            cancellationToken
        );
        return result.IsFailure
            ? Result.Fail<PageResult<Person>>(result.Errors)
            : Result.Ok(result.Value.Map(pr => pr.Person));
    }

    #endregion
}
