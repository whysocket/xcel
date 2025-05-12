namespace Presentation.API.Endpoints.Account.Responses;

public record GetMeResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string EmailAddress,
    IEnumerable<string> Roles
);
