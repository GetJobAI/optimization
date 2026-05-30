using System.Text.Json;
using GetJobAI.Optimisation.Data;
using GetJobAI.Optimisation.Data.Models;
using GetJobAI.Optimisation.Messaging.Events.ResumeScored;
using GetJobAI.Optimisation.OptimisationService.Contexts;
using Microsoft.EntityFrameworkCore;
using Entities = GetJobAI.Optimisation.Data.Entities;

namespace GetJobAI.Optimisation.Services;

public class OptimisationContextFactory(OptimisationDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OptimisationContext?> CreateAsync(Guid optimisationId, CancellationToken ct)
    {
        var optimisation = await db.Optimisations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);
        if (optimisation is null) return null;

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == optimisation.ResumeId, ct);
        if (resume is null) return null;

        var breakdown = optimisation.AtsDetailsJson is not null
            ? JsonSerializer.Deserialize<AtsBreakdown>(optimisation.AtsDetailsJson, JsonOptions) ?? new AtsBreakdown()
            : new AtsBreakdown();

        return Build(optimisation, resume.Content, breakdown);
    }

    private static OptimisationContext Build(
        Entities.Optimisation optimisation,
        ResumeContent content,
        AtsBreakdown bd) => new()
    {
        OptimisationId = optimisation.Id,
        ResumeId = optimisation.ResumeId,
        JobTitle = optimisation.JobTitle,
        CompanyName = optimisation.CompanyName,
        CandidateName = content.Contact?.Name,
        ExistingSummary = content.Summary,
        DetectedLanguage = null,
        OverallScore = optimisation.OverallScore,
        ScoreKeywordEarned = optimisation.ScoreKeywordEarned,
        ScoreKeywordMax = optimisation.ScoreKeywordMax,
        ScoreSkillEarned = optimisation.ScoreSkillEarned,
        ScoreSkillMax = optimisation.ScoreSkillMax,
        ScoreFormatEarned = optimisation.ScoreFormatEarned,
        ScoreFormatMax = optimisation.ScoreFormatMax,
        ScoreExperienceEarned = optimisation.ScoreExperienceEarned,
        ScoreExperienceMax = optimisation.ScoreExperienceMax,
        MatchedKeywords = bd.KeywordMatchRate.Details?.Match ?? [],
        PartialKeywords = bd.KeywordMatchRate.Details?.Partial ?? [],
        MissingKeywords = bd.KeywordMatchRate.Details?.Missing ?? [],
        SkillAlignmentDetails = (bd.SkillAlignment.Details ?? [])
            .Select(d => new SkillAlignmentContext
            {
                RequiredSkill = d.RequiredSkill,
                ClosestMatch = d.ClosestMatch,
                VectorSimilarityScore = d.VectorSimilarityScore,
                Flag = d.Flag
            }).ToList(),
        ExperienceGapDetails = (bd.ExperienceRelevance.Details ?? [])
            .Select(d => new ExperienceGapContext
            {
                JobResponsibility = d.JobResponsibility,
                ClosestMatch = d.ClosestMatch,
                VectorSimilarityScore = d.VectorSimilarityScore,
                Flag = d.Flag
            }).ToList(),
        ParsingFlags = new AtsParsingFlagsContext
        {
            HasComplexLayout = bd.FormatAndParseability.ParsingFlags.HasComplexLayout,
            HasGraphics = bd.FormatAndParseability.ParsingFlags.HasGraphics,
            HasHeadersFooters = bd.FormatAndParseability.ParsingFlags.HasHeadersFooters,
            HasNonStandardFonts = bd.FormatAndParseability.ParsingFlags.HasNonStandardFonts
        },
        WorkExperiences = content.Experience
            .Select(e => new WorkExperienceContext
            {
                EntryId = Guid.NewGuid(),
                JobTitle = e.Title,
                CompanyName = e.Company,
                StartDate = ParseStartDate(e.Dates),
                EndDate = ParseEndDate(e.Dates),
                Bullets = e.Bullets
            }).ToList(),
        Skills = content.Skills
            .SelectMany(g => g.Items.Select(item => new SkillContext
            {
                SkillName = item,
                SkillNameRaw = item,
                Category = g.Category
            }))
            .ToList(),
        Publications = [],
        Activities = [],
        AdditionalSections = BuildAdditionalSections(content),
        JobRequiredSkills = (bd.SkillAlignment.Details ?? [])
            .Select(d => new JobSkillContext
            {
                SkillName = d.RequiredSkill,
                ImportanceScore = d.VectorSimilarityScore,
                IsRequired = true
            }).ToList(),
        JobPreferredSkills = []
    };

    private static string ParseStartDate(string? dates)
    {
        if (string.IsNullOrWhiteSpace(dates)) return string.Empty;
        var parts = dates.Split([" - ", " – ", " to "], 2, StringSplitOptions.TrimEntries);
        return parts[0];
    }

    private static string ParseEndDate(string? dates)
    {
        if (string.IsNullOrWhiteSpace(dates)) return string.Empty;
        var parts = dates.Split([" - ", " – ", " to "], 2, StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    private static List<AdditionalSectionContext> BuildAdditionalSections(ResumeContent content)
    {
        var sections = new List<AdditionalSectionContext>();

        if (content.Certifications.Count > 0)
        {
            sections.Add(new AdditionalSectionContext
            {
                EntryId = Guid.NewGuid(),
                SectionType = "certifications",
                Title = "Certifications",
                ContentJson = JsonSerializer.Serialize(content.Certifications)
            });
        }

        if (content.Languages.Count > 0)
        {
            sections.Add(new AdditionalSectionContext
            {
                EntryId = Guid.NewGuid(),
                SectionType = "languages",
                Title = "Languages",
                ContentJson = JsonSerializer.Serialize(content.Languages)
            });
        }

        if (content.Projects.Count > 0)
        {
            sections.Add(new AdditionalSectionContext
            {
                EntryId = Guid.NewGuid(),
                SectionType = "projects",
                Title = "Projects",
                ContentJson = JsonSerializer.Serialize(content.Projects)
            });
        }

        return sections;
    }
}
