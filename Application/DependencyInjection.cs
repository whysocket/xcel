using Application.Implementations;
using Application.Interfaces;
using Application.UseCases.Commands.Availability;
using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step1;
using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3.BookInterview;
using Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;
using Application.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.AfterInterview;
using Application.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.BookInterview;
using Application.UseCases.Queries;
using Application.UseCases.Queries.Availability;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Common;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;
using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;
using Application.UseCases.Queries.TutorApplicationOnboarding.Reviewer.Step3;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IReviewerAssignmentService, ReviewerAssignmentService>();

        services
            .AddScoped<
                IGetAllSubjectsWithQualificationsQuery,
                GetAllSubjectsWithQualificationsQuery
            >()
            .AddScoped<IApproveInterviewCommand, ApproveInterviewCommand>()
            .AddScoped<IRejectInterviewCommand, RejectInterviewCommand>()
            .AddScoped<IApplicationApproveCvCommand, ApplicationApproveCvCommand>()
            .AddScoped<IApplicationRejectCvCommand, ApplicationRejectCvCommand>()
            .AddScoped<IGetApplicationByIdQuery, GetApplicationByIdQuery>()
            .AddScoped<ITutorApplicationSubmitCommand, TutorApplicationSubmitCommand>()
            .AddScoped<
                IGetApplicationsByOnboardingStepQuery,
                GetApplicationsByOnboardingStepQuery
            >()
            .AddScoped<IGetAvailabilityRulesQuery, GetAvailabilityRulesQuery>()
            .AddScoped<IGetAvailabilitySlotsQuery, GetAvailabilitySlotsQuery>()
            .AddScoped<IGetMyTutorApplicationQuery, GetMyTutorApplicationQuery>()
            .AddScoped<IGetReviewerAssignedInterviewsQuery, GetReviewerAssignedInterviewsQuery>()
            .AddScoped<IGetReviewerAvailabilitySlotsQuery, GetReviewerAvailabilitySlotsQuery>()
            .AddScoped<IApplicantBookInterviewSlotCommand, ApplicantBookInterviewSlotCommand>()
            .AddScoped<
                IReviewerRequestInterviewRescheduleCommand,
                ReviewerRequestInterviewRescheduleCommand
            >()
            .AddScoped<IAddExclusionPeriodCommand, AddExclusionPeriodCommand>()
            .AddScoped<IAddOneOffAvailabilitySlotCommand, AddOneOffAvailabilitySlotCommand>()
            .AddScoped<IDeleteAvailabilityRuleCommand, DeleteAvailabilityRuleCommand>()
            .AddScoped<ISetAvailabilityRulesCommand, SetAvailabilityRulesCommand>()
            .AddScoped<IUpdateAvailabilityRuleCommand, UpdateAvailabilityRuleCommand>();

        return services;
    }
}
