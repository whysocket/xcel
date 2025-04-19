using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IAuthService
{
    Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);

    Task<Result<Role>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken);

    Task<Result<PageResult<Person>>> GetAllPersonsByRoleIdAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<Result<PageResult<Role>>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<Result<Role>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<Result> UpdateRoleAsync(Guid roleId, string roleName, CancellationToken cancellationToken = default);
    Task<Result> AddRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);

    Task<Result<List<Role>>> GetRolesByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken);
    Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otpCode, CancellationToken cancellationToken);
    Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}

internal class AuthService(
    IUserService userService,
    IRoleService roleService,
    IAccountService accountService,
    IAuthenticationService authenticationService,
    IPersonRoleService personRoleService) : IAuthService
{
    public Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default)
        => userService.CreateAccountAsync(person, cancellationToken);

    public Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken = default)
        => userService.DeleteAccountAsync(personId, cancellationToken);

    public Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default)
        => accountService.RequestOtpByEmailAsync(emailAddress, cancellationToken);

    public async Task<Result<PageResult<Person>>> GetAllPersonsByRoleIdAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var result = await personRoleService.GetAllPersonsRolesByRoleIdAsync(
            roleId,
            pageRequest,
            cancellationToken);

        return result.IsFailure
            ? Result.Fail<PageResult<Person>>(result.Errors)
            : Result.Ok(result.Value.Map(pr => pr.Person));
    }

    public async Task<Result<Role>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken)
    {
        var result = await roleService.GetRoleByNameAsync(roleName, cancellationToken);

        return result.IsFailure
            ? Result.Fail<Role>(result.Errors)
            : Result.Ok(result.Value.Map());
    }

    public async Task<Result<PageResult<Role>>> GetAllRolesAsync(CancellationToken cancellationToken)
    {
        var result = await roleService.GetAllRolesAsync(new(1, 100), cancellationToken);

        return result.IsFailure
            ? Result.Fail<PageResult<Role>>(result.Errors)
            : Result.Ok(result.Value.Map(re => re.Map()));
    }

    public async Task<Result<Role>> CreateRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var result = await roleService.CreateRoleAsync(roleName, cancellationToken);

        return result.IsFailure
            ? Result.Fail<Role>(result.Errors)
            : Result.Ok(result.Value.Map());
    }

    public async Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken)
    {
        var result = await roleService.DeleteRoleByNameAsync(roleName, cancellationToken);

        return result.IsFailure
            ? Result.Fail(result.Errors)
            : Result.Ok();
    }

    public async Task<Result> UpdateRoleAsync(Guid roleId, string roleName, CancellationToken cancellationToken)
    {
        var result = await roleService.UpdateRoleAsync(roleId, roleName, cancellationToken);
   
        return result.IsFailure
            ? Result.Fail(result.Errors)
            : Result.Ok();
    }

    public Task<Result> AddRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken)
        => personRoleService.AddRoleToPersonAsync(personId, roleId, cancellationToken);

    public async Task<Result<List<Role>>> GetRolesByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var result = await personRoleService.GetRolesByPersonIdAsync(personId, cancellationToken);

        return result.IsFailure
            ? Result.Fail<List<Role>>(result.Errors)
            : Result.Ok(result.Value.Select(pr => pr.Role.Map()).ToList());
    }

    public Task<Result> RemoveRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken)
        => personRoleService.RemoveRoleFromPersonAsync(personId, roleId, cancellationToken);

    public Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otpCode, CancellationToken cancellationToken)
        => authenticationService.LoginWithOtpAsync(email, otpCode, cancellationToken);

    public Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        => authenticationService.RefreshTokenAsync(refreshToken, cancellationToken);
}

internal static class DomainMapExtensions
{
    internal static Role Map(this RoleEntity role)
    {
        return new()
        {
            Id = role.Id,
            Name = role.Name,
        };
    }

    internal static IEnumerable<Role> Map(this IEnumerable<RoleEntity> roles)
        => roles.Select(r => r.Map());

    internal static RoleEntity Map(this Role role)
    {
        return new()
        {
            Id = role.Id,
            Name = role.Name,
        };
    }

    internal static IEnumerable<RoleEntity> Map(this IEnumerable<Role> roles)
        => roles.Select(r => r.Map());
}