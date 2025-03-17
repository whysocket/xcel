using Application.UseCases.Commands;
using Domain.Entities;
using Domain.Exceptions;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands;

public class CreateSubjectTests : BaseTest
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesSubject()
    {
        // Arrange
        var command = new CreateSubject.Command("History");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);

        var existingSubject = await SubjectsRepository.GetByIdAsync(result.Value);

        Assert.NotNull(existingSubject);
        Assert.Equal(command.Name, existingSubject.Name);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        // Arrange
        await SubjectsRepository.AddAsync(new Subject { Name = "Mathmatics" });
        await SubjectsRepository.SaveChangesAsync();

        var command = new CreateSubject.Command ("Mathmatics");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        Assert.Contains("The subject with name 'Mathmatics' already exists!", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_InvalidName_ReturnsValidationFailure()
    {
        // Arrange
        var command = new CreateSubject.Command ("to");

        // Act and Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => Sender.Send(command));

        var expectedErrors = new Dictionary<string, List<string>>
        {
            { 
                "Name", [ "The length of 'Name' must be at least 3 characters. You entered 2 characters." ] 
            }
        };

        Assert.Equal(expectedErrors, exception.Errors);
    }
}