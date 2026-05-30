using GetJobAI.Optimisation.Data;
using GetJobAI.Optimisation.Data.Models;
using GetJobAI.Optimisation.OptimisationService.Contexts;
using Microsoft.EntityFrameworkCore;

namespace GetJobAI.Optimisation.Services;

public class OptimisationContextFactory(OptimisationDbContext db)
{
    public async Task<OptimisationContext?> CreateAsync(Guid optimisationId, CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);

        if (optimization is null) return null;

        var doc = optimization.AiSuggestions;
        return BuildContext(optimisationId, optimization.ResumeId, doc);
    }

    internal static OptimisationContext BuildContext(Guid optimisationId, Guid resumeId, OptimizationDoc doc)
    {
        return new OptimisationContext
        {
            OptimisationId = optimisationId,
            ResumeId = resumeId,
            JobTitle = doc.JobTitle,
            CompanyName = doc.CompanyName,
            CandidateName = doc.CandidateName,
            ExistingSummary = doc.ExistingSummary,
            DetectedLanguage = null,
            OverallScore = doc.OverallScore,
            ScoreKeywordEarned = doc.ScoreKeywordEarned,
            ScoreKeywordMax = doc.ScoreKeywordMax,
            ScoreSkillEarned = doc.ScoreSkillEarned,
            ScoreSkillMax = doc.ScoreSkillMax,
            ScoreFormatEarned = doc.ScoreFormatEarned,
            ScoreFormatMax = doc.ScoreFormatMax,
            ScoreExperienceEarned = doc.ScoreExperienceEarned,
            ScoreExperienceMax = doc.ScoreExperienceMax,
            MatchedKeywords = doc.MatchedKeywords,
            PartialKeywords = doc.PartialKeywords,
            MissingKeywords = doc.MissingKeywords,
            SkillAlignmentDetails = doc.SkillAlignmentDetails
                .Select(d => new SkillAlignmentContext
                {
                    RequiredSkill = d.RequiredSkill,
                    ClosestMatch = d.ClosestMatch,
                    VectorSimilarityScore = d.VectorSimilarityScore,
                    Flag = d.Flag
                })
                .ToList(),
            ExperienceGapDetails = doc.ExperienceGapDetails
                .Select(d => new ExperienceGapContext
                {
                    JobResponsibility = d.JobResponsibility,
                    ClosestMatch = d.ClosestMatch,
                    VectorSimilarityScore = d.VectorSimilarityScore,
                    Flag = d.Flag
                })
                .ToList(),
            ParsingFlags = doc.ParsingFlags is { } pf
                ? new AtsParsingFlagsContext
                {
                    HasComplexLayout = pf.HasComplexLayout,
                    HasGraphics = pf.HasGraphics,
                    HasHeadersFooters = pf.HasHeadersFooters,
                    HasNonStandardFonts = pf.HasNonStandardFonts
                }
                : null,
            WorkExperiences = doc.ResumeExperiences
                .Select(e => new WorkExperienceContext
                {
                    EntryId = e.EntryId,
                    JobTitle = e.JobTitle,
                    CompanyName = e.CompanyName,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Bullets = e.Bullets
                })
                .ToList(),
            Skills = doc.ResumeSkills
                .Select(s => new SkillContext { SkillName = s, SkillNameRaw = s })
                .ToList(),
            Publications = [],
            Activities = [],
            AdditionalSections = [],
            JobRequiredSkills = doc.JobRequiredSkills
                .Select(s => new JobSkillContext
                {
                    SkillName = s.SkillName,
                    ImportanceScore = s.ImportanceScore,
                    IsRequired = true
                })
                .ToList(),
            JobPreferredSkills = []
        };
    }
}
