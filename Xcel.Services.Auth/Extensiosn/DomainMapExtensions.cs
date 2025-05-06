using Domain.Entities;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Extensiosn;

internal static class DomainMapExtensions
{
    internal static Role Map(this RoleEntity role)
    {
        return new() { Id = role.Id, Name = role.Name };
    }

    internal static IEnumerable<Role> Map(this IEnumerable<RoleEntity> roles) =>
        roles.Select(r => r.Map());

    internal static RoleEntity Map(this Role role)
    {
        return new() { Id = role.Id, Name = role.Name };
    }

    internal static IEnumerable<RoleEntity> Map(this IEnumerable<Role> roles) =>
        roles.Select(r => r.Map());
}
