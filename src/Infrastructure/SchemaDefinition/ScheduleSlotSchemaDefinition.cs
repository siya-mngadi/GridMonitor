using GridMonitor.Domain.Entities;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridMonitor.Infrastructure.SchemaDefinition;

public class ScheduleSlotSchemaDefinition : IEntityTypeConfiguration<ScheduleSlot>
{
	public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
	{
		builder.ToTable("ScheduleSlots", AppDbContext.DefaultSchema);

		builder.HasKey(x => x.Id);

		// Composite unique: one slot per suburb / stage / day / start time
		builder.HasIndex(x => new { x.SuburbId, x.Stage, x.ScheduleDay, x.StartTime })
			   .IsUnique();

		builder.HasIndex(x => x.SuburbId);

		builder.Property(x => x.Stage)
			   .IsRequired();

		builder.Property(x => x.StartTime)
			   .IsRequired();

		builder.Property(x => x.EndTime)
			   .IsRequired();

		builder.Property(x => x.ScheduleDay)
			   .IsRequired()
			   .HasMaxLength(20)
			   .HasConversion<string>();

		builder.Property(x => x.DataHash)
			   .IsRequired()
			   .HasMaxLength(64);

		builder.Property(x => x.CreatedAt)
			   .IsRequired()
			   .HasDefaultValueSql("CURRENT_TIMESTAMP");

		builder.HasOne(x => x.Suburb)
			   .WithMany(x => x.Slots)
			   .HasPrincipalKey(x => x.EskomId)
			   .HasForeignKey(x => x.SuburbId)
			   .OnDelete(DeleteBehavior.Cascade);
	}
}