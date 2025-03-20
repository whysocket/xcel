using Presentation.API;
using Presentation.API.Endpoints;
using Presentation.API.Webhooks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.AddOptionsAndServices(builder.Configuration);

builder.Services
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
    .MapTutorApplicationEndpoints()
    .MapSubjectEndpoints();

app.UseHttpsRedirection();

app.Run();