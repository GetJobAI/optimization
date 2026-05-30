using GetJobAI.Optimisation.Data.Models;

namespace GetJobAI.Optimisation.Data.Entities;

public class Resume
{
    public Guid Id { get; private set; }

    public string UserId { get; set; } = string.Empty;

    public ResumeContent Content { get; set; } = new();

    public DateTime UpdatedAt { get; set; }

    private Resume() { }
}
