using FisherTournament.Application.Common.Resources;
using FisherTournament.Application.Tournaments.Commands.AddCompetitions;
using FisherTournament.Domain.CompetitionAggregate.ValueObjects;

namespace FisherTournament.IntegrationTests.Tournaments.Commands
{
	public class AddCompetitionsHandlerTest : BaseUseCaseTest
	{
		public AddCompetitionsHandlerTest(UseCaseTestsFixture fixture) : base(fixture)
		{ }

		[Fact]
		public async Task Handler_Should_AddCompetitions()
		{
			// 
			using var context = _fixture.TournamentContext;
			var tournament = await context.WithAsync(Tournament.Create(
				"Test Tournament",
				_fixture.DateTimeProvider.Now.AddDays(1),
				_fixture.DateTimeProvider.Now.AddDays(2)));
			var fisher = await context.WithFisherAsync("First", "Last");

			var command = new AddCompetitionsCommand(tournament.Id.ToString(), new List<AddCompetitionCommand>
			{
				new AddCompetitionCommand
				{
					StartDateTime = _fixture.DateTimeProvider.Now.AddDays(1),
					Location = new CompetitionLocationResource(
						"Test City 1",
						"Test State 1",
						"Test Country 1",
						"Test Place 1")
				},
				new AddCompetitionCommand
				{
					StartDateTime = _fixture.DateTimeProvider.Now.AddDays(2),
					Location = new CompetitionLocationResource(
						City: "Test City 2",
						State: "Test State 2",
						Country: "Test Country 2",
						Place: "Test Place 2")
				}
			});

			await context.SaveChangesAndClear();

			// 
			var result = await _fixture.SendAsync(command);
			var tournamentWithCompetitions = await context.FindAsync<Tournament>(tournament.Id);
			var competitions = await context.FindAllAsync<Competition, CompetitionId>(result.Value.Select(c => CompetitionId.Create(c.Id).Value));

			// Assert correct response
			result.IsError.Should().BeFalse($"because the command is valid ({result.Errors.First().Description})");
			tournamentWithCompetitions.Should().NotBeNull();
			tournamentWithCompetitions!.CompetitionsIds.Should()
				.HaveCount(2)
				.And
				.Contain(c => c.ToString() == result.Value.First().Id)
				.And
				.Contain(c => c.ToString() == result.Value.Last().Id);

			competitions.Should().HaveCount(2);

			// Assert correct competition data was stored
			competitions.FirstOrDefault(c => c.StartDateTime == command.Competitions.First().StartDateTime
											 && c.Location.City == command.Competitions.First().Location.City).Should().NotBeNull();

			competitions.FirstOrDefault(c => c.StartDateTime == command.Competitions.Last().StartDateTime
												&& c.Location.City == command.Competitions.Last().Location.City).Should().NotBeNull();
		}
	}
}