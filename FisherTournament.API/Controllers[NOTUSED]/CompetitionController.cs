using FisherTournament.Api.Controllers;
using FisherTournament.Application.Competitions.Commands.AddScore;
using FisherTournament.Application.Competitions.Queries.GetLeaderBoard;
using FisherTournament.Application.Tournaments.Commands.AddCompetitions;
using FisherTournament.Contracts.Competitions;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FisherTournament.API.Controllers;

[Route("tournaments/{tournamentId}/competitions")]
public class CompetitionController : ApiController
{

	private readonly ISender _sender;
	private readonly IMapper _mapper;

	public CompetitionController(ISender sender, IMapper mapper)
	{
		_sender = sender;
		_mapper = mapper;
	}

	[HttpPost]
	public async Task<IActionResult> Create(
		AddCompetitionsRequest request,
		string tournamentId)
	{
		var command = _mapper.Map<AddCompetitionsCommand>((request, tournamentId));

		var response = await _sender.Send(command);

		return response.Match(
		   value => Ok(_mapper.Map<AddCompetitionsResponse>(value)),
		   errors => Problem(errors)
	   );
	}

	[HttpPost("{competitionId}/scores")]
	public async Task<IActionResult> AddScore(
		AddScoreRequest request,
		[FromRoute] string competitionId)
	{
		var command = _mapper.Map<AddScoreCommand>((request, competitionId));
		var response = await _sender.Send(command);
		return response.Match(
			onValue: _ => Ok(),
			onError: errors => Problem(errors)
		);
	}

	[HttpGet("{competitionId}/Leaderboard")]
	public async Task<IActionResult> GetLeaderboard(string competitionId)
	{
		var response = await _sender.Send(new GetCompetitionLeaderBoardQuery(competitionId));

		return response.Match(
			onValue: value => Ok(value),
			onError: errors => Problem(errors)
		);
	}
}
