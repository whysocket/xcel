using Domain.Entities;
using Xcel.Services.Auth.Interfaces.Services;

namespace Xcel.TestUtils.Mocks.XcelServices.Auth;

public class FakeClientInfoService : IClientInfoService
{
    public Person? Person { get; private set; }

    public string IpAddress => "172.17.0.1";
    public Guid PersonId => Person?.Id ?? Guid.Empty;

    public FakeClientInfoService WithPerson(Person person)
    {
        Person = person;
        return this;
    }
}