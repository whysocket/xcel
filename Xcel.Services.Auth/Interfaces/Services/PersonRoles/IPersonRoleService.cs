namespace Xcel.Services.Auth.Interfaces.Services.PersonRoles;

internal interface IPersonRoleService :
    IAssignRoleToPersonService,
    IGetRolesForPersonService,
    IGetPersonRolesByRoleIdService,
    IUnassignRoleFromPersonService;