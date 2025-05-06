using Domain.Entities;

namespace Xcel.Services.Auth.Models;

internal class OtpEntity : BaseEntity
{
    public required string OtpCode { get; set; }

    public required DateTime Expiration { get; set; }

    public required Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
}
