using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestructureDiplomaExaminationCommissions : Migration
    {
        private static readonly string[] DecToMemberIndexColumns = ["DecMemberId", "DiplomaExaminationCommissionId"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Archives_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Archives");

            migrationBuilder.DropForeignKey(
                name: "FK_Defences_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Defences");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Groups_GroupId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropTable(
                name: "DecToMembers",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "DecMembers",
                schema: "diploma");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_GroupId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.AlterColumn<int>(
                name: "DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Archives",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql("UPDATE diploma.\"Archives\" SET \"DiplomaExaminationCommissionId\" = NULL;");
            migrationBuilder.Sql("UPDATE diploma.\"Defences\" SET \"DiplomaExaminationCommissionId\" = NULL;");
            migrationBuilder.Sql("DELETE FROM diploma.\"DiplomaExaminationCommissions\";");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.AddColumn<int>(
                name: "ThirdMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Groups",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "FirstMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HeadPersonaName",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeadPersonaPosition",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SecretaryId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Groups",
                column: "DiplomaExaminationCommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_FirstMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "FirstMemberTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "HeadTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_SecondMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecondMemberTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_SecretaryId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecretaryId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_ThirdMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "ThirdMemberTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Archives_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Archives",
                column: "DiplomaExaminationCommissionId",
                principalSchema: "diploma",
                principalTable: "DiplomaExaminationCommissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Defences_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Defences",
                column: "DiplomaExaminationCommissionId",
                principalSchema: "diploma",
                principalTable: "DiplomaExaminationCommissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Secretaries_SecretaryId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecretaryId",
                principalSchema: "diploma",
                principalTable: "Secretaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_FirstMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "FirstMemberTeacherId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "HeadTeacherId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_SecondMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecondMemberTeacherId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_ThirdMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "ThirdMemberTeacherId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_DiplomaExaminationCommissions_DiplomaExaminationComm~",
                schema: "diploma",
                table: "Groups",
                column: "DiplomaExaminationCommissionId",
                principalSchema: "diploma",
                principalTable: "DiplomaExaminationCommissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Archives_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Archives");

            migrationBuilder.DropForeignKey(
                name: "FK_Defences_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Defences");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Secretaries_SecretaryId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_FirstMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_SecondMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_ThirdMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_DiplomaExaminationCommissions_DiplomaExaminationComm~",
                schema: "diploma",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_FirstMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_SecondMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_SecretaryId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_ThirdMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "FirstMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "HeadPersonaName",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "HeadPersonaPosition",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "SecondMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "SecretaryId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "ThirdMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Archives",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DecMembers",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeacherId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecMembers_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalSchema: "diploma",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DecToMembers",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DecMemberId = table.Column<int>(type: "integer", nullable: false),
                    DiplomaExaminationCommissionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecToMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecToMembers_DecMembers_DecMemberId",
                        column: x => x.DecMemberId,
                        principalSchema: "diploma",
                        principalTable: "DecMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecToMembers_DiplomaExaminationCommissions_DiplomaExaminati~",
                        column: x => x.DiplomaExaminationCommissionId,
                        principalSchema: "diploma",
                        principalTable: "DiplomaExaminationCommissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_GroupId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "GroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecMembers_TeacherId",
                schema: "diploma",
                table: "DecMembers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DecToMembers_DecMemberId_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "DecToMembers",
                columns: DecToMemberIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecToMembers_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "DecToMembers",
                column: "DiplomaExaminationCommissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Archives_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Archives",
                column: "DiplomaExaminationCommissionId",
                principalSchema: "diploma",
                principalTable: "DiplomaExaminationCommissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Defences_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                schema: "diploma",
                table: "Defences",
                column: "DiplomaExaminationCommissionId",
                principalSchema: "diploma",
                principalTable: "DiplomaExaminationCommissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Groups_GroupId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "GroupId",
                principalSchema: "diploma",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
