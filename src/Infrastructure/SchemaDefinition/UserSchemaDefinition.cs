using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

internal class UserSchemaDefinition : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("Users", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => new { x.Email, x.KeycloakId })
			   .IsUnique();

		builder.Property(x => x.Email)
			   .IsRequired()
			   .HasMaxLength(200);

		builder.Property(x => x.KeycloakId)
			   .IsRequired()
			   .HasMaxLength(200);

		builder.Property(x => x.PricingTier)
			   .IsRequired()
			   .HasMaxLength(20)
			   .HasDefaultValue(PricingTier.Free)
			   .HasConversion<string>();

		builder.Property(x => x.Active)
			   .IsRequired()
			   .HasDefaultValue(true);

		builder.Property(x => x.CreatedAt)
			   .IsRequired();
	}
}
