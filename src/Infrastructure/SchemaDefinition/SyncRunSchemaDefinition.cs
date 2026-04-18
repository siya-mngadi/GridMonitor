using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

internal class SyncRunSchemaDefinition : IEntityTypeConfiguration<SyncRun>
{
	public void Configure(EntityTypeBuilder<SyncRun> builder)
	{
		builder.ToTable("SyncRuns", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.StartedAt);

		builder.Property(x => x.Type)
			   .IsRequired()
			   .HasConversion<string>()
			   .HasMaxLength(30);

		builder.Property(x => x.Success)
			   .IsRequired();

		builder.Property(x => x.MunicipalitiesProcessed)
			   .IsRequired()
			   .HasDefaultValue(0);

		builder.Property(x => x.SuburbProcessed)
			   .IsRequired()
			   .HasDefaultValue(0);

		builder.Property(x => x.ErrorMessage)
			   .IsRequired(false);

		builder.Property(x => x.StartedAt)
			   .IsRequired()
			   .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

		// FinishedAt is nullable — null while the run is in progress
		builder.Property(x => x.FinishedAt)
			   .IsRequired(false);
	}
}
