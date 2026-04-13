using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MTKPM_Clothing_Store_web.Models;

public partial class ClothingStoreContext : DbContext
{
    public ClothingStoreContext()
    {
    }

    public ClothingStoreContext(DbContextOptions<ClothingStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwCategoryProductCount> VwCategoryProductCounts { get; set; }

    public virtual DbSet<VwFeaturedProduct> VwFeaturedProducts { get; set; }

    public virtual DbSet<VwInStockProduct> VwInStockProducts { get; set; }

    public virtual DbSet<VwLowStock> VwLowStocks { get; set; }

    public virtual DbSet<VwOrderDetailsFull> VwOrderDetailsFulls { get; set; }

    public virtual DbSet<VwOrderSummary> VwOrderSummaries { get; set; }

    public virtual DbSet<VwProductList> VwProductLists { get; set; }

    public virtual DbSet<VwRecentOrder> VwRecentOrders { get; set; }

    public virtual DbSet<VwTopSellingProduct> VwTopSellingProducts { get; set; }

    public virtual DbSet<VwTotalOrderAmount> VwTotalOrderAmounts { get; set; }

    public virtual DbSet<VwUserOrder> VwUserOrders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=26.74.200.185,1433;Database=Clothing_Store;User Id=sa;Password=123456;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__categori__D54EE9B45ACB7384");

            entity.ToTable("categories", tb => tb.HasTrigger("trg_NoDeleteCategory"));

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__orders__46596229D07418E0");

            entity.ToTable("orders", tb => tb.HasTrigger("trg_SetDate"));

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("order_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__order_de__38E9A2240EE53992");

            entity.ToTable("order_details", tb =>
                {
                    tb.HasTrigger("trg_UpdateStock");
                    tb.HasTrigger("trg_UpdateStockAfterOrder");
                });

            entity.Property(e => e.DetailId).HasColumnName("detail_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderDetails_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_OrderDetails_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__products__47027DF51524F432");

            entity.ToTable("products", tb =>
                {
                    tb.HasTrigger("trg_CheckStock");
                    tb.HasTrigger("trg_LogDelete");
                    tb.HasTrigger("trg_LogInsert");
                    tb.HasTrigger("trg_PriceCheck");
                    tb.HasTrigger("trg_UpdateLog");
                });

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Pic)
                .HasMaxLength(255)
                .HasColumnName("pic");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370FBF36817C");

            entity.ToTable("users", tb =>
                {
                    tb.HasTrigger("trg_DefaultRole");
                    tb.HasTrigger("trg_UserDelete");
                });

            entity.HasIndex(e => e.Email, "UQ__users__AB6E616445D34C82").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("customer")
                .HasColumnName("role");
        });

        modelBuilder.Entity<VwCategoryProductCount>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CategoryProductCount");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.TotalProducts).HasColumnName("total_products");
        });

        modelBuilder.Entity<VwFeaturedProduct>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_FeaturedProducts");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Pic)
                .HasMaxLength(255)
                .HasColumnName("pic");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId)
                .ValueGeneratedOnAdd()
                .HasColumnName("product_id");
        });

        modelBuilder.Entity<VwInStockProduct>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_InStockProducts");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Pic)
                .HasMaxLength(255)
                .HasColumnName("pic");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId)
                .ValueGeneratedOnAdd()
                .HasColumnName("product_id");
        });

        modelBuilder.Entity<VwLowStock>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_LowStock");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Pic)
                .HasMaxLength(255)
                .HasColumnName("pic");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId)
                .ValueGeneratedOnAdd()
                .HasColumnName("product_id");
        });

        modelBuilder.Entity<VwOrderDetailsFull>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OrderDetailsFull");

            entity.Property(e => e.DetailId).HasColumnName("detail_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
        });

        modelBuilder.Entity<VwOrderSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OrderSummary");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.OrderDate)
                .HasColumnType("datetime")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
        });

        modelBuilder.Entity<VwProductList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ProductList");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Pic)
                .HasMaxLength(255)
                .HasColumnName("pic");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
        });

        modelBuilder.Entity<VwRecentOrder>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RecentOrders");

            entity.Property(e => e.OrderDate)
                .HasColumnType("datetime")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderId)
                .ValueGeneratedOnAdd()
                .HasColumnName("order_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<VwTopSellingProduct>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TopSellingProducts");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.TotalSold).HasColumnName("total_sold");
        });

        modelBuilder.Entity<VwTotalOrderAmount>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TotalOrderAmount");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Total)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total");
        });

        modelBuilder.Entity<VwUserOrder>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_UserOrders");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
