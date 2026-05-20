using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DiplomaExaminationCommissionOrderNumberString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE diploma."DiplomaExaminationCommissions"
                ALTER COLUMN "OrderNumber" TYPE character varying(64)
                USING "OrderNumber"::text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE diploma."DiplomaExaminationCommissions"
                ALTER COLUMN "OrderNumber" TYPE integer
                USING "OrderNumber"::integer;
                """);
        }
    }
}
