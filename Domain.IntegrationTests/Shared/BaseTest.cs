using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Payloads;
using Domain.Payloads.Email.Shared;
using Infra;
using Infra.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Domain.IntegrationTests.Shared;

public class InMemoryFileService : IFileService
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public Task<string> UploadAsync(DocumentPayload file, CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid()}-{file.FileName}";
        _files.TryAdd(fileName, file.Content);

        return Task.FromResult(fileName);
    }

    public byte[]? GetFile(string fileName)
    {
        _files.TryGetValue(fileName, out var content);
        return content;
    }
}

public class InMemoryEmailService : IEmailService
{
    private readonly ConcurrentBag<SentEmail> _emails = new();

    internal class SentEmail
    {
        public object Payload { get; set; }
        public string Body { get; set; }
    }

    public Task<bool> SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
    {
        _emails.Add(new SentEmail { Payload = payload, Body = "mock" });
        return Task.FromResult(true);
    }

    internal IReadOnlyCollection<SentEmail> GetSentEmails()
    {
        return _emails.ToList().AsReadOnly();
    }
}


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
            .AddDomainServices()
            .AddInfraServices(infraOptions);

        services.Remove(services.First(x => x.ServiceType == typeof(IFileService)));
        services.AddScoped<IFileService, InMemoryFileService>();

        services.Remove(services.First(x => x.ServiceType == typeof(IEmailService)));
        services.AddScoped<IEmailService, InMemoryEmailService>();

        return services.BuildServiceProvider();
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
