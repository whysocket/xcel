using Xcel.Services.Auth.Interfaces.Services;

namespace Xcel.TestUtils.Mocks.XcelServices.Auth;

public class FakeClientInfoService : IClientInfoService
{
    private readonly Random _random = new();

    public string GetIpAddress()
    {
        var ipv4Address = $"{_random.Next(0, 256)}.{_random.Next(0, 256)}.{_random.Next(0, 256)}.{_random.Next(0, 256)}";

        return ipv4Address;
    }
}