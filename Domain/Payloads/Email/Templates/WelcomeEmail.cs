namespace Domain.Payloads.Email.Templates;

public record WelcomeEmailData(
    string FirstName,
    string LastName
);