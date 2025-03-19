using Application;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Infra;
using Infra.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Xcel.Services.Auth.Interfaces;
using Xcel.Services.Email.Interfaces;
using Xcel.TestUtils.Mocks;
using Xunit;

namespace Xcel.TestUtils;

public abstract class BaseTest : IAsyncLifetime
{
    private ServiceProvider _serviceProvider;
    private readonly AppDbContext _context;

    protected BaseTest()
    {
        _serviceProvider = CreateServiceProvider();
        _context = GetService<AppDbContext>();
    }

    protected static FakeTimeProvider FakeTimeProvider { get; } = new(new DateTime(2025, 1, 1));

    protected ISender Sender => GetService<ISender>();
    protected ISubjectsRepository SubjectsRepository => GetService<ISubjectsRepository>();
    protected ITutorsRepository TutorsRepository => GetService<ITutorsRepository>();
    protected IPersonsRepository PersonsRepository => GetService<IPersonsRepository>();
    protected IOtpRepository OtpRepository => GetService<IOtpRepository>();
    protected InMemoryFileService InMemoryFileService => (InMemoryFileService)GetService<IFileService>();
    protected InMemoryEmailSender InMemoryEmailSender => (InMemoryEmailSender)GetService<IEmailSender>();
    protected IEmailService EmailService => GetService<IEmailService>();
    protected IOtpService OtpService => GetService<IOtpService>();
    protected IAccountService AccountService => GetService<IAccountService>();
    private AppDbContext Context => GetService<AppDbContext>();

    public virtual async Task InitializeAsync()
    {
        await EnsureDatabaseCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await EnsureDatabaseDeletedAsync();
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.test.json")
            .Build();

        var infraOptions = configuration.GetRequiredSection("Infra").Get<InfraOptions>()
                           ?? throw new Exception("It's mandatory to have the Infra configuration");

        infraOptions.Database.ConnectionString =
            infraOptions.Database.ConnectionString.Replace("<guid>", $"{Guid.NewGuid()}");

        var services = new ServiceCollection()
            .AddApplicationServices()
            .AddInfraServices(infraOptions, EnvironmentKind.Production);

        services.AddSingleton(services);
        
        return MockServices(services)
            .BuildServiceProvider();
    }

    private static IServiceCollection MockServices(IServiceCollection services)
    {
        return services
            .AddSingleton<TimeProvider>(FakeTimeProvider)
            .AddScoped<IFileService, InMemoryFileService>()
            .AddScoped<IEmailSender, InMemoryEmailSender>();
    }

    private T GetService<T>() where T : class => _serviceProvider.GetRequiredService<T>();

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