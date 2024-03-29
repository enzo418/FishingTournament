using FisherTournament.Application.Common.Persistence;
using FisherTournament.Domain.CompetitionAggregate;
using FisherTournament.Domain.FisherAggregate;
using FisherTournament.Domain.TournamentAggregate;
using FisherTournament.Domain.UserAggregate;
using FisherTournament.Infrastructure.Mediator;
using FisherTournament.Infrastructure.Persistence.Tournaments.Configurations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FisherTournament.Infrastructure.Persistence.Tournaments;

using Application.Common.Metrics;

public class TournamentFisherDbContext : DbContext, ITournamentFisherDbContext
{
    public DbSet<Tournament> Tournaments { get; set; } = null!;
    public DbSet<Competition> Competitions { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Fisher> Fishers { get; set; } = null!;

    private readonly IMediator _mediator = null!;

    public TournamentFisherDbContext(
        DbContextOptions<TournamentFisherDbContext> options,
        IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    public TournamentFisherDbContext(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.ApplyConfigurationsFromAssembly(typeof(TournamentFisherDbContext).Assembly);

        new UserConfiguration().Configure(modelBuilder.Entity<User>());
        new FisherConfiguration().Configure(modelBuilder.Entity<Fisher>());
        new CompetitionConfiguration().Configure(modelBuilder.Entity<Competition>());
        new TournamentConfigurations().Configure(modelBuilder.Entity<Tournament>());

        base.OnModelCreating(modelBuilder);
    }

    // No need to override SaveChanges(), since ITournamentFisherDbContext only defines SaveChangesAsync().

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        Tag DbContextTag = new("DbContext", GetType().Name); // Create base DbContext?

        using (var _ = ApplicationMetrics.DatabaseMetrics.SaveChangesHistogram.Time(new Tag("order", "before"), DbContextTag))
        {
            await _mediator.DispatchDomainEventsBeforeSaveAsync(this, cancellationToken);
        }

        int changes;
        using (var _ = ApplicationMetrics.DatabaseMetrics.SaveChangesHistogram.Time(new Tag("order", "base"), DbContextTag))
        {
            changes = await base.SaveChangesAsync(cancellationToken);
        }

        using (var _ = ApplicationMetrics.DatabaseMetrics.SaveChangesHistogram.Time(new Tag("order", "after"), DbContextTag))
        {
            await _mediator.DispatchDomainEventsAfterSaveAsync(this, cancellationToken);
        }

        return changes;
    }
}