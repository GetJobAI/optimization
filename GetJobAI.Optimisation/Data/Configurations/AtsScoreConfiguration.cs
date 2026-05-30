using GetJobAI.Optimisation.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetJobAI.Optimisation.Data.Configurations;

public class AtsScoreConfiguration : IEntityTypeConfiguration<AtsScore>
{
    public void Configure(EntityTypeBuilder<AtsScore> builder)
    {
        builder.ToTable("ats_scores", t => t.ExcludeFromMigrations());

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ResumeId)
            .HasColumnName("resume_id");

        builder.Property(x => x.JobPostingId)
            .HasColumnName("job_posting_id");
    }
}
