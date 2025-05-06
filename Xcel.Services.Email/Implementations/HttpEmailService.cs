using System.Net.Http.Json;
using System.Text.Json;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;

namespace Xcel.Services.Email.Implementations;

public class TemplateRequest
{
    public required string Name { get; set; }

    public required object Data { get; set; }
}

public class SendRequest
{
    public required IEnumerable<string> To { get; set; }

    public required string Subject { get; set; }

    public required TemplateRequest Template { get; set; }
}

public static class EmailClientErrors
{
    public static readonly Error Unexpected = new(
        ErrorType.Unexpected,
        "An unexpected error occurred."
    );
    public static readonly Error HttpFailure = new(
        ErrorType.Conflict,
        "HTTP request to email service failed."
    );
    public static readonly Error JsonSerializationError = new(
        ErrorType.Validation,
        "Failed to serialize email payload."
    );
    public static readonly Error InvalidResponse = new(
        ErrorType.Unexpected,
        "Email service returned an error response."
    );
}

public class HttpEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpEmailService> _logger;

    public HttpEmailService(
        EmailOptions emailOptions,
        HttpClient httpClient,
        ILogger<HttpEmailService> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;

        if (!string.IsNullOrEmpty(emailOptions.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(emailOptions.BaseUrl);
        }
    }

    public async Task<Result> SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        CancellationToken cancellationToken = default
    )
        where TData : IEmail
    {
        try
        {
            _logger.LogInformation(
                "[HttpEmailService] Preparing to send email to {To} using template {Template}",
                payload.To,
                payload
            );

            var templateName = typeof(TData).Name;

            var requestPayload = new SendRequest
            {
                Subject = payload.Subject,
                To = payload.To,
                Template = new() { Name = templateName, Data = payload.Data },
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/send",
                requestPayload,
                cancellationToken
            );

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[HttpEmailService] Email sent successfully to {To}",
                    payload.To
                );
                return Result.Ok();
            }

            _logger.LogWarning(
                "[HttpEmailService] Email service responded with failure: {StatusCode}. Payload: {@Payload}, Response: {Response}",
                response.StatusCode,
                payload,
                responseContent
            );

            return Result.Fail(EmailClientErrors.InvalidResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "[HttpEmailService] HttpRequestException while sending email to {To}: {Error}",
                payload.To,
                EmailClientErrors.HttpFailure
            );
            return Result.Fail(EmailClientErrors.HttpFailure);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "[HttpEmailService] JsonException while serializing email to {To}: {Error}",
                payload.To,
                EmailClientErrors.JsonSerializationError
            );
            return Result.Fail(EmailClientErrors.JsonSerializationError);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HttpEmailService] Unexpected exception while sending email to {To}: {Error}",
                payload.To,
                EmailClientErrors.Unexpected
            );
            return Result.Fail(EmailClientErrors.Unexpected);
        }
    }
}
