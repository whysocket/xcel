using Domain.Entities;

namespace Xcel.Services.Auth.Models;

internal class RoleEntity : BaseEntity
{
    public required string Name { get; set; }

    public ICollection<PersonRoleEntity> PersonRoles { get; set; } = [];
}