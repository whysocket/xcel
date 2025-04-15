using Microsoft.Extensions.Logging;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;

namespace Application.UseCases.Commands.TutorApplications.Step3;

public static class TutorApplicationScheduleInterview
{
    public static class Errors
    {
        public static Error TutorApplicationNotFound =
            new(ErrorType.NotFound, "Tutor application or interview not found.");

        public static Error InterviewNotInCorrectState =
            new(ErrorType.Validation, "Interview is not in a confirmable state.");

        public static Error SelectedDateNotValid =
            new(ErrorType.Validation, "Selected date must be one of the proposed options.");

        public static Error EmailSendFailed =
            new(ErrorType.Unexpected, "Failed to send interview confirmation email.");
    }

    public enum Party
    {
        Reviewer,
        Applicant
    }

    public record Command(Guid TutorApplicationId, DateTime SelectedDateUtc, Party Party) : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SelectedDateUtc)
                .NotEmpty().WithMessage("Selected date is required.");
        }
    }

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        private const string ReviewerConfirmationSubject = "Your interview has been confirmed by the reviewer";
        private const string ApplicantConfirmationSubject = "Your interview has been confirmed by the applicant";

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[ScheduleInterview] Confirming interview date for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

            var application = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (application?.Interview is null)
            {
                logger.LogWarning("[ScheduleInterview] Application or interview not found for ID: {TutorApplicationId}", request.TutorApplicationId);
                return Result.Fail(Errors.TutorApplicationNotFound);
            }

            var interview = application.Interview;

            if (!IsConfirmable(interview.Status))
            {
                logger.LogWarning("[ScheduleInterview] Interview not in a confirmable state. Current status: {Status}", interview.Status);
                return Result.Fail(Errors.InterviewNotInCorrectState);
            }

            var expectedParty = GetExpectedConfirmingParty(interview.Status);
            if (expectedParty != request.Party)
            {
                logger.LogWarning("[ScheduleInterview] Invalid confirming party. Expected: {Expected}, Actual: {Actual}", expectedParty, request.Party);
                return Result.Fail(Errors.InterviewNotInCorrectState);
            }

            if (!interview.ProposedDates.Contains(request.SelectedDateUtc))
            {
                logger.LogWarning("[ScheduleInterview] Selected date is not among the proposed options.");
                return Result.Fail(Errors.SelectedDateNotValid);
            }

            UpdateInterview(interview, application.ApplicantId, request);

            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[ScheduleInterview] Interview scheduled at {Date} by {Party} (UserId: {UserId}) for TutorApplicationId: {TutorApplicationId}",
                request.SelectedDateUtc, request.Party, interview.ConfirmedBy, request.TutorApplicationId);

            var subject = GetConfirmationEmailSubject(request.Party);
            var recipients = new[] { application.Applicant.EmailAddress, interview.Reviewer.EmailAddress };
            var emailPayload = new EmailPayload<InterviewScheduledEmail>(
                subject,
                recipients,
                new(
                    application.Applicant.FullName,
                    interview.Reviewer.FullName,
                    interview.ScheduledAt!.Value)
            );

            var emailResult = await emailService.SendEmailAsync(emailPayload, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[ScheduleInterview] Failed to send confirmation email to {Recipients}, Errors: {@Errors}", string.Join(", ", recipients), emailResult.Errors);
                return Result.Fail(Errors.EmailSendFailed);
            }

            logger.LogInformation("[ScheduleInterview] Confirmation email sent to {Recipients}", string.Join(", ", recipients));

            return Result.Ok();
        }

        private static bool IsConfirmable(TutorApplicationInterview.InterviewStatus status)
        {
            return status is TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation or
                   TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation;
        }

        private static Party? GetExpectedConfirmingParty(TutorApplicationInterview.InterviewStatus status)
        {
            return status switch
            {
                TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation => Party.Reviewer,
                TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation => Party.Applicant,
                _ => null
            };
        }

        private static void UpdateInterview(TutorApplicationInterview interview, Guid applicantId, Command request)
        {
            interview.ScheduledAt = request.SelectedDateUtc;
            interview.Status = TutorApplicationInterview.InterviewStatus.Confirmed;
            interview.ConfirmedBy = request.Party switch
            {
                Party.Reviewer => interview.ReviewerId,
                Party.Applicant => applicantId,
                _ => throw new InvalidOperationException("Unexpected party value")
            };
        }

        private static string GetConfirmationEmailSubject(Party party)
        {
            return party == Party.Reviewer ? ReviewerConfirmationSubject : ApplicantConfirmationSubject;
        }
    }
}