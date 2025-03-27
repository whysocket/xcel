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

await builder.Services
    .AddExternalServices(builder.Configuration, environment);

builder.Services
    .AddApiOptions(builder.Configuration)
    .AddSingleton(environment)
    .AddProblemDetails()
    .AddWebhooks()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddOpenApi()
    .AddHttpClient();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapAdminEndpoints()
    .MapTutorApplicantEndpoints()
    .MapSubjectEndpoints()
    .MapAccountEndpoints();

app.UseHttpsRedirection();

app.Run();