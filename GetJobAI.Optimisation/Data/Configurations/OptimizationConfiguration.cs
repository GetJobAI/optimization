using System.Text.Json;
using System.Text.Json.Serialization;
using GetJobAI.Optimisation.Data.Entities;
using GetJobAI.Optimisation.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetJobAI.Optimisation.Data.Configurations;

public class OptimizationConfiguration : IEntityTypeConfiguration<Optimization>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Configure(EntityTypeBuilder<Optimization> builder)
    {
        builder.ToTable("optimizations", t => t.ExcludeFromMigrations());

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ResumeId)
            .HasColumnName("resume_id");

        builder.Property(x => x.JobPostingId)
            .HasColumnName("job_posting_id");

        builder.Property(x => x.AtsScoreId)
            .HasColumnName("ats_score_id");

        builder.Property(x => x.AiSuggestions)
            .HasColumnName("ai_suggestions")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<OptimizationDoc>(v, JsonOptions) ?? new OptimizationDoc()
            );

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}
