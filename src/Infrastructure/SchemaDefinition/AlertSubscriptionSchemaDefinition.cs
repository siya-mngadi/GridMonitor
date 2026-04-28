using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class AlertSubscriptionSchemaDefinition : IEntityTypeConfiguration<AlertSubscription>
{
	public void Configure(EntityTypeBuilder<AlertSubscription> builder)
	{
		builder.ToTable("AlertSubscriptions", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		// Partial index for the alert engine's every-5-min query
		builder.HasIndex(x => new { x.UserId, x.SuburbId });

		builder.HasIndex(x => x.Active);

		builder.Property(x => x.AlertMinutesBefore)
			   .IsRequired()
			   .HasDefaultValue(30);

		builder.Property(x => x.Active)
			   .IsRequired()
			   .HasDefaultValue(true);

		builder.Property(x => x.CreatedAt)
			   .IsRequired()
			   .HasDefaultValueSql("CURRENT_TIMESTAMP");

		builder.HasOne(x => x.User)
			   .WithMany(x => x.Subscriptions)
			   .HasForeignKey(x => x.UserId)
			   .OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Suburb)
			   .WithMany()
			   .HasPrincipalKey(x => x.EskomId)
			   .HasForeignKey(x => x.SuburbId)
			   .OnDelete(DeleteBehavior.Restrict);
	}
}