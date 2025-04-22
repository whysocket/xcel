using Domain.Constants;
using Xcel.Services.Auth.Public;

namespace Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Common;

public interface IGetMyTutorApplicationQuery
{
    Task<Result<TutorApplication>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
}

internal static class GetMyTutorApplicationQueryErrors
{
    internal static Error NotFound(Guid userId) =>
        new(ErrorType.NotFound, $"No tutor application found for user ID '{userId}'.");

    internal static Error AlreadyHasRole(Guid userId) =>
        new(ErrorType.Conflict, $"User '{userId}' already has a role and cannot access the onboarding application.");
}

internal sealed class GetMyTutorApplicationQuery(
    ITutorApplicationsRepository repository,
    IAuthServiceSdk authService,
    ILogger<GetMyTutorApplicationQuery> logger) : IGetMyTutorApplicationQuery
{
    private const string ServiceName = "[GetMyTutorApplicationQuery]";

    public async Task<Result<TutorApplication>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Service} Checking roles for user {UserId}", ServiceName, userId);

        var userRolesResult = await authService.GetRolesByPersonIdAsync(userId, cancellationToken);
        if (userRolesResult.IsFailure)
        {
            logger.LogError("{Service} Failed to fetch roles for user {UserId}: {Errors}", ServiceName, userId, userRolesResult.Errors);
            return Result.Fail<TutorApplication>(userRolesResult.Errors);
        }

        if (userRolesResult.Value.Any(role => UserRoles.All.Contains(role.Name)))
        {
            logger.LogWarning("{Service} User {UserId} has a disallowed role", ServiceName, userId);
            return Result.Fail<TutorApplication>(GetMyTutorApplicationQueryErrors.AlreadyHasRole(userId));
        }

        logger.LogInformation("{Service} Retrieving tutor application for user {UserId}", ServiceName, userId);

        var application = await repository.GetByUserIdAsync(userId, cancellationToken);

        if (application is null)
        {
            logger.LogWarning("{Service} No application found for user {UserId}", ServiceName, userId);
            return Result.Fail<TutorApplication>(GetMyTutorApplicationQueryErrors.NotFound(userId));
        }

        logger.LogInformation("{Service} Application {ApplicationId} found for user {UserId}", ServiceName, application.Id, userId);
        return Result.Ok(application);
    }
}
