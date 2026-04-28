using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class SuburbSchemaDefinition : IEntityTypeConfiguration<Suburb>
{
	public void Configure(EntityTypeBuilder<Suburb> builder)
	{
		builder.ToTable("Suburbs", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.EskomId)
			   .IsUnique();

		// Plain B-tree index — trigram GIN index is added in raw SQL migration
		// for fast ILIKE suburb search (requires pg_trgm extension)
		builder.HasIndex(x => x.Name);

		builder.Property(x => x.EskomId)
			   .IsRequired();

		builder.Property(x => x.Name)
			   .IsRequired()
			   .HasMaxLength(150);

		builder.Property(x => x.LastSyncedAt)
			   .IsRequired()
			   .HasDefaultValueSql("CURRENT_TIMESTAMP");

		builder.HasOne(x => x.Municipality)
			   .WithMany(x => x.Suburbs)
			   .HasPrincipalKey(x => x.EskomId)
			   .HasForeignKey(x => x.MunicipalityId)
			   .OnDelete(DeleteBehavior.Restrict);
	}
}