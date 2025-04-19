namespace Xcel.Services.Auth.Interfaces.Services.PersonRoles.Facade;

internal interface IPersonRoleService :
    IAssignRoleToPersonCommand,
    IGetRolesForPersonQuery,
    IGetPersonRolesByRoleIdQuery,
    IUnassignRoleFromPersonCommand;