using GetJobAI.Optimisation.Messaging.Events.ResumeScored;
using GetJobAI.Optimisation.OptimisationService.Models;
using GetJobAI.Optimisation.OptimisationService.Results;

namespace GetJobAI.Optimisation.Data.Models;

/// <summary>
/// The full content stored in optimizations.ai_suggestions JSONB.
/// Self-contained — no DB joins required for API operations.
/// </summary>
public class OptimizationDoc
{
    // --- state ---
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }

    // --- job context (denormalized) ---
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int OverallScore { get; set; }

    // --- score breakdown (needed to reconstruct OptimisationContext for rewrites) ---
    public short ScoreKeywordEarned { get; set; }
    public short ScoreKeywordMax { get; set; }
    public short ScoreSkillEarned { get; set; }
    public short ScoreSkillMax { get; set; }
    public short ScoreFormatEarned { get; set; }
    public short ScoreFormatMax { get; set; }
    public short ScoreExperienceEarned { get; set; }
    public short ScoreExperienceMax { get; set; }

    // --- keyword lists ---
    public List<string> MatchedKeywords { get; set; } = [];
    public List<string> PartialKeywords { get; set; } = [];
    public List<string> MissingKeywords { get; set; } = [];

    // These types already carry their own [JsonPropertyName] attributes.
    public List<SkillAlignmentDetail> SkillAlignmentDetails { get; set; } = [];
    public List<ExperienceRelevanceDetail> ExperienceGapDetails { get; set; } = [];
    public AtsParsingFlags? ParsingFlags { get; set; }

    // --- resume context snapshot (stored once at creation for stable rewrite context) ---
    public string? CandidateName { get; set; }
    public string? ExistingSummary { get; set; }
    public List<ResumeExperienceSnapshot> ResumeExperiences { get; set; } = [];
    public List<string> ResumeSkills { get; set; } = [];
    public List<JobRequiredSkillSnapshot> JobRequiredSkills { get; set; } = [];

    // --- AI output ---
    public AtsExplanationResult? AtsExplanation { get; set; }
    public SkillsGapResult? SkillsGap { get; set; }
    public SummarySuggestion? Summary { get; set; }
    public List<WorkExperienceSuggestion> WorkExperiences { get; set; } = [];
    public List<ActivitySuggestion> Activities { get; set; } = [];
    public List<SectionRelevancySuggestion> Publications { get; set; } = [];
    public List<SectionRelevancySuggestion> AdditionalSections { get; set; } = [];
    public StoredCoverLetter? CoverLetter { get; set; }
}

public class ResumeExperienceSnapshot
{
    public Guid EntryId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public List<string> Bullets { get; set; } = [];
}

public class JobRequiredSkillSnapshot
{
    public string SkillName { get; set; } = string.Empty;
    public double ImportanceScore { get; set; }
}

public class StoredCoverLetter
{
    public string CoverLetter { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string SalutationUsed { get; set; } = string.Empty;
    public List<string> KeyPointsMade { get; set; } = [];
    public bool? Accepted { get; set; }
    public int RewriteCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}
