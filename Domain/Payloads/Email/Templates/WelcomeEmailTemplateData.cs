using Domain.Payloads.Email.Shared;

namespace Domain.Payloads.Email.Templates;

public class WelcomeEmailTemplateData : ITemplateData
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}