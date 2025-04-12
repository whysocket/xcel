using Microsoft.Extensions.Logging;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.ReviewerInterviewDatesEmail;
using Xcel.Services.Email.Templates.TutorApplicantProposedDatesEmail;

namespace Application.UseCases.Commands.TutorApplications.Step3;

public static class TutorApplicationApplicantProposeInterviewDates
{
    public record Command(Guid TutorApplicationId, List<DateTime> ProposedDates, string? Observations) : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider timeProvider)
        {
            RuleFor(x => x.ProposedDates)
                .NotEmpty().WithMessage("At least one interview date must be proposed.")
                .Must(dates => dates.Count <= 3)
                    .WithMessage("You can propose up to 3 interview dates.")
                .Must(dates => dates.All(d => d > timeProvider.GetUtcNow()))
                    .WithMessage("All proposed dates must be in the future.");
        }
    }

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IEmailSender emailSender,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[ProposeInterviewDates] Tutor {TutorApplicationId} proposing {Count} date(s)",
                request.TutorApplicationId, request.ProposedDates.Count);

            var application = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (application?.Interview is null)
            {
                logger.LogWarning("[ProposeInterviewDates] Application or interview not found for ID: {TutorApplicationId}", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound, "Tutor application or interview not found."));
            }

            var interview = application.Interview;

            if (interview.Status is not
                (TutorApplicationInterview.InterviewStatus.AwaitingTutorApplicantProposedDates or
                 TutorApplicationInterview.InterviewStatus.AwaitingTutorApplicantConfirmation))
            {
                logger.LogWarning("[ProposeInterviewDates] Invalid interview status: {Status} for TutorApplicationId: {TutorApplicationId}",
                    interview.Status, request.TutorApplicationId);

                return Result.Fail(new Error(ErrorType.Validation, "Interview is not ready for new proposed dates."));
            }

            interview.ProposedDates = request.ProposedDates;
            interview.Observations = request.Observations;
            interview.Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation;

            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[ProposeInterviewDates] Interview dates updated and status set to AwaitingReviewerConfirmation for {TutorApplicationId}",
                request.TutorApplicationId);

            var reviewer = interview.Reviewer;

            var emailPayload = new EmailPayload<ReviewerInterviewDatesEmailData>(
                "A tutor has proposed interview dates",
                reviewer.EmailAddress,
                new(application.Person.FullName, request.ProposedDates, request.Observations));

            var emailResult = await emailSender.SendEmailAsync(emailPayload, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[ProposeInterviewDates] Failed to send email to reviewer: {ReviewerEmail}", reviewer.EmailAddress);
                return Result.Fail(Errors.Unexpected);
            }
        
            logger.LogInformation("[ProposeInterviewDates] Email sent to reviewer: {ReviewerEmail}", reviewer.EmailAddress);

            return Result.Ok();
        }
    }
}

public static class TutorApplicationReviewerProposeInterviewDates
{
    public record Command(Guid TutorApplicationId, List<DateTime> ProposedDates, string? Observations) : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider timeProvider)
        {
            RuleFor(x => x.ProposedDates)
                .NotEmpty().WithMessage("At least one interview date must be proposed.")
                .Must(dates => dates.Count <= 3)
                    .WithMessage("You can propose up to 3 interview dates.")
                .Must(dates => dates.All(d => d > timeProvider.GetUtcNow()))
                    .WithMessage("All proposed dates must be in the future.");
        }
    }

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IEmailSender emailSender,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[ReviewerProposeInterviewDates] Reviewer proposing {Count} new date(s) for TutorApplicationId: {TutorApplicationId}",
                request.ProposedDates.Count, request.TutorApplicationId);

            var application = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (application?.Interview is null)
            {
                logger.LogWarning("[ReviewerProposeInterviewDates] Application or interview not found for ID: {TutorApplicationId}", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound, "Tutor application or interview not found."));
            }

            var interview = application.Interview;
            if (interview.Status != TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation)
            {
                logger.LogWarning("[ReviewerProposeInterviewDates] Interview not awaiting reviewer input. Current status: {Status}", interview.Status);
                return Result.Fail(new Error(ErrorType.Validation, "Interview is not in a state for reviewer to propose new dates."));
            }

            interview.ProposedDates = request.ProposedDates;
            interview.Observations = request.Observations;
            interview.Status = TutorApplicationInterview.InterviewStatus.AwaitingTutorApplicantConfirmation;

            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[ReviewerProposeInterviewDates] Interview updated with new dates and status AwaitingTutorApplicantConfirmation for {TutorApplicationId}",
                request.TutorApplicationId);

            var applicant = application.Person;

            var emailPayload = new EmailPayload<TutorApplicantProposedDatesEmailData>(
                 "Your reviewer has proposed new interview dates",
                applicant.EmailAddress,
                new(applicant.FirstName, applicant.LastName, request.ProposedDates, request.Observations));

            var emailResult = await emailSender.SendEmailAsync(emailPayload, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[ReviewerProposeInterviewDates] Failed to send email to tutor: {TutorEmail}", applicant.EmailAddress);
                return Result.Fail(Errors.Unexpected);
            }
            
            logger.LogInformation("[ReviewerProposeInterviewDates] Email sent to tutor: {TutorEmail}", applicant.EmailAddress);

            return Result.Ok();
        }
    }
}

