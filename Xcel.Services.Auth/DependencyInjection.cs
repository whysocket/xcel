using Microsoft.Extensions.DependencyInjection;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Implementations.Services.Authentication;
using Xcel.Services.Auth.Implementations.Services.Authentication.Facade;
using Xcel.Services.Auth.Implementations.Services.Jwt;
using Xcel.Services.Auth.Implementations.Services.Jwt.Facade;
using Xcel.Services.Auth.Implementations.Services.Otp;
using Xcel.Services.Auth.Implementations.Services.Otp.Facade;
using Xcel.Services.Auth.Implementations.Services.PersonRoles;
using Xcel.Services.Auth.Implementations.Services.PersonRoles.Facade;
using Xcel.Services.Auth.Implementations.Services.RefreshTokens;
using Xcel.Services.Auth.Implementations.Services.RefreshTokens.Facade;
using Xcel.Services.Auth.Implementations.Services.Roles;
using Xcel.Services.Auth.Implementations.Services.Roles.Facade;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.Authentication;
using Xcel.Services.Auth.Interfaces.Services.Authentication.Facade;
using Xcel.Services.Auth.Interfaces.Services.Jwt;
using Xcel.Services.Auth.Interfaces.Services.Jwt.Facade;
using Xcel.Services.Auth.Interfaces.Services.Otp;
using Xcel.Services.Auth.Interfaces.Services.Otp.Facade;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles.Facade;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Interfaces.Services.Roles.Facade;

namespace Xcel.Services.Auth;

internal static class DependencyInjection
{
    internal static IServiceCollection AddXcelAuthServices<TOtpRepository, TPersonsRepository, TRolesRepository,
        TPersonRoleRepository, TRefreshTokensRepository>(
        this IServiceCollection services,
        AuthOptions authOptions)
        where TOtpRepository : class, IOtpRepository
        where TPersonsRepository : class, IPersonsRepository
        where TRolesRepository : class, IRolesRepository
        where TPersonRoleRepository : class, IPersonRoleRepository
        where TRefreshTokensRepository : class, IRefreshTokensRepository
    {
        services.TryAddScoped<IOtpRepository, TOtpRepository>();
        services.TryAddScoped<IPersonsRepository, TPersonsRepository>();
        services.TryAddScoped<IRolesRepository, TRolesRepository>();
        services.TryAddScoped<IPersonRoleRepository, TPersonRoleRepository>();
        services.TryAddScoped<IRefreshTokensRepository, TRefreshTokensRepository>();

        services.TryAddSingleton(TimeProvider.System);

        services
            .AddRoleService()
            .AddPersonRoleService()
            .AddRefreshTokenServices()
            .AddJwtTokenServices()
            .AddOtpTokenServices()
            .AddAuthenticationFlowServices()
            .AddScoped<IPersonRoleService, PersonRoleService>()
            .AddScoped<IJwtTokenService, JwtTokenService>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddScoped<IAuthService, AuthService>();

        services
            .AddSingleton(authOptions);

        services
            .AddScoped<IRefreshTokenService, RefreshTokenService>()
            .AddScoped<IUserService, UserService>();

        return services;
    }

    private static IServiceCollection AddRoleService(this IServiceCollection services)
    {
        return services
            .AddScoped<ICreateRoleService, CreateRoleService>()
            .AddScoped<IGetAllRolesService, GetAllRolesService>()
            .AddScoped<IGetRoleByNameService, GetRoleByNameService>()
            .AddScoped<IUpdateRoleService, UpdateRoleService>()
            .AddScoped<IDeleteRoleByNameService, DeleteRoleByNameService>()
            .AddScoped<IRoleService, RoleService>();
    }

    private static IServiceCollection AddPersonRoleService(this IServiceCollection services)
    {
        return services.AddScoped<IAssignRoleToPersonService, AssignRoleToPersonService>()
            .AddScoped<IGetRolesForPersonService, GetRolesForPersonService>()
            .AddScoped<IGetPersonRolesByRoleIdService, GetPersonRolesByRoleIdService>()
            .AddScoped<IUnassignRoleFromPersonService, UnassignRoleFromPersonService>()
            .AddScoped<IPersonRoleService, PersonRoleService>();
    }
    
    private static IServiceCollection AddRefreshTokenServices(this IServiceCollection services)
    {
        return services
            .AddScoped<IGenerateRefreshTokenService, GenerateRefreshTokenService>()
            .AddScoped<IValidateRefreshTokenService, ValidateRefreshTokenService>()
            .AddScoped<IRevokeRefreshTokenService, RevokeRefreshTokenService>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>();
    }
    
    private static IServiceCollection AddJwtTokenServices(this IServiceCollection services)
    {
        return services
            .AddScoped<IGenerateJwtTokenService, GenerateJwtTokenService>()
            .AddScoped<IJwtTokenService, JwtTokenService>();
    }
    
    private static IServiceCollection AddOtpTokenServices(this IServiceCollection services)
    {
        return services
            .AddScoped<IGenerateOtpService, GenerateOtpService>()
            .AddScoped<IValidateOtpService, ValidateOtpService>()
            .AddScoped<IOtpTokenService, OtpTokenService>();
    }
    
    private static IServiceCollection AddAuthenticationFlowServices(this IServiceCollection services)
    {
        return services
            .AddScoped<IRequestOtpByEmailService, RequestOtpByEmailService>()
            .AddScoped<ILoginWithOtpService, LoginWithOtpService>()
            .AddScoped<IRefreshTokenExchangeService, RefreshTokenExchangeService>()
            .AddScoped<IAuthenticationFlowService, AuthenticationFlowService>();
    }
}