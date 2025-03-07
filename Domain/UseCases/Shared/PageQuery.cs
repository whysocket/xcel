using Domain.Interfaces.Repositories.Shared;
using FluentValidation;

namespace Domain.UseCases.Shared;

public static class PageQuery
{
    public interface IPageQuery
    {
        PageRequest PageRequest { get; set; }
    }

    public class Validator<T> : AbstractValidator<T> where T : IPageQuery
    {
        public Validator() : this(100) { }

        public Validator(int maxPageSize)
        {
            RuleFor(c => c.PageRequest.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(c => c.PageRequest.PageSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(maxPageSize);
        }
    }
}