namespace Application.UseCases;

public static class CreateSubject
{
    public record Command(
        string Name) : IRequest<Result<Guid>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50);
        }
    }

    public class Handler(ISubjectsRepository subjectsRepository) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var existsSubject = await subjectsRepository.ExistsByName(request.Name, cancellationToken);
            if (existsSubject)
            {
                return Result<Guid>.Failure($"The subject with name '{request.Name}' already exists!");
            }

            var newSubject = new Subject
            {
                Name = request.Name
            };

            await subjectsRepository.AddAsync(newSubject, cancellationToken);
            await subjectsRepository.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(newSubject.Id);
        }
    }
}