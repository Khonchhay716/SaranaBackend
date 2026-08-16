using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class orderprocessfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_discounts_ProductDiscountId",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_discounts_DiscountId",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_DiscountId",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_items_ProductDiscountId",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "DiscountId",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "LineDiscountPercent",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ProductDiscountId",
                schema: "pos",
                table: "order_items");

            migrationBuilder.CreateTable(
                name: "order_item_discounts",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    DiscountId = table.Column<int>(type: "integer", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_discounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_item_discounts_discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalSchema: "pos",
                        principalTable: "discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_item_discounts_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "pos",
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_item_discounts_DiscountId",
                schema: "pos",
                table: "order_item_discounts",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_discounts_OrderItemId",
                schema: "pos",
                table: "order_item_discounts",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_discounts",
                schema: "pos");

            migrationBuilder.AddColumn<int>(
                name: "DiscountId",
                schema: "pos",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LineDiscountPercent",
                schema: "pos",
                table: "order_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductDiscountId",
                schema: "pos",
                table: "order_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_DiscountId",
                schema: "pos",
                table: "orders",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductDiscountId",
                schema: "pos",
                table: "order_items",
                column: "ProductDiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_discounts_ProductDiscountId",
                schema: "pos",
                table: "order_items",
                column: "ProductDiscountId",
                principalSchema: "pos",
                principalTable: "discounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_discounts_DiscountId",
                schema: "pos",
                table: "orders",
                column: "DiscountId",
                principalSchema: "pos",
                principalTable: "discounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
