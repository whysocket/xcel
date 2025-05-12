using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

internal class TutorApplicationsRepository(AppDbContext dbContext)
    : GenericRepository<TutorApplication>(dbContext),
        ITutorApplicationsRepository
{
    public async Task<TutorApplicationInterview?> GetBookingAtSlotAsync(
        Guid reviewerId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default
    )
    {
        // Find any interview for this reviewer that is Confirmed and overlaps with the requested slot
        return await DbContext
            .Set<TutorApplicationInterview>()
            .Where(i =>
                i.ReviewerId == reviewerId
                && i.Status == TutorApplicationInterview.InterviewStatus.Confirmed
                // Check for overlap: proposed slot start < existing end AND existing start < proposed slot end
                && startUtc < i.ScheduledAtUtc!.Value.AddMinutes(30) // Assuming booked slots are 30 mins as per command logic
                && i.ScheduledAtUtc.Value < endUtc
            )
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TutorApplicationInterview>> GetScheduledInterviewsForOwnerAsync(
        Guid ownerId,
        DateTime afterUtc,
        CancellationToken cancellationToken = default
    )
    {
        // Implementation to get confirmed interviews for an owner after a certain time
        return await DbContext
            .Set<TutorApplicationInterview>()
            .Where(i =>
                i.ReviewerId == ownerId
                && i.Status == TutorApplicationInterview.InterviewStatus.Confirmed
                && i.ScheduledAtUtc > afterUtc
            )
            .ToListAsync(cancellationToken);
    }

    public async Task<
        List<TutorApplication>
    > GetAllWithDocumentsAndApplicantAndInterviewByOnboardingStep(
        TutorApplication.OnboardingStep onboardingStep,
        CancellationToken cancellationToken = default
    )
    {
        return await DbContext
            .Set<TutorApplication>()
            .Include(t => t.Documents)
            .Include(t => t.Applicant)
            .Include(t => t.Interview)
            .ThenInclude(i => i!.Reviewer)
            .Where(t => t.CurrentStep == onboardingStep)
            .ToListAsync(cancellationToken);
    }

    public async Task<TutorApplication?> GetByIdWithDocumentsAndApplicantAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    )
    {
        return await DbContext
            .Set<TutorApplication>()
            .Include(a => a.Applicant)
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == tutorApplicationId, cancellationToken);
    }

    public Task<TutorApplication?> GetByIdWithInterviewAndPeopleAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    )
    {
        return DbContext
            .Set<TutorApplication>()
            .Include(t => t.Applicant)
            .Include(t => t.Documents)
            .Include(t => t.Interview)
            .ThenInclude(i => i!.Reviewer)
            .FirstOrDefaultAsync(t => t.Id == tutorApplicationId, cancellationToken);
    }

    public Task<TutorApplication?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return DbContext
            .Set<TutorApplication>()
            .Include(t => t.Documents)
            .Include(t => t.Interview)
            .ThenInclude(i => i!.Reviewer)
            .FirstOrDefaultAsync(t => t.ApplicantId == userId, cancellationToken);
    }

    public Task<List<TutorApplication>> GetAllByReviewerIdAsync(
        Guid reviewerId,
        CancellationToken cancellationToken
    )
    {
        return DbContext
            .Set<TutorApplication>()
            .Include(a => a.Applicant)
            .Include(a => a.Interview)
            .ThenInclude(i => i!.Reviewer)
            .Where(a => a.Interview != null && a.Interview.ReviewerId == reviewerId)
            .ToListAsync(cancellationToken);
    }
}
