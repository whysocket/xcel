using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Endpoints.TutorApplication.Requests;

public record CreateTutorApplicationRequest(
    [FromForm] string FirstName,
    [FromForm] string LastName,
    [FromForm] string EmailAddress,
    IFormFile Cv);