using Application;
using Application.Interfaces;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Infra;
using Infra.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xcel.Config.Options;
using Xcel.Services.Auth;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Public;
using Xcel.Services.Email.Interfaces;
using Xcel.TestUtils.Mocks.XcelServices;
using Xcel.TestUtils.Mocks.XcelServices.Auth;
using Xcel.TestUtils.Mocks.XcelServices.Email;
using Xunit;
using Xunit.Abstractions;

namespace Xcel.TestUtils;

public abstract class BaseTest : IAsyncLifetime
{
    protected static ILogger<T> CreateLogger<T>() => NullLogger<T>.Instance;

    private ServiceProvider _serviceProvider = null!;
    private AppDbContext _context = null!;

    protected static FakeTimeProvider FakeTimeProvider { get; } = new(DateTimeOffset.UtcNow);

    protected ISender Sender => GetService<ISender>();
    protected ISubjectsRepository SubjectsRepository => GetService<ISubjectsRepository>();
    protected ITutorApplicationsRepository TutorApplicationsRepository => GetService<ITutorApplicationsRepository>();
    protected ITutorDocumentsRepository TutorDocumentsRepository => GetService<ITutorDocumentsRepository>();
    protected ITutorProfilesRepository TutorProfilesesRepository => GetService<ITutorProfilesRepository>();
    protected IPersonsRepository PersonsRepository => GetService<IPersonsRepository>();
    protected FakeClientInfoService FakeClientInfoService => (FakeClientInfoService)GetService<IClientInfoService>();
    protected IAuthServiceSdk AuthServiceSdk => GetService<IAuthServiceSdk>();
    protected IReviewerAssignmentService ReviewerAssignmentService => GetService<IReviewerAssignmentService>();
    protected InMemoryFileService InMemoryFileService => (InMemoryFileService)GetService<IFileService>();
    protected InMemoryEmailService InMemoryEmailService => (InMemoryEmailService)GetService<IEmailService>();
    protected IEmailService EmailService => GetService<IEmailService>();
    
    protected IGetMyTutorApplicationQuery GetMyTutorApplicationQuery => GetService<IGetMyTutorApplicationQuery>();

    protected InfraOptions InfraOptions => GetService<InfraOptions>();
    protected AuthOptions AuthOptions => GetService<InfraOptions>().Auth;
    private AppDbContext Context => GetService<AppDbContext>();

    public virtual async Task InitializeAsync()
    {
        _serviceProvider = await CreateServiceProvider();
        _context = GetService<AppDbContext>();
        await EnsureDatabaseCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await EnsureDatabaseDeletedAsync();
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }
    
    private static bool IsDevelopmentEnvironment()
    {
        // This string is defined by Rider
        var envString = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        if (string.IsNullOrWhiteSpace(envString))
            return false;

        return Enum.TryParse<EnvironmentType>(envString, ignoreCase: true, out var env) 
               && env == EnvironmentType.Development;
    }

    private static async Task<ServiceProvider> CreateServiceProvider()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.test.json");

        if (IsDevelopmentEnvironment())
        {
            builder.AddJsonFile("appsettings.local.json", optional: true);
        }

        var infraOptions = builder.Build().GetRequiredSection("Infra").Get<InfraOptions>()
                           ?? throw new Exception("It's mandatory to have the Infra configuration");

        infraOptions.Database.ConnectionString =
            infraOptions.Database.ConnectionString.Replace("{Guid}", Guid.NewGuid().ToString());

        var services = new ServiceCollection()
            .AddApplicationServices();

        await services.AddInfraServicesAsync(infraOptions, new(EnvironmentType.Production));

        services.AddSingleton(services);

        return MockServices(services).BuildServiceProvider();
    }


    private static IServiceCollection MockServices(IServiceCollection services)
    {
        return services
            .AddSingleton<TimeProvider>(FakeTimeProvider)
            .AddSingleton<IClientInfoService, FakeClientInfoService>()
            .AddScoped<IFileService, InMemoryFileService>()
            .AddScoped<IEmailService, InMemoryEmailService>();
    }

    protected T GetService<T>() where T : class => _serviceProvider.GetRequiredService<T>();

    private async Task EnsureDatabaseCreatedAsync()
    {
        await Context.Database.EnsureCreatedAsync();
    }

    private async Task EnsureDatabaseDeletedAsync()
    {
        try
        {
            await Context.Database.EnsureDeletedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting database: {ex}");
        }
    }
}