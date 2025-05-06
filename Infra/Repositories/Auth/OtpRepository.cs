using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories.Auth;

internal class OtpRepository(AppDbContext dbContext, TimeProvider timeProvider)
    : GenericRepository<OtpEntity>(dbContext),
        IOtpRepository
{
    /// <summary>
    /// Retrieves an unexpired and unused OTP entity associated with a specific person ID.
    /// </summary>
    /// <param name="personId">The unique identifier of the person associated with the OTP.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// An <see cref="OtpEntity"/> if a valid, unexpired, and unused OTP is found for the given person ID; otherwise, <c>null</c>.
    /// </returns>
    public Task<OtpEntity?> GetOtpByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        var utcNow = timeProvider.GetUtcNow();

        return DbContext
            .Set<OtpEntity>()
            .Where(otp => otp.PersonId == personId && otp.Expiration > utcNow)
            .OrderByDescending(otp => otp.Expiration)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task DeletePreviousOtpsByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        return RemoveByAsync(otp => otp.PersonId == personId, cancellationToken);
    }
}
