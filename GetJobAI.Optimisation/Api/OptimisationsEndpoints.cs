using GetJobAI.Optimisation.Api.Requests;
using GetJobAI.Optimisation.Api.Responses;
using GetJobAI.Optimisation.Contracts;
using GetJobAI.Optimisation.Data;
using GetJobAI.Optimisation.Data.Models;
using GetJobAI.Optimisation.OptimisationService.Contexts;
using GetJobAI.Optimisation.OptimisationService.Models;
using GetJobAI.Optimisation.Services;
using Microsoft.EntityFrameworkCore;

namespace GetJobAI.Optimisation.Api;

public static class OptimisationsEndpoints
{
    public static IEndpointRouteBuilder MapOptimisationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/optimisations/{optimisationId:guid}");

        group.MapPost("/work-experiences/{suggestionId:guid}/review", ReviewWorkExperience)
            .WithTags("Work Experiences")
            .WithSummary("Review a work experience suggestion")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/work-experiences/{suggestionId:guid}/rewrite", RewriteWorkExperience)
            .WithTags("Work Experiences")
            .WithSummary("Rewrite a work experience suggestion")
            .Produces<WorkExperienceRewriteResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway);

        group.MapPost("/bullets/{bulletId:guid}/review", ReviewBullet)
            .WithTags("Bullets")
            .WithSummary("Review a bullet point")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/activities/{suggestionId:guid}/review", ReviewActivity)
            .WithTags("Activities")
            .WithSummary("Review an activity suggestion")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/activities/{suggestionId:guid}/rewrite", RewriteActivity)
            .WithTags("Activities")
            .WithSummary("Rewrite an activity suggestion")
            .Produces<ActivityRewriteResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway);

        group.MapPost("/cover-letter/generate", GenerateCoverLetter)
            .WithTags("Cover Letter")
            .WithSummary("Generate a cover letter")
            .Produces<CoverLetterResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway);

        group.MapGet("/cover-letter", GetCoverLetter)
            .WithTags("Cover Letter")
            .WithSummary("Get the cover letter")
            .Produces<CoverLetterResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ReviewWorkExperience(
        Guid optimisationId,
        Guid suggestionId,
        ReviewRequest request,
        OptimisationDbContext db,
        CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);
        if (optimization is null) return Results.NotFound();

        var we = optimization.AiSuggestions.WorkExperiences
            .FirstOrDefault(x => x.Id == suggestionId);
        if (we is null) return Results.NotFound();

        we.Accepted = request.Accepted;
        we.RejectionHint = request.Accepted ? null : request.Hint;

        db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RewriteWorkExperience(
        Guid optimisationId,
        Guid suggestionId,
        RewriteRequest request,
        OptimisationDbContext db,
        IPromptRunner promptRunner,
        CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);
        if (optimization is null) return Results.NotFound();

        var we = optimization.AiSuggestions.WorkExperiences
            .FirstOrDefault(x => x.Id == suggestionId);
        if (we is null) return Results.NotFound();

        var ctx = OptimisationContextFactory.BuildContext(optimisationId, optimization.ResumeId, optimization.AiSuggestions);
        var entry = ctx.WorkExperiences.FirstOrDefault(e => e.EntryId == we.EntryId);
        if (entry is null) return Results.NotFound();

        var result = await promptRunner.RewriteExperienceAsync(entry, ctx, ct, request.Hint);
        if (!result.Success)
            return Results.Problem("AI rewrite failed", statusCode: 502);

        foreach (var bullet in result.Content.Bullets)
            bullet.Id = Guid.NewGuid();

        we.Include = result.Content.Include;
        we.Reason = result.Content.Reason;
        we.Bullets = result.Content.Bullets;
        we.Accepted = null;
        we.RejectionHint = null;
        we.RewriteCount = (we.RewriteCount ?? 0) + 1;

        db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new WorkExperienceRewriteResponse(
            we.Id,
            we.EntryId,
            we.Include,
            we.Reason,
            we.RewriteCount ?? 0,
            we.Bullets.Select(b => new BulletSuggestionResponse(
                b.Id, b.Original, b.Rewritten, b.KeywordsAdded, b.XyzApplied, b.Accepted)).ToList()));
    }

    private static async Task<IResult> ReviewBullet(
        Guid optimisationId,
        Guid bulletId,
        ReviewRequest request,
        OptimisationDbContext db,
        CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);
        if (optimization is null) return Results.NotFound();

        var bullet = optimization.AiSuggestions.WorkExperiences
            .SelectMany(we => we.Bullets)
            .FirstOrDefault(b => b.Id == bulletId);
        if (bullet is null) return Results.NotFound();

        bullet.Accepted = request.Accepted;

        db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ReviewActivity(
        Guid optimisationId,
        Guid suggestionId,
        ReviewRequest request,
        OptimisationDbContext db,
        CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);
        if (optimization is null) return Results.NotFound();

        var activity = optimization.AiSuggestions.Activities
            .FirstOrDefault(a => a.Id == suggestionId);
        if (activity is null) return Results.NotFound();

        activity.Accepted = request.Accepted;
        activity.RejectionHint = request.Accepted ? null : request.Hint;

        db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RewriteActivity(
        Guid optimisationId,
        Guid suggestionId,
        RewriteRequest request,
        OptimisationDbContext db,
        IPromptRunner promptRunner,
        CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);
        if (optimization is null) return Results.NotFound();

        var activity = optimization.AiSuggestions.Activities
            .FirstOrDefault(a => a.Id == suggestionId);
        if (activity is null) return Results.NotFound();

        var ctx = OptimisationContextFactory.BuildContext(optimisationId, optimization.ResumeId, optimization.AiSuggestions);
        var activityCtx = ctx.Activities.FirstOrDefault(a => a.EntryId == activity.EntryId);
        if (activityCtx is null) return Results.NotFound();

        var result = await promptRunner.RewriteActivityAsync(activityCtx, ctx, ct, request.Hint);
        if (!result.Success)
            return Results.Problem("AI rewrite failed", statusCode: 502);

        activity.Include = result.Content.Include;
        activity.Reason = result.Content.Reason;
        activity.HighlightsRewritten = result.Content.HighlightsRewritten;
        activity.Accepted = null;
        activity.RejectionHint = null;
        activity.RewriteCount = (activity.RewriteCount ?? 0) + 1;

        db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new ActivityRewriteResponse(
            activity.Id,
            activity.EntryId,
            activity.Include,
            activity.Reason,
            activity.HighlightsRewritten,
            activity.RewriteCount ?? 0));
    }

    private static async Task<IResult> GenerateCoverLetter(
        Guid optimisationId,
        GenerateCoverLetterRequest request,
        OptimisationDbContext db,
        IPromptRunner promptRunner,
        CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);
        if (optimization is null) return Results.NotFound();

        var doc = optimization.AiSuggestions;

        var acceptedSummary = doc.Summary?.Rewritten
            ?? doc.Summary?.Original
            ?? doc.ExistingSummary
            ?? string.Empty;

        var topAchievements = request.TopAchievements?.Count > 0
            ? request.TopAchievements
            : doc.WorkExperiences
                .Where(we => we.Include)
                .SelectMany(we => we.Bullets.Select(b => b.Rewritten))
                .Take(5)
                .ToList();

        var coverLetterCtx = new CoverLetterContext
        {
            JobTitle = doc.JobTitle,
            CompanyName = doc.CompanyName,
            CompanyDescription = request.CompanyDescription ?? string.Empty,
            CandidateName = doc.CandidateName,
            AcceptedSummary = acceptedSummary,
            TopAchievements = topAchievements,
            AcceptedSkills = doc.ResumeSkills,
            MissingKeywords = doc.MissingKeywords,
            Language = "en-GB",
            CustomNote = request.CustomNote,
            RewriteCount = doc.CoverLetter?.RewriteCount ?? 0
        };

        var result = await promptRunner.GenerateCoverLetterAsync(coverLetterCtx, ct);
        if (!result.Success)
            return Results.Problem("AI generation failed", statusCode: 502);

        var cl = result.Content;

        doc.CoverLetter = new StoredCoverLetter
        {
            CoverLetter = cl.CoverLetter,
            WordCount = cl.WordCount,
            SalutationUsed = cl.SalutationUsed,
            KeyPointsMade = cl.KeyPointsMade,
            Accepted = null,
            RewriteCount = (doc.CoverLetter?.RewriteCount ?? 0) + 1,
            GeneratedAt = DateTime.UtcNow
        };

        db.Entry(optimization).Property(o => o.AiSuggestions).IsModified = true;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToCoverLetterResponse(doc.CoverLetter));
    }

    private static async Task<IResult> GetCoverLetter(
        Guid optimisationId,
        OptimisationDbContext db,
        CancellationToken ct)
    {
        var optimization = await db.Optimizations
            .FirstOrDefaultAsync(o => o.Id == optimisationId, ct);

        if (optimization?.AiSuggestions.CoverLetter is null)
            return Results.NotFound();

        return Results.Ok(ToCoverLetterResponse(optimization.AiSuggestions.CoverLetter));
    }

    private static CoverLetterResponse ToCoverLetterResponse(StoredCoverLetter cl) =>
        new(cl.CoverLetter, cl.WordCount, cl.SalutationUsed, cl.KeyPointsMade, cl.Accepted, cl.RewriteCount, cl.GeneratedAt);
}
