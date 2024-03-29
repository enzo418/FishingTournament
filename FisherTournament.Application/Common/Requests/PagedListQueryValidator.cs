using FluentValidation;

namespace FisherTournament.Application.Common.Requests
{
    public class PagedListQueryValidator<T> : AbstractValidator<T>
        where T : IPagedListQuery
    {
        public PagedListQueryValidator()
        {
            RuleFor(c => c.Page).GreaterThanOrEqualTo(1);
            RuleFor(c => c.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
        }
    }
}