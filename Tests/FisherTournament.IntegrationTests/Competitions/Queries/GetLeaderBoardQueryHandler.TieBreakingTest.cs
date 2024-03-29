﻿using FisherTournament.Application.Competitions.Queries.GetLeaderBoard;
using FisherTournament.Application.LeaderBoard;
using FisherTournament.Domain.CompetitionAggregate.Entities;
using FisherTournament.IntegrationTests.Common;
using FisherTournament.IntegrationTests.Competitions.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FisherTournament.IntegrationTests.Competitions.Queries
{
    public partial class GetLeaderBoardQueryHandlerTest
    {
        [Fact]
        public async Task Handler_Should_ReturnLeaderBoardWithTieBreakingByLargerPiece()
        {
            // Arrange
            using var context = _fixture.TournamentContext;
            var fisher1 = context.PrepareFisher("First1", "Last1", out var _);
            var fisher2 = context.PrepareFisher("First2", "Last2", out var _);
            var fisher3 = context.PrepareFisher("First3", "Last3", out var _);

            var tournament = await TournamentBuilder.Create(context, _fixture.DateTimeProvider)
                .WithName("Test Tournament")
                .WithStartDate(_fixture.DateTimeProvider.Now)
                .WithEndDate(_fixture.DateTimeProvider.Now.AddDays(2))

                .WithCategory("Primary")
                .WithCategory("Secondary")

                .WithInscription(fisher1.Id, 1, "Primary")
                .WithInscription(fisher2.Id, 2, "Primary")
                .WithInscription(fisher3.Id, 3, "Primary")

                .Build(CancellationToken.None);

            var categoryPrimary = tournament.Categories.First(c => c.Name == "Primary");

            var competition = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                .WithScore(fisher1.Id, 50)
                .WithScore(fisher1.Id, 10)

                .WithScore(fisher2.Id, 10)
                .WithScore(fisher2.Id, 20)

                // fisher3 and fisher2 are tied, but fisher3 has a larger piece
                // (25 vs 20) so it's 2° and fisher2 is 3°
                .WithScore(fisher3.Id, 25)
                .WithScore(fisher3.Id, 5)

                .WithN(1)

                .Build());

            int jobsExecuted = await _fixture.ExecutePendingLeaderBoardJobs();

            // Act
            var result = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // Assert
            jobsExecuted.Should().BeGreaterThan(0);

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Should().HaveCount(2);

            result.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(3)
                        .ShouldHavePosition(1, fisher1.Id, 60)
                        .ShouldHavePosition(2, fisher3.Id, 30, TieBreakingReasonGenerator.ByLargerPiece(25))
                        .ShouldHavePosition(3, fisher2.Id, 30, TieBreakingReasonGenerator.ByLargerPiece(20));
        }

        [Fact]
        public async Task Handler_Should_ReturnLeaderBoardWithTripleTieBreakingByLargerPiece()
        {
            // Arrange
            using var context = _fixture.TournamentContext;
            var fisher1 = context.PrepareFisher("First1", "Last1", out var _);
            var fisher2 = context.PrepareFisher("First2", "Last2", out var _);
            var fisher3 = context.PrepareFisher("First3", "Last3", out var _);

            var tournament = await TournamentBuilder.Create(context, _fixture.DateTimeProvider)
                .WithName("Test Tournament")
                .WithStartDate(_fixture.DateTimeProvider.Now)
                .WithEndDate(_fixture.DateTimeProvider.Now.AddDays(2))

                .WithCategory("Primary")
                .WithCategory("Secondary")

                .WithInscription(fisher1.Id, 1, "Primary")
                .WithInscription(fisher2.Id, 2, "Primary")
                .WithInscription(fisher3.Id, 3, "Primary")

                .Build(CancellationToken.None);

            var categoryPrimary = tournament.Categories.First(c => c.Name == "Primary");

            var competition = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                .WithScore(fisher1.Id, 1)
                .WithScore(fisher1.Id, 29) // 1°

                .WithScore(fisher2.Id, 10)
                .WithScore(fisher2.Id, 20) // 3°

                .WithScore(fisher3.Id, 25) // 2°
                .WithScore(fisher3.Id, 5)

                .WithN(1)

                .Build());

            int jobsExecuted = await _fixture.ExecutePendingLeaderBoardJobs();

            // Act
            var result = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // Assert
            jobsExecuted.Should().BeGreaterThan(0);

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Should().HaveCount(2);

            result.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(3)
                        .ShouldHavePosition(1, fisher1.Id, 30, TieBreakingReasonGenerator.ByLargerPiece(29))
                        .ShouldHavePosition(2, fisher3.Id, 30, TieBreakingReasonGenerator.ByLargerPiece(25))
                        .ShouldHavePosition(3, fisher2.Id, 30, TieBreakingReasonGenerator.ByLargerPiece(20));
        }

        [Fact]
        public async Task Handler_Should_ReturnLeaderBoardWithTieBreakingByLargerPiece_WithMultipleCompetitions()
        {
            // Arrange
            using var context = _fixture.TournamentContext;
            var fisher1 = context.PrepareFisher("First1", "Last1", out var _);
            var fisher2 = context.PrepareFisher("First2", "Last2", out var _);
            var fisher3 = context.PrepareFisher("First3", "Last3", out var _);

            var tournament = await TournamentBuilder.Create(context, _fixture.DateTimeProvider)
                .WithName("Test Tournament")
                .WithStartDate(_fixture.DateTimeProvider.Now)
                .WithEndDate(_fixture.DateTimeProvider.Now.AddDays(2))

                .WithCategory("Primary")
                .WithCategory("Secondary")

                .WithInscription(fisher1.Id, 1, "Primary")
                .WithInscription(fisher2.Id, 2, "Primary")
                .WithInscription(fisher3.Id, 3, "Primary")

                .Build(CancellationToken.None);

            var competition1 = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                .WithScore(fisher1.Id, 50)
                .WithScore(fisher1.Id, 10)

                .WithScore(fisher2.Id, 10)
                .WithScore(fisher2.Id, 20)

                // fisher3 and fisher2 are tied, but fisher3 has a larger piece
                // (25 vs 20) so it's 2° and fisher2 is 3°
                .WithScore(fisher3.Id, 25)
                .WithScore(fisher3.Id, 5)

                .WithN(1)

                .Build());


            var categoryPrimary = tournament.Categories.First(c => c.Name == "Primary");

            // competition to verify that the competition leader board is not affected by other competitions
            var competition2 = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                .WithScore(fisher1.Id, 10)

                .WithScore(fisher2.Id, 500)

                .WithScore(fisher3.Id, 50)

                .WithN(2)

                .Build());

            int jobsExecuted = await _fixture.ExecutePendingLeaderBoardJobs();

            // Act
            var result = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition1.Id.ToString()));

            // Assert
            jobsExecuted.Should().BeGreaterThan(0);

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Should().HaveCount(2);

            result.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(3)
                        .ShouldHavePosition(1, fisher1.Id, 60)
                        .ShouldHavePosition(2, fisher3.Id, 30, TieBreakingReasonGenerator.ByLargerPiece(25))
                        .ShouldHavePosition(3, fisher2.Id, 30, TieBreakingReasonGenerator.ByLargerPiece(20));
        }

        [Fact]
        public async Task Handler_Should_ReturnLeaderBoardWithTieBreakingByFirstLargerPiece()
        {
            // Arrange
            using var context = _fixture.TournamentContext;
            var fisher1 = context.PrepareFisher("First1", "Last1", out var _);
            var fisher2 = context.PrepareFisher("First2", "Last2", out var _);
            var fisher3 = context.PrepareFisher("First3", "Last3", out var _);

            var tournament = await TournamentBuilder.Create(context, _fixture.DateTimeProvider)
                .WithName("Test Tournament")
                .WithStartDate(_fixture.DateTimeProvider.Now)
                .WithEndDate(_fixture.DateTimeProvider.Now.AddDays(2))

                .WithCategory("Primary")

                .WithInscription(fisher1.Id, 1, "Primary")
                .WithInscription(fisher2.Id, 2, "Primary")
                .WithInscription(fisher3.Id, 3, "Primary")

                .Build(CancellationToken.None);

            var categoryPrimary = tournament.Categories.First(c => c.Name == "Primary");

            var competition = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                .WithScore(fisher1.Id, 50)
                .WithScore(fisher1.Id, 10)

                .WithScore(fisher2.Id, 6)
                .WithScore(fisher2.Id, 15)
                .WithScore(fisher2.Id, 14)

                // fisher3 and fisher2 are tied and have the same larger piece (15),
                // but fisher2 has a larger first piece (6 vs 5) so it's 2° and fisher3 is 3°
                .WithScore(fisher3.Id, 5)
                .WithScore(fisher3.Id, 15)
                .WithScore(fisher3.Id, 15)

                .WithN(1)

                .Build());

            int jobsExecuted = await _fixture.ExecutePendingLeaderBoardJobs();

            // Act
            var result = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // Assert
            jobsExecuted.Should().BeGreaterThan(0);

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Should().HaveCount(2);

            result.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(3)
                        .ShouldHavePosition(1, fisher1.Id, 60)
                        .ShouldHavePosition(2, fisher2.Id, 35, TieBreakingReasonGenerator.ByNthLargerPiece(1, 6))
                        .ShouldHavePosition(3, fisher3.Id, 35, TieBreakingReasonGenerator.ByNthLargerPiece(1, 5));
        }

        [Fact]
        public async Task Handler_Should_ReturnLeaderBoardWithMultiTieBreakingBySecondLargerPiece()
        {
            // Arrange
            using var context = _fixture.TournamentContext;
            var fisher1 = context.PrepareFisher("First1", "Last1", out var _);
            var fisher2 = context.PrepareFisher("First2", "Last2", out var _);
            var fisher3 = context.PrepareFisher("First3", "Last3", out var _);

            var fisher4 = context.PrepareFisher("First4", "Last4", out var _);
            var fisher5 = context.PrepareFisher("First5", "Last5", out var _);

            var tournament = await TournamentBuilder.Create(context, _fixture.DateTimeProvider)
                .WithName("Test Tournament")
                .WithStartDate(_fixture.DateTimeProvider.Now)
                .WithEndDate(_fixture.DateTimeProvider.Now.AddDays(2))

                .WithCategory("Primary")

                .WithInscription(fisher1.Id, 1, "Primary")
                .WithInscription(fisher2.Id, 2, "Primary")
                .WithInscription(fisher3.Id, 3, "Primary")
                .WithInscription(fisher4.Id, 4, "Primary")
                .WithInscription(fisher5.Id, 5, "Primary")

                .Build(CancellationToken.None);

            var categoryPrimary = tournament.Categories.First(c => c.Name == "Primary");

            var competition = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                // same larger piece
                .WithScore(fisher1.Id, 15)
                .WithScore(fisher2.Id, 15)
                .WithScore(fisher3.Id, 15)
                .WithScore(fisher4.Id, 15)
                .WithScore(fisher5.Id, 15)

                // 1 - break tie by second larger piece
                .WithScore(fisher1.Id, 10) // 1°
                .WithScore(fisher1.Id, 10)

                .WithScore(fisher2.Id, 6) // 2°
                .WithScore(fisher2.Id, 14)

                .WithScore(fisher3.Id, 5) // 3°
                .WithScore(fisher3.Id, 15)

                // 2 - break tie by third larger piece
                .WithScore(fisher4.Id, 4)
                .WithScore(fisher5.Id, 4)

                .WithScore(fisher4.Id, 12) // 4°
                .WithScore(fisher5.Id, 10) // 5°

                .WithScore(fisher4.Id, 4) // total == 35
                .WithScore(fisher5.Id, 6) // total == 33

                .WithN(1)

                .Build());

            int jobsExecuted = await _fixture.ExecutePendingLeaderBoardJobs();

            // Act
            var result = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // Assert
            jobsExecuted.Should().BeGreaterThan(0);

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Should().HaveCount(2);

            result.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(5)
                        .ShouldHavePosition(1, fisher1.Id, 35, TieBreakingReasonGenerator.ByNthLargerPiece(2, 10))
                        .ShouldHavePosition(2, fisher2.Id, 35, TieBreakingReasonGenerator.ByNthLargerPiece(2, 6))
                        .ShouldHavePosition(3, fisher3.Id, 35, TieBreakingReasonGenerator.ByNthLargerPiece(2, 5))
                        .ShouldHavePosition(4, fisher4.Id, 35, TieBreakingReasonGenerator.ByNthLargerPiece(3, 12))
                        .ShouldHavePosition(5, fisher5.Id, 35, TieBreakingReasonGenerator.ByNthLargerPiece(3, 10));
        }

        /// <summary>
        /// This test verifies that the system updates the leaderboard sucessfully even when it applies a default tie breaking.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Handler_Should_ReturnLeaderBoardDefaultWithTieBreaking()
        {
            // Arrange
            using var context = _fixture.TournamentContext;
            var fisher1 = context.PrepareFisher("First1", "Last1", out var _);
            var fisher2 = context.PrepareFisher("First2", "Last2", out var _);
            var fisher3 = context.PrepareFisher("First3", "Last3", out var _);

            var tournament = await TournamentBuilder.Create(context, _fixture.DateTimeProvider)
                .WithName("Test Tournament")
                .WithStartDate(_fixture.DateTimeProvider.Now)
                .WithEndDate(_fixture.DateTimeProvider.Now.AddDays(2))

                .WithCategory("Primary")

                .WithInscription(fisher1.Id, 1, "Primary")
                .WithInscription(fisher2.Id, 2, "Primary")
                .WithInscription(fisher3.Id, 3, "Primary")

                .Build(CancellationToken.None);

            var categoryPrimary = tournament.Categories.First(c => c.Name == "Primary");

            var competition = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                .WithScore(fisher1.Id, 6)
                .WithScore(fisher2.Id, 6)
                .WithScore(fisher3.Id, 6)

                .WithScore(fisher1.Id, 10)
                .WithScore(fisher2.Id, 10)
                .WithScore(fisher3.Id, 10)

                // Default to some other tie breaking

                .WithN(1)

                .Build());

            int jobsExecuted = await _fixture.ExecutePendingLeaderBoardJobs();

            // Act
            var result = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // Assert
            jobsExecuted.Should().BeGreaterThan(0);

            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result.Value!.Should().HaveCount(2);

            result.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(3);

            var leaderboard = result.Value.First(c => c.Id == categoryPrimary.Id);

            leaderboard.LeaderBoard.ElementAt(0).Position.Should().Be(1);
            leaderboard.LeaderBoard.ElementAt(1).Position.Should().Be(2);
            leaderboard.LeaderBoard.ElementAt(2).Position.Should().Be(3);

            leaderboard.LeaderBoard.ElementAt(0).TieBreakingReason.Should().Be(TieBreakingReasonGenerator.ByDefault());
            leaderboard.LeaderBoard.ElementAt(1).TieBreakingReason.Should().Be(TieBreakingReasonGenerator.ByDefault());
            leaderboard.LeaderBoard.ElementAt(2).TieBreakingReason.Should().Be(TieBreakingReasonGenerator.ByDefault());
        }



        /// <summary>
        /// This test verifies that the tie breaking reason is updated when the leaderboard is updated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Handler_Should_ReturnLeaderBoardWithTieBreakingReasonUpdated()
        {
            // Arrange
            using var context = _fixture.TournamentContext;

            var fisher1 = context.PrepareFisher("First1", "Last1", out var _);
            var fisher2 = context.PrepareFisher("First2", "Last2", out var _);

            var tournament = await TournamentBuilder.Create(context, _fixture.DateTimeProvider)
                .WithName("Test Tournament")
                .WithStartDate(_fixture.DateTimeProvider.Now)
                .WithEndDate(_fixture.DateTimeProvider.Now.AddDays(2))

                .WithCategory("Primary")
                .WithCategory("Secondary")

                .WithInscription(fisher1.Id, 1, "Primary")
                .WithInscription(fisher2.Id, 2, "Primary")

                .Build(CancellationToken.None);

            var cmp = await context.WithAsync(CompetitionBuilder.Create(_fixture.DateTimeProvider)
                .WithTournament(tournament.Id)
                .WithLocation(Location.Create("Test City", "Test State", "Test Country", "Test Place"))

                .WithScore(fisher1.Id, 5)

                .WithScore(fisher2.Id, 5)

                .WithN(1)

                .Build());


            var categoryPrimary = tournament.Categories.First(c => c.Name == "Primary");

            var competition = await context.Competitions.Where(c => c.Id == cmp.Id).FirstAsync(); // Track

            // job1
            int jobsExecuted1 = await _fixture.ExecutePendingLeaderBoardJobs();
            var result1 = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // job1 - Assert
            jobsExecuted1.Should().BeGreaterThan(0);

            result1.IsError.Should().BeFalse();
            result1.Value.Should().NotBeNull();
            result1.Value!.Should().HaveCount(2);

            result1.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(2);

            var leaderboard = result1.Value.First(c => c.Id == categoryPrimary.Id).LeaderBoard;
            leaderboard.Should().Contain(f => f.FisherId == fisher1.Id.ToString() && f.TieBreakingReason == TieBreakingReasonGenerator.ByDefault());
            leaderboard.Should().Contain(f => f.FisherId == fisher2.Id.ToString() && f.TieBreakingReason == TieBreakingReasonGenerator.ByDefault());

            // job2
            competition.AddScore(fisher1.Id, 3, _fixture.DateTimeProvider);
            await context.SaveChangesAsync(CancellationToken.None);
            int jobsExecuted2 = await _fixture.ExecutePendingLeaderBoardJobs();

            competition.AddScore(fisher2.Id, 1, _fixture.DateTimeProvider);
            await context.SaveChangesAsync(CancellationToken.None);
            int jobsExecuted3 = await _fixture.ExecutePendingLeaderBoardJobs();

            var result2 = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // job2 - Assert
            jobsExecuted2.Should().BeGreaterThan(0);
            jobsExecuted3.Should().BeGreaterThan(0);

            result2.IsError.Should().BeFalse();
            result2.Value.Should().NotBeNull();
            result2.Value!.Should().HaveCount(2);

            result2.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(2)
                        .ShouldHavePosition(1, fisher1.Id, 8, null)
                        .ShouldHavePosition(2, fisher2.Id, 6, null);

            // job3
            competition.AddScore(fisher1.Id, 1, _fixture.DateTimeProvider);
            await context.SaveChangesAsync(CancellationToken.None);
            int jobsExecuted4 = await _fixture.ExecutePendingLeaderBoardJobs();

            competition.AddScore(fisher2.Id, 3, _fixture.DateTimeProvider);
            await context.SaveChangesAsync(CancellationToken.None);
            int jobsExecuted5 = await _fixture.ExecutePendingLeaderBoardJobs();

            var result3 = await _fixture.SendAsync(new GetCompetitionLeaderBoardQuery(competition.Id.ToString()));

            // job3 - Assert
            jobsExecuted4.Should().BeGreaterThan(0);
            jobsExecuted5.Should().BeGreaterThan(0);

            result3.IsError.Should().BeFalse();
            result3.Value.Should().NotBeNull();
            result3.Value!.Should().HaveCount(2);

            result3.Value.ShouldHaveCategoryLeaderBoard("Primary", categoryPrimary.Id)
                        .ShouldHaveNPositions(2)
                        .ShouldHavePosition(1, fisher1.Id, 9, TieBreakingReasonGenerator.ByNthLargerPiece(2, 3))
                        .ShouldHavePosition(2, fisher2.Id, 9, TieBreakingReasonGenerator.ByNthLargerPiece(2, 1));
        }
    }
}
