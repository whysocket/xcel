namespace Xcel.Services.Auth.Tests;

public class AuthBaseTest : BaseTest
{
    protected async Task<Person> CreatePersonAsync()
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            EmailAddress = "test@test.com",
            FirstName = "John",
            LastName = "Doe",
        };

        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        return person;
    }
}