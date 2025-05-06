using Application.UseCases.Queries;
using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Subjects;

public class GetAllSubjectsWithQualificationsQueryTests : BaseTest
{
    private IGetAllSubjectsWithQualificationsQuery _query = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _query = new GetAllSubjectsWithQualificationsQuery(SubjectsRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSubjectsWithQualifications_WhenValid()
    {
        // Arrange
        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Maths",
            Qualifications =
            [
                new Qualification { Id = Guid.NewGuid(), Name = "GCSE" },
                new Qualification { Id = Guid.NewGuid(), Name = "A-Level" },
            ],
        };

        await SubjectsRepository.AddAsync(subject);
        await SubjectsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(new PageRequest(1, 10));

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.Subjects;
        Assert.Single(items);
        Assert.Equal(subject.Id, items[0].Id);
        Assert.Equal(subject.Name, items[0].Name);
        Assert.Equal(2, items[0].Qualifications.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenNoSubjectsExist()
    {
        // Arrange
        // Act
        var result = await _query.ExecuteAsync(new PageRequest(1, 10));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Subjects);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.Pages);
    }
}
