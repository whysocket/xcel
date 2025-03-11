using Domain.Entities;
using Domain.UseCases;
using Domain.IntegrationTests.Shared;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.IntegrationTests.UseCases;

public class GetAllSubjectsWithQualificationsTests : BaseTest
{
    [Fact]
    public async Task Handle_ReturnsPaginatedSubjectsWithQualifications()
    {
        // Arrange
        var subjects = new List<Subject>
        {
            new() { Name = "Subject 1", Qualifications = [new() { Name = "Qualification 1" }] },
            new() { Name = "Subject 2", Qualifications = [new() { Name = "Qualification 2" }] },
            new() { Name = "Subject 3", Qualifications = [new() { Name = "Qualification 3" }] }
        };
        await SubjectsRepository.AddRangeAsync(subjects);
        await SubjectsRepository.SaveChangesAsync();

        var query = new GetAllSubjectsWithQualifications.Query { PageRequest = new PageRequest(1, 2) };

        // Act
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Subjects.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Pages);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListWhenNoSubjectsExist()
    {
        // Arrange
        var query = new GetAllSubjectsWithQualifications.Query();

        // Act
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Subjects);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.Pages);
    }

    [Fact]
    public async Task Handle_AppliesPaginationCorrectly()
    {
        // Arrange
        var subjects = new List<Subject>
        {
            new() { Name = "Subject 1", Qualifications = [new() { Name = "Qualification 1" }] },
            new() { Name = "Subject 2", Qualifications = [new() { Name = "Qualification 2" }] },
            new() { Name = "Subject 3", Qualifications = [new() { Name = "Qualification 3" }] },
            new() { Name = "Subject 4", Qualifications = [new() { Name = "Qualification 4" }] }
        };

        await SubjectsRepository.AddRangeAsync(subjects);
        await SubjectsRepository.SaveChangesAsync();

        var query = new GetAllSubjectsWithQualifications.Query { PageRequest = new PageRequest(2, 2) };

        // Act
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Subjects.Count);
        Assert.Equal(4, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Pages);
    }
}