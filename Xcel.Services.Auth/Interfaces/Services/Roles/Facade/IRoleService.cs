namespace Xcel.Services.Auth.Interfaces.Services.Roles.Facade;

internal interface IRoleService :
    ICreateRoleCommand,
    IGetAllRolesQuery,
    IGetRoleByNameQuery,
    IUpdateRoleCommand,
    IDeleteRoleByNameCommand;