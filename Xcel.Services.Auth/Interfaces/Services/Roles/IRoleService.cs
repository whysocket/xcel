namespace Xcel.Services.Auth.Interfaces.Services.Roles;

internal interface IRoleService :
    ICreateRoleService,
    IGetAllRolesService,
    IGetRoleByNameService,
    IUpdateRoleService,
    IDeleteRoleByNameService;