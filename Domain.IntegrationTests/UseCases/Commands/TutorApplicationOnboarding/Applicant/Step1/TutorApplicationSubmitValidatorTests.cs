using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step1;
using Domain.Payloads;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step1;

public class TutorApplicationSubmitValidatorTests
{
    [Fact]
    public void Validate_ShouldReturnSuccess_WhenInputIsValid()
    {
        var input = new TutorApplicationSubmitInput(
            "Alice",
            "Johnson",
            "alice@example.com",
            new DocumentPayload("cv.pdf", "application/pdf", [1, 2, 3]));

        var result = TutorApplicationSubmitValidator.Validate(input);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AB")]
    public void Validate_ShouldFail_WhenFirstNameIsInvalid(string? firstName)
    {
        var input = new TutorApplicationSubmitInput(
            firstName!,
            "Doe",
            "john@example.com",
            new DocumentPayload("cv.pdf", "application/pdf", [1]));

        var result = TutorApplicationSubmitValidator.Validate(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("First name must be between"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AB")]
    public void Validate_ShouldFail_WhenLastNameIsInvalid(string? lastName)
    {
        var input = new TutorApplicationSubmitInput(
            "John",
            lastName!,
            "john@example.com",
            new DocumentPayload("cv.pdf", "application/pdf", [1]));

        var result = TutorApplicationSubmitValidator.Validate(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("Last name must be between"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_ShouldFail_WhenEmailIsInvalid(string? email)
    {
        var input = new TutorApplicationSubmitInput(
            "John",
            "Doe",
            email!,
            new DocumentPayload("cv.pdf", "application/pdf", [1]));

        var result = TutorApplicationSubmitValidator.Validate(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("A valid email address is required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCvFileNameIsEmpty()
    {
        var input = new TutorApplicationSubmitInput(
            "John",
            "Doe",
            "john@example.com",
            new DocumentPayload("", "application/pdf", [1]));

        var result = TutorApplicationSubmitValidator.Validate(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("CV filename is required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCvContentIsEmpty()
    {
        var input = new TutorApplicationSubmitInput(
            "John",
            "Doe",
            "john@example.com",
            new DocumentPayload("cv.pdf", "application/pdf", []));

        var result = TutorApplicationSubmitValidator.Validate(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("CV content must not be empty"));
    }
}