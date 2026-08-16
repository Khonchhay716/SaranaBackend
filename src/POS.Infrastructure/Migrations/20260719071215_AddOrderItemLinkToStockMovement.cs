using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemLinkToStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                schema: "pos",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_OrderItemId",
                schema: "pos",
                table: "stock_movements",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_order_items_OrderItemId",
                schema: "pos",
                table: "stock_movements",
                column: "OrderItemId",
                principalSchema: "pos",
                principalTable: "order_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_order_items_OrderItemId",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_OrderItemId",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                schema: "pos",
                table: "stock_movements");
        }
    }
}
