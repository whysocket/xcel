using Domain.Entities;

namespace Xcel.Services.Auth.Models;

public class RoleEntity : BaseEntity
{
    public required string Name { get; set; }
}