using System.Text.Json;
using GetJobAI.Optimisation.Contracts;
using GetJobAI.Optimisation.Data;
using GetJobAI.Optimisation.Data.Models;
using GetJobAI.Optimisation.Messaging.Events;
using GetJobAI.Optimisation.Messaging.Events.ResumeScored;
using GetJobAI.Optimisation.OptimisationService.Contexts;
using GetJobAI.Optimisation.OptimisationService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Entities = GetJobAI.Optimisation.Data.Entities;

namespace GetJobAI.Optimisation.Messaging.Consumers;

public class ResumeScoredConsumer(
    IOptimisationOrchestrator orchestrator,
    OptimisationDbContext db,
    ILogger<ResumeScoredConsumer> logger) : IConsumer<ResumeScoredEvent>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task Consume(ConsumeContext<ResumeScoredEvent> context)
    {
        var msg = context.Message;
        var breakdown = msg.Breakdown;

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == msg.ResumeId, context.CancellationToken);

        if (resume is null)
        {
            logger.LogWarning(
                "Resume {ResumeId} not found — skipping optimisation for job {JobAnalysisId}",
                msg.ResumeId, msg.JobAnalysisId);

            return;
        }

        var optimisation = Entities.Optimisation.Create(
            resumeId: msg.ResumeId,
            jobAnalysisId: msg.JobAnalysisId,
            jobTitle: msg.JobTitle,
            companyName: msg.CompanyName,
            overallScore: msg.Score,
            scoreKeywordEarned: (short)breakdown.KeywordMatchRate.Earned,
            scoreKeywordMax: (short)breakdown.KeywordMatchRate.Max,
            scoreSkillEarned: (short)breakdown.SkillAlignment.Earned,
            scoreSkillMax: (short)breakdown.SkillAlignment.Max,
            scoreFormatEarned: (short)breakdown.FormatAndParseability.Earned,
            scoreFormatMax: (short)breakdown.FormatAndParseability.Max,
            scoreExperienceEarned: (short)breakdown.ExperienceRelevance.Earned,
            scoreExperienceMax: (short)breakdown.ExperienceRelevance.Max,
            atsDetailsJson: JsonSerializer.Serialize(breakdown, JsonOptions));

        db.Optimisations.Add(optimisation);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Optimisation {OptimisationId} started — resume {ResumeId}, job {JobAnalysisId}, score {Score}",
            optimisation.Id, msg.ResumeId, msg.JobAnalysisId, msg.Score);

        optimisation.Start();
        await db.SaveChangesAsync(context.CancellationToken);

        try
        {
            var optimisationContext = BuildContext(optimisation, msg, resume.Content);
            var suggestions = await orchestrator.RunAsync(optimisationContext, context.CancellationToken);

            SaveSuggestions(optimisation.Id, suggestions);
            optimisation.Complete(suggestions.AtsExplanation, suggestions.SkillsGap);
            await db.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new ResumeOptimized
            {
                OptimisationId = optimisation.Id,
                ResumeId = optimisation.ResumeId,
                OriginalAtsScore = optimisation.OverallScore,
                Status = "AwaitingReview"
            });

            logger.LogInformation("Optimisation {OptimisationId} completed", optimisation.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Optimisation {OptimisationId} failed", optimisation.Id);

            optimisation.Fail(ex.Message);
            await db.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new ResumeOptimized
            {
                OptimisationId = optimisation.Id,
                ResumeId = optimisation.ResumeId,
                OriginalAtsScore = optimisation.OverallScore,
                Status = "Failed",
                ErrorMessage = ex.Message
            });
        }
    }

    private void SaveSuggestions(Guid optimisationId, AiSuggestionsDocument doc)
    {
        if (doc.Summary is not null)
        {
            db.SummarySuggestions.Add(Entities.OptimisationSummarySuggestion.Create(
                optimisationId,
                doc.Summary.Original,
                doc.Summary.Rewritten,
                doc.Summary.KeywordsIncorporated));
        }

        foreach (var we in doc.WorkExperience)
        {
            var weEntity = Entities.OptimisationWorkExperienceSuggestion.Create(
                optimisationId, we.EntryId, we.Include, we.Reason);

            foreach (var bullet in we.Bullets)
                db.BulletSuggestions.Add(Entities.OptimisationBulletSuggestion.Create(
                    weEntity.Id,
                    bullet.Original,
                    bullet.Rewritten,
                    bullet.KeywordsAdded,
                    bullet.XyzApplied));

            db.WorkExperienceSuggestions.Add(weEntity);
        }

        foreach (var activity in doc.Activities)
            db.ActivitySuggestions.Add(Entities.OptimisationActivitySuggestion.Create(
                optimisationId,
                activity.EntryId,
                activity.Include,
                activity.Reason,
                activity.HighlightsRewritten));

        foreach (var pub in doc.Publications)
            db.SectionSuggestions.Add(Entities.OptimisationSectionSuggestion.Create(
                optimisationId,
                Entities.OptimisationSectionCategory.Publication,
                pub.EntryId,
                pub.SectionType,
                pub.Include,
                pub.Reason));

        foreach (var section in doc.AdditionalSections)
            db.SectionSuggestions.Add(Entities.OptimisationSectionSuggestion.Create(
                optimisationId,
                Entities.OptimisationSectionCategory.AdditionalSection,
                section.EntryId,
                section.SectionType,
                section.Include,
                section.Reason));
    }

    private static OptimisationContext BuildContext(
        Entities.Optimisation optimisation,
        ResumeScoredEvent msg,
        ResumeContent content)
    {
        var bd = msg.Breakdown;

        return new OptimisationContext
        {
            OptimisationId = optimisation.Id,
            ResumeId = optimisation.ResumeId,
            JobTitle = msg.JobTitle,
            CompanyName = msg.CompanyName,
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
                })
                .ToList(),

            ExperienceGapDetails = (bd.ExperienceRelevance.Details ?? [])
                .Select(d => new ExperienceGapContext
                {
                    JobResponsibility = d.JobResponsibility,
                    ClosestMatch = d.ClosestMatch,
                    VectorSimilarityScore = d.VectorSimilarityScore,
                    Flag = d.Flag
                })
                .ToList(),

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
                })
                .ToList(),

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
                })
                .ToList(),

            JobPreferredSkills = []
        };
    }

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
                ContentJson = System.Text.Json.JsonSerializer.Serialize(content.Certifications)
            });
        }

        if (content.Languages.Count > 0)
        {
            sections.Add(new AdditionalSectionContext
            {
                EntryId = Guid.NewGuid(),
                SectionType = "languages",
                Title = "Languages",
                ContentJson = System.Text.Json.JsonSerializer.Serialize(content.Languages)
            });
        }

        if (content.Projects.Count > 0)
        {
            sections.Add(new AdditionalSectionContext
            {
                EntryId = Guid.NewGuid(),
                SectionType = "projects",
                Title = "Projects",
                ContentJson = System.Text.Json.JsonSerializer.Serialize(content.Projects)
            });
        }

        return sections;
    }
}
