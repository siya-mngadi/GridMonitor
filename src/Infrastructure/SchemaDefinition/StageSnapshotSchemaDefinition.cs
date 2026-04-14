using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class StageSnapshotSchemaDefinition : IEntityTypeConfiguration<StageSnapshot>
{
	public void Configure(EntityTypeBuilder<StageSnapshot> builder)
	{
		builder.ToTable("StageSnapshots", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.CreatedAt);

		builder.Property(x => x.Stage)
			   .IsRequired();

		builder.Property(x => x.RawText)
			   .IsRequired()
			   .HasMaxLength(10);
	}
}