using Domain.Entities;

namespace Xcel.Services.Auth.Models;

public class OtpEntity : BaseEntity
{
    public required Guid PersonId { get; set; }

    // Navigation Property
    public Person Person { get; set; } = null!;

    public required string OtpCode { get; set; }
    public required DateTime Expiration { get; set; }
}
