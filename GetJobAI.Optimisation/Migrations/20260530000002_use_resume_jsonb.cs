using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GetJobAI.Optimisation.Migrations
{
    /// <inheritdoc />
    public partial class use_resume_jsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop child resume tables created by the init migration (if they exist).
            // The resumes table is owned by core-api and is excluded from EF migrations.
            migrationBuilder.Sql("DROP TABLE IF EXISTS resume_work_experiences CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS resume_skills CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS resume_publications CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS resume_activities CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS resume_additional_sections CASCADE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Child tables are not recreated on rollback — resume data lives in resumes.content JSONB.
        }
    }
}
