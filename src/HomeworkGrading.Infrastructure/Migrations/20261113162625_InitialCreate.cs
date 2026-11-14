using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkGrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false, collation: "NOCASE"),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeacherId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    ReferenceAnswer = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    Rubric = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    MaxScore = table.Column<long>(type: "INTEGER", nullable: false),
                    DueAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignments", x => x.Id);
                    table.CheckConstraint("CK_assignments_MaxScore", "\"MaxScore\" > 0");
                    table.ForeignKey(
                        name: "FK_assignments_accounts_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 30000, nullable: false),
                    SubmittedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Score = table.Column<long>(type: "INTEGER", nullable: true),
                    Feedback = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    GradedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissions", x => x.Id);
                    table.CheckConstraint("CK_submissions_GradeFields", "(\"Score\" IS NULL AND \"Feedback\" IS NULL AND \"GradedAt\" IS NULL)\nOR (\"Score\" IS NOT NULL AND \"Feedback\" IS NOT NULL AND \"GradedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_submissions_Score", "\"Score\" IS NULL OR \"Score\" >= 0");
                    table.ForeignKey(
                        name: "FK_submissions_accounts_StudentId",
                        column: x => x.StudentId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_submissions_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_Email",
                table: "accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assignments_CreatedAt_Id",
                table: "assignments",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_assignments_TeacherId_CreatedAt_Id",
                table: "assignments",
                columns: new[] { "TeacherId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_submissions_AssignmentId_StudentId",
                table: "submissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_AssignmentId_SubmittedAt_Id",
                table: "submissions",
                columns: new[] { "AssignmentId", "SubmittedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_submissions_StudentId",
                table: "submissions",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submissions");

            migrationBuilder.DropTable(
                name: "assignments");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
