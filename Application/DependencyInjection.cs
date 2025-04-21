using Application.Implementations;
using Application.Interfaces;
using Application.Pipelines;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(IAssemblyMarker).Assembly;
        var domainAssembly = typeof(Domain.IAssemblyMarker).Assembly;

        services
            .AddValidatorsFromAssemblies([applicationAssembly, domainAssembly])
            .AddMediatR(c =>
            {
                c.RegisterServicesFromAssembly(applicationAssembly);
                c.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

        services
            .AddScoped<IReviewerAssignmentService, ReviewerAssignmentService>();
        
        services
            .AddScoped<IGetMyTutorApplicationQuery, GetMyTutorApplicationQuery>();

        return services;
    }
}