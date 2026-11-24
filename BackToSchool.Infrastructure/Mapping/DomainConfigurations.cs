using BackToSchool.core.Entities.Catalog;
using BackToSchool.core.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackToSchool.Infrastructure.Mapping;

public class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> entity)
    {
        entity.ToTable("school");
        entity.HasKey(e => e.SchoolId);
        entity.Property(e => e.SchoolId).HasColumnName("school_id").ValueGeneratedOnAdd();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
        entity.Property(e => e.IsActive).HasColumnName("is_active");
    }
}

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> entity)
    {
        entity.ToTable("course");
        entity.HasKey(e => e.CourseId);
        entity.Property(e => e.CourseId).HasColumnName("course_id").ValueGeneratedOnAdd();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        entity.Property(e => e.Level).HasColumnName("level").HasMaxLength(20).IsRequired();
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
        entity.Property(e => e.IsActive).HasColumnName("is_active");
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("product");
        entity.HasKey(e => e.ProductId);
        entity.Property(e => e.ProductId).HasColumnName("product_id").ValueGeneratedOnAdd();
        entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(30);
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        entity.Property(e => e.Section).HasColumnName("section").HasMaxLength(20).IsRequired();
        entity.Property(e => e.AccountGroup).HasColumnName("account_group").HasMaxLength(100);
        entity.Property(e => e.GarmentName).HasColumnName("garment_name").HasMaxLength(150);
        entity.Property(e => e.Size).HasColumnName("size").HasMaxLength(20);
        entity.Property(e => e.SizeOrder).HasColumnName("size_order");
        entity.Property(e => e.Price).HasColumnName("price").HasPrecision(12, 2);
        entity.Property(e => e.UnitCost).HasColumnName("unit_cost").HasPrecision(12, 2);
        entity.Property(e => e.Stock).HasColumnName("stock").HasPrecision(12, 2);
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}

public class CourseProductConfiguration : IEntityTypeConfiguration<CourseProduct>
{
    public void Configure(EntityTypeBuilder<CourseProduct> entity)
    {
        entity.ToTable("course_product");
        entity.HasKey(e => e.CourseProductId);
        entity.Property(e => e.CourseProductId).HasColumnName("course_product_id").ValueGeneratedOnAdd();
        entity.Property(e => e.CourseId).HasColumnName("course_id");
        entity.Property(e => e.ProductId).HasColumnName("product_id");
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
        entity.Property(e => e.IsDefaultSelected).HasColumnName("is_default_selected");
    }
}

public class SaleOrderConfiguration : IEntityTypeConfiguration<SaleOrder>
{
    public void Configure(EntityTypeBuilder<SaleOrder> entity)
    {
        entity.ToTable("sale_order");
        entity.HasKey(e => e.SaleOrderId);
        entity.Property(e => e.SaleOrderId).HasColumnName("sale_order_id").ValueGeneratedOnAdd();
        entity.Property(e => e.TicketNumber).HasColumnName("ticket_number");
        entity.Property(e => e.TicketDate).HasColumnName("ticket_date");
        entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.TaxId).HasColumnName("tax_id").HasMaxLength(30);
        entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(30);
        entity.Property(e => e.SchoolId).HasColumnName("school_id");
        entity.Property(e => e.SchoolName).HasColumnName("school_name").HasMaxLength(150);
        entity.Property(e => e.PromoterAppUserId).HasColumnName("promoter_app_user_id");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(e => e.Total).HasColumnName("total").HasPrecision(12, 2);
        entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.PrintedAt).HasColumnName("printed_at");
        entity.Property(e => e.ReadyAt).HasColumnName("ready_at");
        entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
        entity.Property(e => e.CancelReason).HasColumnName("cancel_reason").HasMaxLength(250);
        entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.SaleOrderId);
    }
}

public class SaleOrderLineConfiguration : IEntityTypeConfiguration<SaleOrderLine>
{
    public void Configure(EntityTypeBuilder<SaleOrderLine> entity)
    {
        entity.ToTable("sale_order_line");
        entity.HasKey(e => e.SaleOrderLineId);
        entity.Property(e => e.SaleOrderLineId).HasColumnName("sale_order_line_id").ValueGeneratedOnAdd();
        entity.Property(e => e.SaleOrderId).HasColumnName("sale_order_id");
        entity.Property(e => e.ProductId).HasColumnName("product_id");
        entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(30);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(250).IsRequired();
        entity.Property(e => e.Size).HasColumnName("size").HasMaxLength(20);
        entity.Property(e => e.Section).HasColumnName("section").HasMaxLength(20).IsRequired();
        entity.Property(e => e.CourseId).HasColumnName("course_id");
        entity.Property(e => e.CourseName).HasColumnName("course_name").HasMaxLength(100);
        entity.Property(e => e.StudentLabel).HasColumnName("student_label").HasMaxLength(100);
        entity.Property(e => e.Quantity).HasColumnName("quantity");
        entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
        entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2);
        entity.Property(e => e.IsManual).HasColumnName("is_manual");
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
    }
}

public class TicketCounterConfiguration : IEntityTypeConfiguration<TicketCounter>
{
    public void Configure(EntityTypeBuilder<TicketCounter> entity)
    {
        entity.ToTable("ticket_counter");
        entity.HasKey(e => e.TicketDate);
        entity.Property(e => e.TicketDate).HasColumnName("ticket_date");
        entity.Property(e => e.LastNumber).HasColumnName("last_number");
    }
}
