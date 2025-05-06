namespace Xcel.Services.Auth.Interfaces.Services;

public interface IClientInfoService
{
    string IpAddress { get; }

    Guid UserId { get; }
}
