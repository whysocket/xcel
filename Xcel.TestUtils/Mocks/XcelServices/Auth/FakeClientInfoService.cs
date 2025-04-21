using Domain.Entities;
using Xcel.Services.Auth.Interfaces.Services;

namespace Xcel.TestUtils.Mocks.XcelServices.Auth;

public class FakeClientInfoService : IClientInfoService
{
    private Person? Person { get; set; }

    public string IpAddress => "172.17.0.1";
    public Guid UserId => Person?.Id ?? Guid.Empty;

    public FakeClientInfoService WithUser(Person person)
    {
        Person = person;
        return this;
    }
}