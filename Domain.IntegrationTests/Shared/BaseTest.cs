using Domain.Interfaces.Repositories;
using Infra;
using Infra.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Domain.IntegrationTests.Shared;

public abstract class BaseTest : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AppDbContext _context;
    protected readonly ISender Sender;
    protected readonly ISubjectsRepository SubjectsRepository;

    public BaseTest()
    {
        _serviceProvider = SetupDependencyInjection();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();

        Sender = _serviceProvider.GetRequiredService<ISender>();
        SubjectsRepository = _serviceProvider.GetRequiredService<ISubjectsRepository>();
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

        return new ServiceCollection()
            .AddDomainServices()
            .AddInfraServices(infraOptions)
            .BuildServiceProvider();
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
