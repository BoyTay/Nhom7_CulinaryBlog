using CulinaryBlog.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class JobExecutionConfiguration : IEntityTypeConfiguration<JobExecutionRecord>
{
    public void Configure(EntityTypeBuilder<JobExecutionRecord> builder)
    {
        builder.ToTable("JobExecutions", table =>
        {
            table.HasCheckConstraint("CK_JobExecutions_Status", "\"Status\" IN ('Running', 'Failed', 'Completed')");
            table.HasCheckConstraint("CK_JobExecutions_AttemptCount", "\"AttemptCount\" > 0");
        });
        builder.HasKey(execution => execution.IdempotencyKey);
        builder.Property(execution => execution.IdempotencyKey).HasMaxLength(256);
        builder.Property(execution => execution.Status).HasMaxLength(16).IsRequired();
        builder.HasIndex(execution => execution.CompletedAt).HasFilter("\"Status\" = 'Completed'");
        builder.HasIndex(execution => execution.LastAttemptAt).HasFilter("\"Status\" <> 'Completed'");
        builder.HasIndex(execution => execution.LockedUntil).HasFilter("\"Status\" = 'Running'");
    }
}
