using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces;

public interface IOtpRepository : IGenericRepository<OtpEntity>
{
    Task<OtpEntity?> GetOtpByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}