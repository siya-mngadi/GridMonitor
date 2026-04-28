using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class MunicipalitySchemaDefinition : IEntityTypeConfiguration<Municipality>
{
	public void Configure(EntityTypeBuilder<Municipality> builder)
	{
		builder.ToTable("Municipalities", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.EskomId)
			   .IsUnique();

		builder.Property(x => x.EskomId)
			   .IsRequired();

		builder.Property(x => x.Name)
			   .IsRequired()
			   .HasMaxLength(150);

		builder.Property(x => x.LastSyncedAt)
			   .IsRequired()
			   .HasDefaultValueSql("CURRENT_TIMESTAMP");

		builder.HasOne(x => x.Province)
			   .WithMany(x => x.Municipalities)
			   .HasPrincipalKey(x => x.EskomId)
			   .HasForeignKey(x => x.ProvinceId)
			   .OnDelete(DeleteBehavior.Restrict);
	}
}