using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xcel.Services.Auth.Features.Account.Commands.Implementations;
using Xcel.Services.Auth.Features.Account.Commands.Interfaces;
using Xcel.Services.Auth.Features.Authentication.Commands.Implementations;
using Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;
using Xcel.Services.Auth.Features.Jwt.Commands.Implementations;
using Xcel.Services.Auth.Features.Jwt.Commands.Interfaces;
using Xcel.Services.Auth.Features.Otp.Commands.Implementations;
using Xcel.Services.Auth.Features.Otp.Commands.Interfaces;
using Xcel.Services.Auth.Features.PersonRoles.Commands.Implementations;
using Xcel.Services.Auth.Features.PersonRoles.Commands.Interfaces;
using Xcel.Services.Auth.Features.PersonRoles.Queries.Implementations;
using Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Implementations;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;
using Xcel.Services.Auth.Features.Roles.Commands.Implementations;
using Xcel.Services.Auth.Features.Roles.Commands.Interfaces;
using Xcel.Services.Auth.Features.Roles.Queries.Implementations;
using Xcel.Services.Auth.Features.Roles.Queries.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Public;

namespace Xcel.Services.Auth;

internal static class DependencyInjection
{
    internal static IServiceCollection AddXcelAuthServices<
        TOtpRepository,
        TPersonsRepository,
        TRolesRepository,
        TPersonRoleRepository,
        TRefreshTokensRepository
    >(this IServiceCollection services, AuthOptions authOptions)
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
            .AddRoleFeature()
            .AddPersonRoleFeature()
            .AddRefreshTokenFeature()
            .AddJwtFeature()
            .AddOtpFeature()
            .AddAuthenticationFeature()
            .AddAccountFeature();

        services.AddScoped<IAuthServiceSdk, AuthServiceSdk>();

        services.AddSingleton(authOptions);

        return services;
    }

    private static IServiceCollection AddRoleFeature(this IServiceCollection services)
    {
        return services
            .AddScoped<ICreateRoleCommand, CreateRoleCommand>()
            .AddScoped<IGetAllRolesQuery, GetAllRolesQuery>()
            .AddScoped<IGetRoleByNameQuery, GetRoleByNameQuery>()
            .AddScoped<IUpdateRoleCommand, UpdateRoleCommand>()
            .AddScoped<IDeleteRoleByNameCommand, DeleteRoleByNameCommand>();
    }

    private static IServiceCollection AddPersonRoleFeature(this IServiceCollection services)
    {
        return services
            .AddScoped<IAssignRoleToPersonCommand, AssignRoleToPersonCommand>()
            .AddScoped<IGetRolesForPersonQuery, GetRolesForPersonQuery>()
            .AddScoped<IGetPersonRolesByRoleIdQuery, GetPersonRolesByRoleIdQuery>()
            .AddScoped<IUnassignRoleFromPersonCommand, UnassignRoleFromPersonCommand>();
    }

    private static IServiceCollection AddRefreshTokenFeature(this IServiceCollection services)
    {
        return services
            .AddScoped<IGenerateRefreshTokenCommand, GenerateRefreshTokenCommand>()
            .AddScoped<IValidateRefreshTokenCommand, ValidateRefreshTokenCommand>()
            .AddScoped<IRevokeRefreshTokenCommand, RevokeRefreshTokenCommand>();
    }

    private static IServiceCollection AddJwtFeature(this IServiceCollection services)
    {
        return services.AddScoped<IGenerateJwtTokenCommand, GenerateJwtTokenCommand>();
    }

    private static IServiceCollection AddOtpFeature(this IServiceCollection services)
    {
        return services
            .AddScoped<IGenerateOtpCommand, GenerateOtpCommand>()
            .AddScoped<IValidateOtpCommand, ValidateOtpCommand>();
    }

    private static IServiceCollection AddAuthenticationFeature(this IServiceCollection services)
    {
        return services
            .AddScoped<IRequestOtpByEmailCommand, RequestOtpByEmailCommand>()
            .AddScoped<ILoginWithOtpCommand, LoginWithOtpCommand>()
            .AddScoped<IExchangeRefreshTokenCommand, ExchangeRefreshTokenCommand>();
    }

    private static IServiceCollection AddAccountFeature(this IServiceCollection services)
    {
        return services
            .AddScoped<ICreateAccountCommand, CreateAccountCommand>()
            .AddScoped<IDeleteAccountCommand, DeleteAccountCommand>();
    }
}
