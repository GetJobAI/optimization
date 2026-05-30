using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GetJobAI.Optimisation.Migrations
{
    /// <inheritdoc />
    public partial class use_optimizations_jsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop all optimizer-owned tables from the EF init migration (if they were ever applied).
            // The actual schema is owned by core-api migrations.
            migrationBuilder.Sql("DROP TABLE IF EXISTS optimisation_bullet_suggestions CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS optimisation_work_experience_suggestions CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS optimisation_activity_suggestions CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS optimisation_section_suggestions CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS optimisation_summary_suggestions CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS optimisation_cover_letters CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS optimisations CASCADE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
