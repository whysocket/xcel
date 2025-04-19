using Domain.Results;

namespace Xcel.Services.Auth.Features.Account.Commands.Interfaces;

internal interface IDeleteAccountCommand
{
    Task<Result> ExecuteAsync(Guid personId, CancellationToken cancellationToken = default);
}