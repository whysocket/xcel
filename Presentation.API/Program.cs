using Microsoft.AspNetCore.Authentication.JwtBearer;
using Presentation.API;
using Presentation.API.Endpoints.Account;
using Presentation.API.Endpoints.Admin;
using Presentation.API.Endpoints.Moderator;
using Presentation.API.Endpoints.TutorApplication;
using Presentation.API.Services.Xcel.Auth;
using Presentation.API.Transformers;
using Presentation.API.Webhooks;
using Scalar.AspNetCore;
using Xcel.Services.Auth.Interfaces.Services;

var builder = WebApplication.CreateBuilder(args);

var environmentOptions = builder
    .Services
    .AddEnvironmentOptions(builder.Configuration);

var infraOptions = await builder.Services
    .AddExternalServices(builder.Configuration, environmentOptions);

var apiOptions = builder
    .Services
    .AddApiOptions(builder.Configuration);

// Xcel.Auth
builder.Services
    .AddSingleton<IClientInfoService, HttpClientInfoService>();

builder.Services
    .AddProblemDetails()
    .AddWebhooks()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddOpenApi(options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); })
    .AddHttpClient()
    .AddHttpContextAccessor();

builder.Services
    .AddControllers();

builder.Services
    .AddAuthorization()
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = infraOptions.Auth.Jwt.TokenValidationParameters;
    });

builder.Services
    .AddCors(op => op.AddDefaultPolicy(builder =>
    {
        builder
            .WithOrigins(apiOptions.Cors.FrontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }));

var app = builder.Build();
app.UseExceptionHandler();

app.UseAuthentication()
    .UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithPreferredScheme(JwtBearerDefaults.AuthenticationScheme);
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.RestSharp);
    });
}

app.UseCors();

app
    .MapAdminEndpoints()
    .MapModeratorEndpoints()
    .MapTutorApplicationEndpoints()
    .MapAccountEndpoints();

app.UseHttpsRedirection();

app.Run();