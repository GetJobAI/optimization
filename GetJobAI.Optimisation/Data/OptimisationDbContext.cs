using Microsoft.EntityFrameworkCore;

namespace GetJobAI.Optimisation.Data;

public class OptimisationDbContext(DbContextOptions<OptimisationDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Optimisation> Optimisations => Set<Entities.Optimisation>();

    public DbSet<Entities.OptimisationSummarySuggestion> SummarySuggestions => Set<Entities.OptimisationSummarySuggestion>();

    public DbSet<Entities.OptimisationWorkExperienceSuggestion> WorkExperienceSuggestions => Set<Entities.OptimisationWorkExperienceSuggestion>();

    public DbSet<Entities.OptimisationBulletSuggestion> BulletSuggestions => Set<Entities.OptimisationBulletSuggestion>();

    public DbSet<Entities.OptimisationActivitySuggestion> ActivitySuggestions => Set<Entities.OptimisationActivitySuggestion>();

    public DbSet<Entities.OptimisationSectionSuggestion> SectionSuggestions => Set<Entities.OptimisationSectionSuggestion>();

    public DbSet<Entities.OptimisationCoverLetter> CoverLetters => Set<Entities.OptimisationCoverLetter>();

    public DbSet<Entities.Resume> Resumes => Set<Entities.Resume>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OptimisationDbContext).Assembly);
    }
}
