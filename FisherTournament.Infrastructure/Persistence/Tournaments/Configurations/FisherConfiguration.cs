using FisherTournament.Domain.FisherAggregate;
using FisherTournament.Domain.UserAggregate;
using FisherTournament.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FisherTournament.Infrastructure.Persistence.Tournaments.Configurations;

public class FisherConfiguration : IEntityTypeConfiguration<Fisher>
{
    public void Configure(EntityTypeBuilder<Fisher> builder)
    {
        builder.ToTable("Fishers");
        builder.HasGuidIdKey(f => f.Id);
    }
}