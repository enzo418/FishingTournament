using FluentValidation;

namespace FisherTournament.Application.Competitions.Commands.AddScore;

public class AddScoreCommandValidator : AbstractValidator<AddScoreCommand>
{
    public AddScoreCommandValidator()
    {
        RuleFor(c => c.CompetitionId).NotEmpty();
        RuleFor(c => c.FisherId).NotEmpty();
        RuleFor(c => c.Score)
            .GreaterThan(0);
    }
}