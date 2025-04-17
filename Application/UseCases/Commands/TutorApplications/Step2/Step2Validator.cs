namespace Application.UseCases.Commands.TutorApplications.Step2;

public static class Step2Validator
{
    public static Result ValidateTutorApplicationForCvReview(this TutorApplication tutorApplication, ILogger logger)
    {
        if (tutorApplication.IsRejected)
        {
            logger.LogWarning("[Step2Validator] Tutor Application with ID '{TutorApplicationId}' is already rejected.", tutorApplication.Id);
            return Result.Fail(new Error(ErrorType.Conflict, $"Tutor Application with ID '{tutorApplication.Id}' is already rejected."));
        }

        if (tutorApplication.CurrentStep != TutorApplication.OnboardingStep.CvUnderReview)
        {
            logger.LogError("[Step2Validator] Tutor Application with ID '{TutorApplicationId}' is not in the CV review state. Current step: {CurrentStep}", tutorApplication.Id, tutorApplication.CurrentStep);
            return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{tutorApplication.Id}' is not in the CV review state."));
        }

        if (tutorApplication.Documents.Count != 1)
        {
            logger.LogError("[Step2Validator] Tutor Application with ID '{TutorApplicationId}' has incorrect document count: {DocumentCount}", tutorApplication.Id, tutorApplication.Documents.Count);
            return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{tutorApplication.Id}' has an incorrect number of submitted documents."));
        }

        var cvDocument = tutorApplication.Documents.SingleOrDefault(d => d.DocumentType == TutorDocument.TutorDocumentType.Cv);
        if (cvDocument is null || cvDocument.Status != TutorDocument.TutorDocumentStatus.Pending)
        {
            logger.LogError("[Step2Validator] Tutor Application with ID '{TutorApplicationId}' CV document is missing or not in pending state. CV document: {@CvDocument}", tutorApplication.Id, cvDocument);
            return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{tutorApplication.Id}' CV document is missing or not in pending state."));
        }

        return Result.Ok();
    }
}