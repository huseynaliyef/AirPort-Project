using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class initalDBPostgresql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    CertificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    CorrectionNumber = table.Column<int>(type: "integer", nullable: false),
                    LTBegin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LTEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VTBegin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VTEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Interpretation = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ports");
        }
    }
}
