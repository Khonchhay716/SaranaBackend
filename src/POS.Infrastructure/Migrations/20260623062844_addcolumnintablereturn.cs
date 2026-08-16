using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addcolumnintablereturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "pos",
                table: "stock_returns",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                schema: "pos",
                table: "stock_returns",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                schema: "pos",
                table: "stock_return_items",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                schema: "pos",
                table: "stock_return_items",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "pos",
                table: "stock_returns");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                schema: "pos",
                table: "stock_returns");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                schema: "pos",
                table: "stock_return_items");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                schema: "pos",
                table: "stock_return_items");
        }
    }
}
