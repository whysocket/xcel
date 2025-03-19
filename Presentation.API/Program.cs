using Application;
using Infra;
using Presentation.API;
using Presentation.API.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var infraOptions = builder.Configuration.GetRequiredSection("Infra").Get<InfraOptions>()
                   ?? throw new Exception("It's mandatory to have the Infra configuration");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var environment = builder.Configuration.GetRequiredSection("Environment").Get<EnvironmentKind>();

builder.Services.AddInfraServices(infraOptions, environment);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapAdminEndpoints()
    .MapTutorApplicationEndpoints()
    .MapSubjectEndpoints();

app.UseHttpsRedirection();
app.Run();