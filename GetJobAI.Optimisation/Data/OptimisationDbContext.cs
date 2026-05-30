using Microsoft.EntityFrameworkCore;

namespace GetJobAI.Optimisation.Data;

public class OptimisationDbContext(DbContextOptions<OptimisationDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Resume> Resumes => Set<Entities.Resume>();

    public DbSet<Entities.AtsScore> AtsScores => Set<Entities.AtsScore>();

    public DbSet<Entities.Optimization> Optimizations => Set<Entities.Optimization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OptimisationDbContext).Assembly);
    }
}
