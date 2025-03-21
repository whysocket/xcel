using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Endpoints.TutorApplication;

public record CreateTutorApplicationRequest(
    [FromForm] string FirstName,
    [FromForm] string LastName,
    [FromForm] string EmailAddress,
    IFormFile Cv);