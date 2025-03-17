using Application;
using Domain.IntegrationTests.Services;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Infra;
using Infra.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xcel.Services.Email.Interfaces;

namespace Domain.IntegrationTests.Shared;

public abstract class BaseTest : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AppDbContext _context;
    protected readonly ISender Sender;
    protected readonly ISubjectsRepository SubjectsRepository;
    protected readonly ITutorsRepository TutorsRepository;
    protected InMemoryFileService InMemoryFileService;
    protected InMemoryEmailService InMemoryEmailService;

    public BaseTest()
    {
        _serviceProvider = SetupDependencyInjection();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();

        Sender = _serviceProvider.GetRequiredService<ISender>();
        SubjectsRepository = _serviceProvider.GetRequiredService<ISubjectsRepository>();
        TutorsRepository = _serviceProvider.GetRequiredService<ITutorsRepository>();
        InMemoryFileService = (InMemoryFileService)_serviceProvider.GetRequiredService<IFileService>();
        InMemoryEmailService = (InMemoryEmailService)_serviceProvider.GetRequiredService<IEmailService>();
    }

    private static ServiceProvider SetupDependencyInjection()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.test.json")
            .Build();

        var infraOptions = configuration.GetRequiredSection("Infra").Get<InfraOptions>()
                           ?? throw new Exception("It's mandatory to have the Infra configuration");

        infraOptions.Database.ConnectionString = infraOptions.Database.ConnectionString.Replace("<guid>", $"{Guid.NewGuid()}");


        var services = new ServiceCollection()
            .AddApplicationServices()
            .AddInfraServices(infraOptions);

        return MockServices(services).BuildServiceProvider();
    }

    private static IServiceCollection MockServices(IServiceCollection services)
    {
        return services
            .AddScoped<IFileService, InMemoryFileService>()
            .AddScoped<IEmailService, InMemoryEmailService>();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _context.Database.EnsureDeletedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting database: {ex}");
        }
        finally
        {
            await _context.DisposeAsync();
            await _serviceProvider.DisposeAsync();
        }
    }
}