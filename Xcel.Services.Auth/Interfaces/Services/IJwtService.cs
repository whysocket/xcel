using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IJwtService
{
    Result<string> Generate(Person person);
}