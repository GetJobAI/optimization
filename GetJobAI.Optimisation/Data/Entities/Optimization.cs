using GetJobAI.Optimisation.Data.Models;

namespace GetJobAI.Optimisation.Data.Entities;

public class Optimization
{
    public Guid Id { get; private set; }

    public Guid ResumeId { get; private set; }

    public Guid JobPostingId { get; private set; }

    public Guid AtsScoreId { get; private set; }

    public OptimizationDoc AiSuggestions { get; set; } = new();

    public DateTime CreatedAt { get; private set; }

    private Optimization() { }

    public static Optimization Create(Guid resumeId, Guid jobPostingId, Guid atsScoreId) => new()
    {
        Id = Guid.NewGuid(),
        ResumeId = resumeId,
        JobPostingId = jobPostingId,
        AtsScoreId = atsScoreId,
        CreatedAt = DateTime.UtcNow
    };
}
