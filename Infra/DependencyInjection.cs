using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Infra.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infra;

public class DatabaseOptions
{
    public required string ConnectionString { get; set; }
}

public class InfraOptions
{
    public required DatabaseOptions Database { get; set; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfraServices(
        this IServiceCollection services,
        InfraOptions infraOptions)
    {
        services
            .AddDbContext<AppDbContext>(o =>
            {
                o.UseNpgsql(infraOptions.Database.ConnectionString);
            })
            .AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))
            .AddScoped<ISubjectsRepository, SubjectsRepository>();

        return services;
    }
}
