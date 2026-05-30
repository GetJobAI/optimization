namespace GetJobAI.Optimisation.Data.Entities;

public class AtsScore
{
    public Guid Id { get; private set; }

    public Guid ResumeId { get; private set; }

    public Guid JobPostingId { get; private set; }

    private AtsScore() { }
}
