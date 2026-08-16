using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addcolumnbeforeandafter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityAfter",
                schema: "pos",
                table: "stock_adjustments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityBefore",
                schema: "pos",
                table: "stock_adjustments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityAfter",
                schema: "pos",
                table: "stock_adjustments");

            migrationBuilder.DropColumn(
                name: "QuantityBefore",
                schema: "pos",
                table: "stock_adjustments");
        }
    }
}
