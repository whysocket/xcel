using Domain.Entities;

namespace Xcel.Services.Auth.Models;

internal class RefreshTokenEntity : BaseEntity
{
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }

    public required Guid PersonId { get; set; } // Foreign key to Applicant
    public Person? Person { get; set; } // Navigation property

    public bool IsRevoked => RevokedAt.HasValue;
}