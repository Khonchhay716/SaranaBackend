using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addcolumnwarranty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarrantyDays",
                schema: "pos",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WarrantyEndDate",
                schema: "pos",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WarrantyStartDate",
                schema: "pos",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarrantyDays",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "WarrantyEndDate",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "WarrantyStartDate",
                schema: "pos",
                table: "order_items");
        }
    }
}
