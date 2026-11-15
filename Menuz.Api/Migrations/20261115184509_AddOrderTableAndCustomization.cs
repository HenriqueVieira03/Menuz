using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menuz.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTableAndCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TableNumber",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "Addons",
                table: "OrderItems",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "OrderItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "RemovedIngredients",
                table: "OrderItems",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<string>(
                name: "SizeName",
                table: "OrderItems",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TableNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Addons",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "RemovedIngredients",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SizeName",
                table: "OrderItems");
        }
    }
}
