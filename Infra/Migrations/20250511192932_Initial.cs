using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infra.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "AvailabilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTimeUtc = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTimeUtc = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ActiveFromUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ActiveUntilUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilityRules_Persons_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Otps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OtpCode = table.Column<string>(type: "text", nullable: false),
                    Expiration = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Otps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Otps_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedByIp = table.Column<string>(type: "text", nullable: true),
                    RevokedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    RevokedByIp = table.Column<string>(type: "text", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TutorApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRejected = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorApplications_Persons_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TutorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorProfiles_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PersonRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonRoles_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_PersonRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Qualifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qualifications_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TutorApplicationInterviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observations = table.Column<string>(type: "text", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorApplicationInterviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorApplicationInterviews_Persons_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_TutorApplicationInterviews_TutorApplications_TutorApplicati~",
                        column: x => x.TutorApplicationId,
                        principalTable: "TutorApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TutorDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DocumentPath = table.Column<string>(type: "text", nullable: false),
                    ModeratorReason = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    TutorApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorDocuments_TutorApplications_TutorApplicationId",
                        column: x => x.TutorApplicationId,
                        principalTable: "TutorApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "FieldVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ModeratorReason = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    TutorProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldVersions_TutorProfiles_TutorProfileId",
                        column: x => x.TutorProfileId,
                        principalTable: "TutorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    QualificationId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Levels_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TutorServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PricePerHour = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorServices_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_TutorServices_TutorProfiles_TutorProfileId",
                        column: x => x.TutorProfileId,
                        principalTable: "TutorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRules_OwnerId",
                table: "AvailabilityRules",
                column: "OwnerId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FieldVersions_TutorProfileId",
                table: "FieldVersions",
                column: "TutorProfileId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Levels_QualificationId",
                table: "Levels",
                column: "QualificationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Otps_PersonId",
                table: "Otps",
                column: "PersonId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoles_PersonId_RoleId",
                table: "PersonRoles",
                columns: new[] { "PersonId", "RoleId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoles_RoleId",
                table: "PersonRoles",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Persons_EmailAddress",
                table: "Persons",
                column: "EmailAddress",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_SubjectId",
                table: "Qualifications",
                column: "SubjectId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_PersonId",
                table: "RefreshTokens",
                column: "PersonId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TutorApplicationInterviews_ReviewerId",
                table: "TutorApplicationInterviews",
                column: "ReviewerId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TutorApplicationInterviews_TutorApplicationId",
                table: "TutorApplicationInterviews",
                column: "TutorApplicationId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_TutorApplications_ApplicantId",
                table: "TutorApplications",
                column: "ApplicantId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_TutorDocuments_TutorApplicationId",
                table: "TutorDocuments",
                column: "TutorApplicationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TutorProfiles_PersonId",
                table: "TutorProfiles",
                column: "PersonId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TutorServices_QualificationId",
                table: "TutorServices",
                column: "QualificationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TutorServices_TutorProfileId",
                table: "TutorServices",
                column: "TutorProfileId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AvailabilityRules");

            migrationBuilder.DropTable(name: "FieldVersions");

            migrationBuilder.DropTable(name: "Levels");

            migrationBuilder.DropTable(name: "Otps");

            migrationBuilder.DropTable(name: "PersonRoles");

            migrationBuilder.DropTable(name: "RefreshTokens");

            migrationBuilder.DropTable(name: "TutorApplicationInterviews");

            migrationBuilder.DropTable(name: "TutorDocuments");

            migrationBuilder.DropTable(name: "TutorServices");

            migrationBuilder.DropTable(name: "Roles");

            migrationBuilder.DropTable(name: "TutorApplications");

            migrationBuilder.DropTable(name: "Qualifications");

            migrationBuilder.DropTable(name: "TutorProfiles");

            migrationBuilder.DropTable(name: "Subjects");

            migrationBuilder.DropTable(name: "Persons");
        }
    }
}
