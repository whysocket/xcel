﻿using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.TutorApprovalEmail;

namespace Application.UseCases.Commands.Moderator;

public static class ApproveTutorApplicant
{
    public record Command(Guid TutorId) : IRequest<Result>;

    public class Handler(
        ITutorsRepository tutorsRepository,
        IEmailSender emailSender) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var tutor = await tutorsRepository.GetByIdAsync(request.TutorId, cancellationToken);
            if (tutor == null)
            {
                return Result.Fail(new Error(ErrorType.NotFound, $"Tutor with ID '{request.TutorId}' not found."));
            }

            if (tutor.Status != Tutor.TutorStatus.Pending)
            {
                return Result.Fail(new Error(ErrorType.Validation, $"Tutor with ID '{request.TutorId}' is not in a pending state."));
            }

            tutor.Status = Tutor.TutorStatus.Approved;
            tutor.CurrentStep = Tutor.OnboardingStep.ProfileValidated;

            tutorsRepository.Update(tutor);
            await tutorsRepository.SaveChangesAsync(cancellationToken);

            var emailPayload = new EmailPayload<TutorApprovalEmailData>(
                "Your Tutor Application Status",
                tutor.Person.EmailAddress,
                new TutorApprovalEmailData(tutor.Person.FirstName, tutor.Person.LastName));

            await emailSender.SendEmailAsync(emailPayload, cancellationToken);

            return Result.Ok();
        }
    }
}