using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class AlertLogSchemaDefinition : IEntityTypeConfiguration<AlertLog>
{
	public void Configure(EntityTypeBuilder<AlertLog> builder)
	{
		builder.ToTable("AlertLogs", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.SubscriptionId);

		builder.HasIndex(x => x.SentAt);

		builder.Property(x => x.ChannelType)
			   .IsRequired()
			   .HasMaxLength(20)
			   .HasConversion<string>();

		builder.Property(x => x.Destination)
			   .IsRequired()
			   .HasMaxLength(500);

		builder.Property(x => x.Event)
			   .IsRequired()
			   .HasMaxLength(30)
			   .HasConversion<string>();

		builder.Property(x => x.Stage)
			   .IsRequired();

		builder.Property(x => x.Success)
			   .IsRequired();

		builder.Property(x => x.ErrorMessage)
			   .IsRequired(false)
			   .HasMaxLength(1000);

		builder.Property(x => x.AttemptCount)
			   .IsRequired()
			   .HasDefaultValue(1);

		builder.Property(x => x.SentAt)
			   .IsRequired();

		builder.HasOne(x => x.Subscription)
			   .WithMany(x => x.Logs)
			   .HasForeignKey(x => x.SubscriptionId)
			   .OnDelete(DeleteBehavior.Cascade);
	}
}