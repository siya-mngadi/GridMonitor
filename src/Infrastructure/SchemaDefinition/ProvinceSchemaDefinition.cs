using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class ProvinceSchemaDefinition : IEntityTypeConfiguration<Province>
{
	public void Configure(EntityTypeBuilder<Province> builder)
	{
		builder.ToTable("Provinces", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.EskomId)
			   .IsUnique();

		builder.Property(x => x.Name)
			   .IsRequired()
			   .HasMaxLength(100);

		builder.Property(x => x.LastSyncedAt)
			   .ValueGeneratedOnAddOrUpdate()
			   .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
	}
}