using Infra.Repositories.Shared;
using Xcel.Services.Auth.Interfaces;

namespace Infra.Repositories;

public class OtpRepository(
    AppDbContext dbContext,
    TimeProvider timeProvider) : GenericRepository<OtpEntity>(dbContext), IOtpRepository
{
    /// <summary>
    /// Retrieves the OTP for a given user ID, provided it has not expired.
    /// </summary>
    /// <param name="personId">The user ID associated with the OTP.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The OTP string if found and not expired, otherwise null.</returns>
    public async Task<string?> GetOtpByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow();
        var otpEntity = await GetByAsync(otp => otp.PersonId == personId && otp.Expiration > utcNow, cancellationToken);
        return otpEntity?.OtpCode;
    }

    public async Task UpsertOtpAsync(OtpEntity otpEntity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(otpEntity);

        var existingOtpEntity = await GetByAsync(o => o.PersonId == otpEntity.PersonId, cancellationToken);
        if (existingOtpEntity == null)
        {
            await AddAsync(otpEntity, cancellationToken);
        }
        else
        {
            existingOtpEntity.OtpCode = otpEntity.OtpCode;
            existingOtpEntity.Expiration = otpEntity.Expiration;

            Update(existingOtpEntity);
        }

        await SaveChangesAsync(cancellationToken);
    }
}