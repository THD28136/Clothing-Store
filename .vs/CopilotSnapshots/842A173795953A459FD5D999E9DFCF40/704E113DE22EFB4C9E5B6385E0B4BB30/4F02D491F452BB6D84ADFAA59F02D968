using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTKPM_Clothing_Store_web.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestColumnsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__categori__D54EE9B45ACB7384", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    coupon_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    coupon_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    discount_percent = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__coupons__58CF6389BA62B676", x => x.coupon_id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    payment_method_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    method_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__payment___8A3EA9EB700AE1A7", x => x.payment_method_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "customer"),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__users__B9BE370FBF36817C", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    image = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: true),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    is_featured = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__products__47027DF51524F432", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_Products_Categories",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "carts",
                columns: table => new
                {
                    cart_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__carts__2EF52A27DB9C171D", x => x.cart_id);
                    table.ForeignKey(
                        name: "FK_Carts_Users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    order_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    coupon_id = table.Column<int>(type: "int", nullable: true),
                    payment_method_id = table.Column<int>(type: "int", nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuestPhone = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__orders__46596229D07418E0", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_Orders_Coupons",
                        column: x => x.coupon_id,
                        principalTable: "coupons",
                        principalColumn: "coupon_id");
                    table.ForeignKey(
                        name: "FK_Orders_PaymentMethods",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "payment_method_id");
                    table.ForeignKey(
                        name: "FK_Orders_Users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    rating = table.Column<int>(type: "int", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    review_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__reviews__60883D9023838BDB", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_Reviews_Products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "FK_Reviews_Users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    cart_item_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cart_id = table.Column<int>(type: "int", nullable: false),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__cart_ite__5D9A6C6E95C80E98", x => x.cart_item_id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts",
                        column: x => x.cart_id,
                        principalTable: "carts",
                        principalColumn: "cart_id");
                    table.ForeignKey(
                        name: "FK_CartItems_Products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "order_details",
                columns: table => new
                {
                    detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: true),
                    product_id = table.Column<int>(type: "int", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__order_de__38E9A2240EE53992", x => x.detail_id);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "FK_OrderDetails_Products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_product_id",
                table: "cart_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Cart_Product",
                table: "cart_items",
                columns: new[] { "cart_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__carts__B9BE370EE7FD5133",
                table: "carts",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__coupons__357D4CF9D56E4FC0",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_details_order_id",
                table: "order_details",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_details_product_id",
                table: "order_details",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_coupon_id",
                table: "orders",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_payment_method_id",
                table: "orders",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__payment___2DA2FAEE775E56F9",
                table: "payment_methods",
                column: "method_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_product_id",
                table: "reviews",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_user_id",
                table: "reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__users__AB6E616445D34C82",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_items");

            migrationBuilder.DropTable(
                name: "order_details");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "carts");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
