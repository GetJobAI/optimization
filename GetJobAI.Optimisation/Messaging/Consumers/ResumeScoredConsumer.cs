using GetJobAI.Optimisation.Contracts;
using GetJobAI.Optimisation.Data;
using GetJobAI.Optimisation.Data.Entities;
using GetJobAI.Optimisation.Data.Models;
using GetJobAI.Optimisation.Messaging.Events;
using GetJobAI.Optimisation.Messaging.Events.ResumeScored;
using GetJobAI.Optimisation.OptimisationService.Contexts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace GetJobAI.Optimisation.Messaging.Consumers;

public class ResumeScoredConsumer(
    IOptimisationOrchestrator orchestrator,
    OptimisationDbContext db,
    ILogger<ResumeScoredConsumer> logger) : IConsumer<ResumeScoredEvent>
{
    public async Task Consume(ConsumeContext<ResumeScoredEvent> context)
    {
        var msg = context.Message;
        var bd = msg.Breakdown;

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == msg.ResumeId, context.CancellationToken);

        if (resume is null)
        {
            logger.LogWarning("Resume {ResumeId} not found — skipping optimisation", msg.ResumeId);
            return;
        }

        var atsScoreId = await db.AtsScores
            .Where(s => s.ResumeId == msg.ResumeId && s.JobPostingId == msg.JobAnalysisId)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (atsScoreId == Guid.Empty)
        {
            logger.LogWarning(
                "ATS score not found for resume {ResumeId} / job {JobId} — skipping optimisation",
                msg.ResumeId, msg.JobAnalysisId);
            return;
        }

        var optimization = Optimization.Create(msg.ResumeId, msg.JobAnalysisId, atsScoreId);

        var doc = optimization.AiSuggestions;
        doc.Status = "in_progress";
        doc.JobTitle = msg.JobTitle;
        doc.CompanyName = msg.CompanyName;
        doc.OverallScore = msg.Score;
        doc.ScoreKeywordEarned = (short)bd.KeywordMatchRate.Earned;
        doc.ScoreKeywordMax = (short)bd.KeywordMatchRate.Max;
        doc.ScoreSkillEarned = (short)bd.SkillAlignment.Earned;
        doc.ScoreSkillMax = (short)bd.SkillAlignment.Max;
        doc.ScoreFormatEarned = (short)bd.FormatAndParseability.Earned;
        doc.ScoreFormatMax = (short)bd.FormatAndParseability.Max;
        doc.ScoreExperienceEarned = (short)bd.ExperienceRelevance.Earned;
        doc.ScoreExperienceMax = (short)bd.ExperienceRelevance.Max;
        doc.MatchedKeywords = bd.KeywordMatchRate.Details?.Match ?? [];
        doc.PartialKeywords = bd.KeywordMatchRate.Details?.Partial ?? [];
        doc.MissingKeywords = bd.KeywordMatchRate.Details?.Missing ?? [];
        doc.SkillAlignmentDetails = bd.SkillAlignment.Details ?? [];
        doc.ExperienceGapDetails = bd.ExperienceRelevance.Details ?? [];
        doc.ParsingFlags = bd.FormatAndParseability.ParsingFlags;
        doc.CandidateName = resume.Content.Contact?.Name;
        doc.ExistingSummary = resume.Content.Summary;
        doc.ResumeSkills = resume.Content.Skills
            .SelectMany(g => g.Items)
            .ToList();
        doc.JobRequiredSkills = (bd.SkillAlignment.Details ?? [])
            .Select(d => new JobRequiredSkillSnapshot
            {
                SkillName = d.RequiredSkill,
                ImportanceScore = d.VectorSimilarityScore
            })
            .ToList();

        // Build resume experience snapshots with stable entry IDs.
        doc.ResumeExperiences = resume.Content.Experience
            .Select(e => new ResumeExperienceSnapshot
            {
                EntryId = Guid.NewGuid(),
                JobTitle = e.Title,
                CompanyName = e.Company,
                StartDate = ParseStartDate(e.Dates),
                EndDate = ParseEndDate(e.Dates),
                Bullets = e.Bullets
            })
            .ToList();

        db.Optimizations.Add(optimization);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Optimization {OptimizationId} created — resume {ResumeId}, job {JobId}, score {Score}",
            optimization.Id, msg.ResumeId, msg.JobAnalysisId, msg.Score);

        try
        {
            var optimisationContext = BuildContext(optimization, doc, resume.UserId);
            var suggestions = await orchestrator.RunAsync(optimisationContext, context.CancellationToken);

            // Assign stable IDs to every suggestion item.
            foreach (var we in suggestions.WorkExperience)
            {
                we.Id = Guid.NewGuid();
                foreach (var bullet in we.Bullets)
                    bullet.Id = Guid.NewGuid();
            }
            foreach (var act in suggestions.Activities)
                act.Id = Guid.NewGuid();
            foreach (var pub in suggestions.Publications)
                pub.Id = Guid.NewGuid();
            foreach (var sec in suggestions.AdditionalSections)
                sec.Id = Guid.NewGuid();

            doc.Status = "completed";
            doc.AtsExplanation = suggestions.AtsExplanation;
            doc.SkillsGap = suggestions.SkillsGap;
            doc.Summary = suggestions.Summary;
            doc.WorkExperiences = suggestions.WorkExperience;
            doc.Activities = suggestions.Activities;
            doc.Publications = suggestions.Publications;
            doc.AdditionalSections = suggestions.AdditionalSections;

            db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
            await db.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new ResumeOptimized
            {
                OptimisationId = optimization.Id,
                ResumeId = optimization.ResumeId,
                UserId = Guid.TryParse(resume.UserId, out var uid) ? uid : Guid.Empty,
                OriginalAtsScore = doc.OverallScore,
                Status = "AwaitingReview"
            });

            logger.LogInformation("Optimization {OptimizationId} completed", optimization.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Optimization {OptimizationId} failed", optimization.Id);

            doc.Status = "failed";
            doc.ErrorMessage = ex.Message;
            db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
            await db.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new ResumeOptimized
            {
                OptimisationId = optimization.Id,
                ResumeId = optimization.ResumeId,
                UserId = Guid.TryParse(resume.UserId, out var uid) ? uid : Guid.Empty,
                OriginalAtsScore = doc.OverallScore,
                Status = "Failed",
                ErrorMessage = ex.Message
            });
        }
    }

    private static OptimisationContext BuildContext(Optimization optimization, OptimizationDoc doc, string userId)
    {
        return new OptimisationContext
        {
            OptimisationId = optimization.Id,
            ResumeId = optimization.ResumeId,
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
}
