using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Account.Commands.Interfaces;

namespace Xcel.Services.Auth.Features.Account.Commands.Implementations;

internal static class DeleteAccountCommandErrors
{
    internal static Error PersonNotFound(Guid personId) =>
        new(ErrorType.NotFound, $"The person with ID '{personId}' does not exist.");
}

internal sealed class DeleteAccountCommand(
    IPersonsRepository personRepository,
    ILogger<DeleteAccountCommand> logger
) : IDeleteAccountCommand
{
    private const string ServiceName = "[DeleteAccountCommand]";

    public async Task<Result> ExecuteAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation($"{ServiceName} - Attempting to soft-delete person: {personId}");

        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null)
        {
            logger.LogWarning($"{ServiceName} - Person not found: {personId}");
            return Result.Fail(DeleteAccountCommandErrors.PersonNotFound(personId));
        }

        person.IsDeleted = true;

        personRepository.Update(person);
        await personRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Person marked as deleted: {personId}");
        return Result.Ok();
    }
}
