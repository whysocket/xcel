using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Public;

public interface IAuthServiceSdk
{
    #region Account

    Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken = default);

    #endregion

    #region Authentication

    Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> LoginWithOtpAsync(string emailAddress, string otpCode, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> ExchangeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    #endregion

    #region Roles

    Task<Result<Role>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<Result<PageResult<Role>>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<Result<Role>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<Result> UpdateRoleAsync(Guid roleId, string roleName, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);

    #endregion

    #region Person-Role Assignments

    Task<Result> AddRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result> RemoveRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<List<Role>>> GetRolesByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<Result<PageResult<Person>>> GetAllPersonsByRoleIdAsync(Guid roleId, PageRequest pageRequest, CancellationToken cancellationToken = default);

    #endregion
}