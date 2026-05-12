using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIS.DataAcces.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "ideas",
                type: "datetime",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "ideas");
        }
    }
}
