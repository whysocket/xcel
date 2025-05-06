using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Endpoints.TutorApplication.Responses;

public record CreateRequest(
    [FromForm] [property: Description("Applicant's first name.")] string FirstName,
    [FromForm] [property: Description("Applicant's last name.")] string LastName,
    [FromForm] [property: Description("Applicant's email address.")] string EmailAddress,
    [property: Description("CV document file uploaded by the applicant.")] IFormFile Cv
);

public record CreateTutorApplicationResponse(
    [property: Description("Unique identifier of the newly created tutor application.")]
        Guid TutorApplicationId
);
