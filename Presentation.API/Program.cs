using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Polly;
using Presentation.API;
using Presentation.API.Endpoints.Account;
using Presentation.API.Endpoints.Admin;
using Presentation.API.Endpoints.Moderator;
using Presentation.API.Endpoints.Reviewer;
using Presentation.API.Endpoints.TutorApplication;
using Presentation.API.Services.Xcel.Auth;
using Presentation.API.Transformers;
using Presentation.API.Webhooks;
using Scalar.AspNetCore;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Email.Implementations;
using Xcel.Services.Email.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var environmentOptions = builder.Services.AddEnvironmentOptions(builder.Configuration);

var infraOptions = await builder.Services.AddExternalServices(
    builder.Configuration,
    environmentOptions
);

var apiOptions = builder.Services.AddApiOptions(builder.Configuration);

if (environmentOptions.IsProduction())
{
    infraOptions.Database.ConnectionString = GetRequiredEnvironmentVariable("CONNECTION_STRING");
    apiOptions.Webhooks.DiscordUrl = GetRequiredEnvironmentVariable("DISCORD_WEBHOOK_URL");
    infraOptions.Auth.Jwt.SecretKey = GetRequiredEnvironmentVariable("JWT_SECRET_KEY");
    infraOptions.Email.BaseUrl = GetRequiredEnvironmentVariable("EMAIL_BASE_URL");
}

// Xcel.Auth
builder.Services.AddSingleton<IClientInfoService, HttpClientInfoService>();

builder
    .Services.AddProblemDetails()
    .AddWebhooks()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    })
    .AddHttpClient()
    .AddHttpContextAccessor();

builder.Services.AddControllers();

builder
    .Services.AddAuthorization()
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = infraOptions.Auth.Jwt.TokenValidationParameters;
    });

builder.Services.AddCors();

builder
    .Services.AddHttpClient<IEmailService, HttpEmailService>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
    );

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
        // Optionally restrict to known proxies/networks
        // KnownProxies = { IPAddress.Parse("127.0.0.1") }
    }
);

app.UseExceptionHandler();

app.UseAuthentication().UseAuthorization();

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.WithPreferredScheme(JwtBearerDefaults.AuthenticationScheme);
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.RestSharp);
});

app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.MapAdminEndpoints()
    .MapModeratorEndpoints()
    .MapReviewerEndpoints()
    .MapTutorApplicationEndpoints()
    .MapAccountEndpoints();

app.Run();

static string GetRequiredEnvironmentVariable(string variableName)
{
    return Environment.GetEnvironmentVariable(variableName)
        ?? throw new InvalidOperationException(
            $"{variableName} environment variable must be set in production."
        );
}
