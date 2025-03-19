using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using Xcel.Services.Email.Implementations;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.OtpEmail;
using Xcel.Services.Email.Templates.TutorApprovalEmail;
using Xcel.Services.Email.Templates.TutorRejectionEmail;
using Xcel.Services.Email.Templates.WelcomeEmail;
using Xcel.TestUtils.Mocks;

namespace Xcel.Services.Email.Tests;

public class TemplatedEmailServiceTemplateRenderingTests
{
    private readonly ILogger<TemplatedEmailService> _loggerSubstitute;
    private readonly InMemoryEmailSender _inMemoryEmailSender = new();
    private readonly TemplatedEmailService _emailService;
    private readonly Random _random = new();

    public TemplatedEmailServiceTemplateRenderingTests()
    {
        _loggerSubstitute = Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplatedEmailService>.Instance;
        _emailService = new TemplatedEmailService(_inMemoryEmailSender, _loggerSubstitute);
    }

    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task SendEmailAsync_ValidPayload_SendsEmailWithCorrectContent(Type dataType, string json)
    {
        // Arrange
        var dataInstance = JsonSerializer.Deserialize(json, dataType)!;
        ValidateTemplateRendering(dataType, dataInstance);

        var subject = GenerateRandomString("Subject");
        var toAddress = GenerateRandomEmailAddress();
        var payload = CreateEmailPayload(dataType, dataInstance, subject, toAddress);

        // Act
        var sentEmail = await SendAndRetrieveEmail(payload, dataType);

        // Assert
        AssertSentEmailBodyMatchesRendered(sentEmail, dataType, dataInstance);
        AssertPayloadsAreEqual(payload, sentEmail.GetType().GetProperty("Payload")!.GetValue(sentEmail)!);

        // Cleanup
        _inMemoryEmailSender.ClearSentEmails();
    }

    private void ValidateTemplateRendering(Type dataType, object dataInstance)
    {
        var renderedBody = RenderTemplate(dataType, dataInstance);
        Assert.NotNull(renderedBody);
        Assert.NotEmpty(renderedBody);
    }

    private static object CreateEmailPayload(Type dataType, object dataInstance, string subject, string toAddress)
    {
        var emailPayloadType = typeof(EmailPayload<>).MakeGenericType(dataType);
        return Activator.CreateInstance(emailPayloadType, subject, toAddress, dataInstance)!;
    }

    private void AssertSentEmailBodyMatchesRendered(object sentEmail, Type dataType, object dataInstance)
    {
        var renderedBody = RenderTemplate(dataType, dataInstance);
        var sentPayload = sentEmail.GetType().GetProperty("Payload")!.GetValue(sentEmail);
        Assert.Equal(renderedBody, sentPayload!.GetType().GetProperty("Body")!.GetValue(sentPayload));
    }

    public static TheoryData<Type, string> GetTestData()
    {
        var data = new TheoryData<Type, string>();

        var assembly = Assembly.Load("Xcel.Services.Email");
        var namespaceName = "Xcel.Services.Email.Templates";

        var dataClasses = assembly.GetTypes()
            .Where(type => type.Namespace!.StartsWith(namespaceName) &&
                           type.Name.EndsWith("Data") &&
                           (type.IsClass || IsRecord(type)) &&
                           type.IsPublic)
            .ToList();

        var dataGeneratorFactory = new DataGeneratorFactory();

        foreach (var dataClass in dataClasses)
        {
            var dataInstance = dataGeneratorFactory.CreateDataInstance(dataClass);
            var json = JsonSerializer.Serialize(dataInstance);
            data.Add(dataClass, json);
        }

        return data;
    }

    private async Task<object> SendAndRetrieveEmail(object payload, Type dataType)
    {
        try
        {
            await _emailService.SendEmailAsync(payload as dynamic);

            var getSentEmailMethod = _inMemoryEmailSender.GetType().GetMethod(nameof(InMemoryEmailSender.GetSentEmail));
            var genericMethod = getSentEmailMethod!.MakeGenericMethod(dataType);

            return genericMethod.Invoke(_inMemoryEmailSender, null)!;
        }
        catch (Exception ex)
        {
            _loggerSubstitute.LogError(ex, "Error sending and retrieving email.");
            throw;
        }
    }

    private static void AssertPayloadsAreEqual(object expected, object actual)
    {
        var expectedProperties = expected.GetType().GetProperties();

        foreach (var property in expectedProperties)
        {
            var actualProperty = actual.GetType().GetProperty(property.Name);
            Assert.NotNull(actualProperty);

            var expectedValue = property.GetValue(expected);
            var actualValue = actualProperty.GetValue(actual);

            Assert.Equal(expectedValue, actualValue);
        }
    }

    private string GenerateRandomString(string prefix)
    {
        return $"{prefix}_{_random.Next(1000, 9999)}";
    }

    private string GenerateRandomEmailAddress()
    {
        return $"{GenerateRandomString("user")}@{GenerateRandomString("domain")}.com";
    }

    private string RenderTemplate(Type dataType, object dataInstance)
    {
        var templateName = dataType.Name.Replace("Data", "Template.hbs");
        var templateFolderName = dataType.Name.Replace("Data", "");
        var templatePath = Path.Combine("Templates", templateFolderName, templateName);

        try
        {
            if (!File.Exists(templatePath))
            {
                _loggerSubstitute.LogWarning($"Template file not found: {templatePath}");
                return "";
            }

            var templateContent = File.ReadAllText(templatePath);
            var template = Handlebars.Compile(templateContent);
            return template(dataInstance);
        }
        catch (Exception ex)
        {
            _loggerSubstitute.LogError(ex, $"Error rendering template: {templatePath}");
            return "";
        }
    }

    private static bool IsRecord(Type type)
    {
        return type.IsClass &&
               type.GetMethods().Any(m => m.Name == "<Clone>$");
    }

    private class DataGeneratorFactory
    {
        private readonly Dictionary<Type, Func<object>> _generators = new();

        public DataGeneratorFactory()
        {
            AddFake<WelcomeEmailData>(() => new("TestFirstName", "TestLastName"));
            AddFake<OtpEmailData>(() => new("123456", DateTime.UtcNow, "Test Full Name"));
            AddFake<TutorApprovalEmailData>(() => new("TestFirstName", "TestLastName"));
            AddFake<TutorRejectionEmailData>(() => new("TestFirstName", "TestLastName", "Rejection"));
        }

        private void AddFake<T>(Func<T> generator) where T : class
        {
            _generators[typeof(T)] = () => generator();
        }

        public object CreateDataInstance(Type dataType)
        {
            if (_generators.TryGetValue(dataType, out var generator))
            {
                return generator();
            }

            throw new NotSupportedException($"No generator found for data type {dataType.Name}.");
        }
    }
}