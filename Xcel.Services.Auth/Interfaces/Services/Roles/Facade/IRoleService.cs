namespace Xcel.Services.Auth.Interfaces.Services.Roles.Facade;

internal interface IRoleService :
    ICreateRoleService,
    IGetAllRolesService,
    IGetRoleByNameService,
    IUpdateRoleService,
    IDeleteRoleByNameService;