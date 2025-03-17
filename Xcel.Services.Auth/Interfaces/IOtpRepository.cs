using Domain.Entities;

namespace Xcel.Services.Auth.Interfaces;

public class OtpEntity : BaseEntity
{
    public required Guid PersonId { get; set; }

    // Navigation Property
    public Person Person { get; set; } = null!;

    public required string OtpCode { get; set; }
    public required DateTime Expiration { get; set; }
}

public interface IOtpRepository
{
    Task<string?> GetOtpByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task UpsertOtpAsync(OtpEntity otpEntity, CancellationToken cancellationToken = default);
}