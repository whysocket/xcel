using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Presentation.API;
using Presentation.API.Endpoints;
using Presentation.API.Endpoints.Account;
using Presentation.API.Endpoints.Admin;
using Presentation.API.Endpoints.TutorApplication;
using Presentation.API.Webhooks;
using Scalar.AspNetCore;
using Xcel.Config.Options;

var builder = WebApplication.CreateBuilder(args);

var environment = new EnvironmentOptions(builder.Configuration.GetValue<EnvironmentType>("Environment"));

var infraOptions = await builder.Services
    .AddExternalServices(builder.Configuration, environment);

builder.Services
    .AddApiOptions(builder.Configuration)
    .AddSingleton(environment)
    .AddProblemDetails()
    .AddWebhooks()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddOpenApi(options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); })
    .AddHttpClient();

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

app.MapAdminEndpoints()
    .MapTutorApplicantEndpoints()
    .MapSubjectEndpoints()
    .MapAccountEndpoints();

app.MapGet("/test1", () => "This is test 1 endpoint")
    .WithName("Test1")
    .RequireAuthorization();

app.UseHttpsRedirection();

app.Run();

internal sealed class BearerSecuritySchemeTransformer(
    Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                [JwtBearerDefaults.AuthenticationScheme] = new()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = JwtBearerDefaults.AuthenticationScheme, Type = ReferenceType.SecurityScheme } }] =
                        Array.Empty<string>()
                });
            }
        }
    }
}