using Domain.Pipelines;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        var currentAssembly = typeof(DependencyInjection).Assembly;

        services
            .AddValidatorsFromAssembly(currentAssembly)
            .AddMediatR(c =>
            {
                c.RegisterServicesFromAssembly(currentAssembly);
                c.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

        return services;
    }
}
