using ErrorOr;
using FisherTournament.Application.Common.Persistence;
using FisherTournament.Domain.CompetitionAggregate.ValueObjects;
using FisherTournament.Domain.FisherAggregate.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FisherTournament.Domain.Common.Errors;
using FisherTournament.ReadModels.Models;
using FisherTournament.ReadModels.Persistence;
using FisherTournament.Application.Common.Instrumentation;

namespace FisherTournament.Application.Competitions.Queries.GetLeaderBoard;

public record struct GetCompetitionLeaderBoardQuery(string CompetitionId)
     : IRequest<ErrorOr<IEnumerable<LeaderBoardCategory>>>;

public record struct LeaderBoardItem(FisherId FisherId, string Name, int Position, int TotalScore);

public record struct LeaderBoardCategory(string Name, string Id, IEnumerable<LeaderBoardItem> LeaderBoard);

public record FishersWithName(FisherId Id, string Name);

public class GetCompetitionLeaderBoardQueryHandler
 : IRequestHandler<GetCompetitionLeaderBoardQuery, ErrorOr<IEnumerable<LeaderBoardCategory>>>
{
    private readonly ITournamentFisherDbContext _context;
    private readonly ILeaderBoardRepository _leaderBoardRepository;
    private readonly ApplicationInstrumentation _instrumentation;

    public GetCompetitionLeaderBoardQueryHandler(ITournamentFisherDbContext context,
                                                 ILeaderBoardRepository leaderBoardRepository,
                                                 ApplicationInstrumentation instrumentation)
    {
        _context = context;
        _leaderBoardRepository = leaderBoardRepository;
        _instrumentation = instrumentation;
    }

    public async Task<ErrorOr<IEnumerable<LeaderBoardCategory>>> Handle(
        GetCompetitionLeaderBoardQuery request,
        CancellationToken cancellationToken)
    {
        ErrorOr<CompetitionId> competitionId = CompetitionId.Create(request.CompetitionId);

        if (competitionId.IsError)
        {
            return Errors.Id.NotValidWithProperty(nameof(request.CompetitionId));
        }

        IEnumerable<LeaderboardCompetitionCategoryItem> leaderBoard;
        using (_instrumentation.ActivitySource.StartActivity("GetLeaderBoard"))
        {
            leaderBoard = _leaderBoardRepository.GetCompetitionLeaderBoard(competitionId.Value);
        }

        // OPTIMIZE: Cache fisher names instead of a join? 
        List<FishersWithName> fishersNames = new();
        using (_instrumentation.ActivitySource.StartActivity("SelectNames"))
        {
            fishersNames = await _context.Fishers
               .Where(f => leaderBoard.Select(l => l.FisherId).Contains(f.Id))
               .Select(f => new FishersWithName(f.Id, f.Name))
               .ToListAsync(cancellationToken);
        }

        // FIXME: we need the categories names!!! category.key is using the id

        var categories = leaderBoard
            .GroupBy(r => r.CategoryId)
            .Select(category => new LeaderBoardCategory(
                    category.Key,
                    category.First().CategoryId,
                    category.Select(r =>
                    {
                        var fisher = fishersNames.FirstOrDefault(f => f.Id == r.FisherId);

                        return new LeaderBoardItem(
                            r.FisherId,
                            fisher?.Name ?? string.Empty,
                            r.Position,
                            r.Score
                        );
                    })
                )
            );

        return new List<LeaderBoardCategory>(categories);
    }
}