using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using FluentValidation;
using MediatR;

namespace Domain.UseCases;

public static class CreateSubject
{
    public class Command : AbstractValidator<Command>, IRequest<Result<Guid>>
    {
        public Command()
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50);
        }

        public required string Name { get; set; }
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