using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class orderprocessfixv3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_discounts",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "order_item_serials",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "IX_orders_Status",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PointsUsed",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "LineDiscountAmount",
                schema: "pos",
                table: "order_items");

            migrationBuilder.RenameColumn(
                name: "GrandTotal",
                schema: "pos",
                table: "orders",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "DiscountTotal",
                schema: "pos",
                table: "orders",
                newName: "DiscountAmount");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "pos",
                table: "orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                schema: "pos",
                table: "orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "pos",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointEarned",
                schema: "pos",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PointUsed",
                schema: "pos",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "pos",
                table: "order_items",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountId",
                schema: "pos",
                table: "order_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumbers",
                schema: "pos",
                table: "order_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_items_DiscountId",
                schema: "pos",
                table: "order_items",
                column: "DiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_discounts_DiscountId",
                schema: "pos",
                table: "order_items",
                column: "DiscountId",
                principalSchema: "pos",
                principalTable: "discounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_discounts_DiscountId",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_items_DiscountId",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PointEarned",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PointUsed",
                schema: "pos",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "DiscountId",
                schema: "pos",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "SerialNumbers",
                schema: "pos",
                table: "order_items");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                schema: "pos",
                table: "orders",
                newName: "GrandTotal");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                schema: "pos",
                table: "orders",
                newName: "DiscountTotal");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "pos",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                schema: "pos",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "PointsUsed",
                schema: "pos",
                table: "orders",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineDiscountAmount",
                schema: "pos",
                table: "order_items",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "order_item_discounts",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscountId = table.Column<int>(type: "integer", nullable: false),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "order_item_serials",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    SerialStockId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_serials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_item_serials_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "pos",
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_item_serials_serial_stocks_SerialStockId",
                        column: x => x.SerialStockId,
                        principalSchema: "pos",
                        principalTable: "serial_stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status",
                schema: "pos",
                table: "orders",
                column: "Status");

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

            migrationBuilder.CreateIndex(
                name: "IX_order_item_serials_OrderItemId",
                schema: "pos",
                table: "order_item_serials",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_serials_SerialStockId",
                schema: "pos",
                table: "order_item_serials",
                column: "SerialStockId",
                unique: true);
        }
    }
}
