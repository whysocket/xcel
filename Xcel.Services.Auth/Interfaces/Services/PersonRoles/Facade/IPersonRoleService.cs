namespace Xcel.Services.Auth.Interfaces.Services.PersonRoles.Facade;

internal interface IPersonRoleService :
    IAssignRoleToPersonService,
    IGetRolesForPersonService,
    IGetPersonRolesByRoleIdService,
    IUnassignRoleFromPersonService;