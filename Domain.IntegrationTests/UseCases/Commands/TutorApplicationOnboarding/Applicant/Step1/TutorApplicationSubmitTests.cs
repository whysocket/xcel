using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step1;
using Domain.Entities;
using Domain.Interfaces.Services;
using Domain.Payloads;
using Domain.Results;
using NSubstitute;
using Xcel.Services.Auth.Public;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step1;

public class TutorApplicationSubmitCommandTests : BaseTest
{
    private ITutorApplicationSubmitCommand _command = null!;
    private IAuthServiceSdk _authService = null!;
    private IFileService _fileService = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _authService = Substitute.For<IAuthServiceSdk>();
        _fileService = Substitute.For<IFileService>();

        _command = new TutorApplicationSubmitCommand(
            CreateLogger<TutorApplicationSubmitCommand>(),
            TutorApplicationsRepository,
            _authService,
            _fileService);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateTutorApplication_WhenValid()
    {
        // Arrange
        var person = new Person { FirstName = "Emma", LastName = "Stone", EmailAddress = "emma@xcel.com" };
        var input = new TutorApplicationSubmitInput(
            person.FirstName,
            person.LastName,
            person.EmailAddress,
            new DocumentPayload("cv.pdf", "application/pdf", [1, 2, 3]));

        _fileService.UploadAsync(Arg.Any<DocumentPayload>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok("path/to/cv.pdf"));

        // Act
        var result = await new TutorApplicationSubmitCommand(
            CreateLogger<TutorApplicationSubmitCommand>(),
            TutorApplicationsRepository,
            AuthServiceSdk, // Not the best thing
            _fileService).ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var app = await TutorApplicationsRepository.GetByIdAsync(result.Value);
        Assert.NotNull(app);
        Assert.Equal(input.EmailAddress.ToLower(), app!.Applicant.EmailAddress);
        Assert.Single(app.Documents);
        Assert.Equal("path/to/cv.pdf", app.Documents.First().DocumentPath);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenAccountCreationFails()
    {
        // Arrange
        var input = new TutorApplicationSubmitInput("John", "Doe", "john@fail.com", new DocumentPayload("cv.pdf", "application/pdf",
            [1]));
        _authService.CreateAccountAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<Person>(new Error(ErrorType.Unexpected, "Account creation failed")));

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Contains("Account creation failed", error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenCvUploadFails()
    {
        // Arrange
        _authService.CreateAccountAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new Person { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", EmailAddress = "jane@xcel.com" }));

        _fileService.UploadAsync(Arg.Any<DocumentPayload>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<string>(new Error(ErrorType.Unexpected, "Upload failed")));

        var input = new TutorApplicationSubmitInput("Jane", "Doe", "jane@xcel.com", new DocumentPayload("cv.pdf", "application/pdf",
            [1]));

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Contains("Upload failed", error.Message);
    }
}