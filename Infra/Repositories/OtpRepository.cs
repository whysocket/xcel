using Infra.Repositories.Shared;
using Xcel.Services.Auth.Interfaces;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories;

internal class OtpRepository(
    AppDbContext dbContext,
    TimeProvider timeProvider) : GenericRepository<OtpEntity>(dbContext), IOtpRepository
{
    /// <summary>
    /// Retrieves an unexpired and unused OTP entity associated with a specific person ID.
    /// </summary>
    /// <param name="personId">The unique identifier of the person associated with the OTP.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// An <see cref="OtpEntity"/> if a valid, unexpired, and unused OTP is found for the given person ID; otherwise, <c>null</c>.
    /// </returns>
    public async Task<OtpEntity?> GetOtpByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow();
        var otpEntity = await GetByAsync(
            otp => otp.PersonId == personId
                   && otp.Expiration > utcNow
                   && otp.IsAlreadyUsed == false,
            cancellationToken);

        return otpEntity;
    }
}