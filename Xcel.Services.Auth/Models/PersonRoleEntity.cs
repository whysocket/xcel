using Domain.Entities;

namespace Xcel.Services.Auth.Models;

public class PersonRoleEntity : BaseEntity
{
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public Guid RoleId { get; set; }
    public RoleEntity Role { get; set; } = null!;
}
