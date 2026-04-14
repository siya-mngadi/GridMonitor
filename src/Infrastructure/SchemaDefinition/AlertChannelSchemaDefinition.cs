using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class AlertChannelSchemaDefinition : IEntityTypeConfiguration<AlertChannel>
{
	public void Configure(EntityTypeBuilder<AlertChannel> builder)
	{
		builder.ToTable("AlertChannels", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		// ChannelType enum stored as string so the column is readable without joins
		builder.Property(x => x.ChannelType)
			   .IsRequired()
			   .HasMaxLength(20)
			   .HasConversion<string>();

		builder.Property(x => x.Destination)
			   .IsRequired()
			   .HasMaxLength(500);

		// Nullable — only populated for webhook channels
		builder.Property(x => x.WebhookSecret)
			   .IsRequired(false)
			   .HasMaxLength(100);

		builder.Property(x => x.Active)
			   .IsRequired()
			   .HasDefaultValue(true);

		builder.HasOne(x => x.Subscription)
			   .WithMany(x => x.Channels)
			   .HasForeignKey(x => x.SubscriptionId)
			   .OnDelete(DeleteBehavior.Cascade);
	}
}