using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Features.Account.Commands.Interfaces;

internal interface ICreateAccountCommand
{
    Task<Result<Person>> ExecuteAsync(Person person, CancellationToken cancellationToken = default);
}