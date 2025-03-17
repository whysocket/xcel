using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Xcel.Services.Auth.Interfaces;

public class OtpEntity : BaseEntity
{
    public required Guid PersonId { get; set; }

    // Navigation Property
    public Person Person { get; set; } = null!;

    public required string OtpCode { get; set; }
    public required DateTime Expiration { get; set; }

    public bool IsAlreadyUsed { get; set; }
}

public interface IOtpRepository : IGenericRepository<OtpEntity>
{
    Task<OtpEntity?> GetOtpByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}