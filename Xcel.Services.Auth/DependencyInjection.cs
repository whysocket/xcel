using Microsoft.Extensions.DependencyInjection;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Implementations.Services.Roles;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.Roles;

namespace Xcel.Services.Auth;

internal static class DependencyInjection
{
    internal static IServiceCollection AddXcelAuthServices<TOtpRepository, TPersonsRepository, TRolesRepository, TPersonRoleRepository, TRefreshTokensRepository>(
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
            .AddScoped<IOtpService, OtpService>()
            .AddScoped<IAccountService, AccountService>()
            .AddScoped<IPersonRoleService, PersonRoleService>()
            .AddScoped<IJwtService, JwtService>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddScoped<IAuthService, AuthService>();

        services
            .AddSingleton(authOptions);

        services
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>()
            .AddScoped<IUserService, UserService>();

        return services;
    }

    internal static IServiceCollection AddRoleService(this IServiceCollection services)
    {
        return services
            .AddScoped<ICreateRoleService, CreateRoleService>()
            .AddScoped<IGetAllRolesService, GetAllRolesService>()
            .AddScoped<IGetRoleByNameService, GetRoleByNameService>()
            .AddScoped<IUpdateRoleService, UpdateRoleService>()
            .AddScoped<IDeleteRoleByNameService, DeleteRoleByNameService>()
            .AddScoped<IRoleService, RoleService>();
    }
}