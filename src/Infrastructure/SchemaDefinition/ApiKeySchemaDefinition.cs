using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class ApiKeySchemaDefinition : IEntityTypeConfiguration<ApiKey>
{
	public void Configure(EntityTypeBuilder<ApiKey> builder)
	{
		builder.ToTable("ApiKeys", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.KeyHash)
			   .IsUnique();

		builder.Property(x => x.KeyHash)
			   .IsRequired()
			   .HasMaxLength(64);  // SHA-256 hex = 64 chars

		builder.Property(x => x.KeyPrefix)
			   .IsRequired()
			   .HasMaxLength(20);

		builder.Property(x => x.Active)
			   .IsRequired()
			   .HasDefaultValue(true);

		builder.Property(x => x.DailyCallLimit)
			   .IsRequired()
			   .HasDefaultValue(50);

		builder.Property(x => x.CreatedAt)
			   .IsRequired()
			   .HasDefaultValueSql("CURRENT_TIMESTAMP");

		builder.HasOne(x => x.User)
			   .WithMany(x => x.ApiKeys)
			   .HasForeignKey(x => x.UserId)
			   .OnDelete(DeleteBehavior.Cascade);
	}
}