using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Repositories;

internal interface IOtpRepository : IGenericRepository<OtpEntity>
{
    Task<OtpEntity?> GetOtpByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task DeletePreviousOtpsByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}