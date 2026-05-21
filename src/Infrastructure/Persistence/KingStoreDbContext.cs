using KingStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace KingStore.Infrastructure.Persistence;

public class KingStoreDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public KingStoreDbContext(DbContextOptions<KingStoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Shoe> Shoes { get; set; } = null!;
    public DbSet<ShoeImage> ShoeImages { get; set; } = null!; // إضافة جدول الصور
    public DbSet<UserAddress> UserAddresses { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<ShippingCompany> ShippingCompanies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- إعدادات الحذاء (Shoe) ---
        modelBuilder.Entity<Shoe>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Price).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FootLength).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Size).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Stock).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.Property(e => e.Gender)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion(
                    v => v.ToString(),
                    v => (Gender)Enum.Parse(typeof(Gender), v));

            // إعداد العلاقة مع الصور (One-to-Many)
            // نستخدم الحقل الخاص _images للوصول للبيانات
            entity.HasMany(e => e.Images)
                .WithOne(i => i.Shoe)
                .HasForeignKey(i => i.ShoeId)
                .OnDelete(DeleteBehavior.Cascade); // حذف الصور عند حذف الحذاء

            entity.Navigation(e => e.Images)
                .HasField("_images")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // --- إعدادات صور الحذاء (ShoeImage) ---
        modelBuilder.Entity<ShoeImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PublicId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IsPrimary).IsRequired();
        });

        // --- إعدادات المستخدم (User) ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasMany(e => e.Addresses)
                .WithOne()
                .HasForeignKey(a => a.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(e => e.Addresses)
                .HasField("_addresses")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // --- إعدادات عنوان المستخدم (UserAddress) ---
        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(100);
        });

        // --- إعدادات الطلب (Order) ---
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmountUsd).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.ShippingCostSyp).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(30);

            entity.HasOne(o => o.ShippingCompany)
                .WithMany()
                .HasForeignKey(o => o.ShippingCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(e => e.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // --- إعدادات عناصر الطلب (OrderItem) ---
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).IsRequired().HasPrecision(18, 2);
            
            entity.HasOne(e => e.Shoe)
                .WithMany()
                .HasForeignKey(e => e.ShoeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- إعدادات شركات الشحن (ShippingCompany) ---
        modelBuilder.Entity<ShippingCompany>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ShippingPrice).HasPrecision(18, 2);
        });

        // ================= SEED DATA =================
        var staticSeedDate = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Shoe>().HasData(
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Air Jordan 1 High", Brand = "Nike", Description = "Legendary sneaker.", Price = 180.00m, Category = "Sneakers", Gender = Gender.Men, FootLength = 27.0m, Size = 42.5m, Stock = 12, CreatedAt = staticSeedDate },
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Ultraboost 22", Brand = "Adidas", Description = "Running comfort.", Price = 190.00m, Category = "Running", Gender = Gender.Men, FootLength = 28.5m, Size = 44.0m, Stock = 8, CreatedAt = staticSeedDate },
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Classic Leather White", Brand = "Reebok", Description = "Casual look.", Price = 90.00m, Category = "Casual", Gender = Gender.Women, FootLength = 24.0m, Size = 38.0m, Stock = 15, CreatedAt = staticSeedDate }
        );

        modelBuilder.Entity<ShippingCompany>().HasData(
            new { Id = Guid.Parse("f1111111-1111-1111-1111-111111111111"), Name = "شركة القدموس للشحن", Governorate = "دمشق", BranchName = "فرع الحريقة", ShippingPrice = 25000m, ExpectedDays = "24-48 ساعة" },
            new { Id = Guid.Parse("f5555555-5555-5555-5555-555555555555"), Name = "شركة الهرم", Governorate = "اللاذقية", BranchName = "فرع الشيخ ضاهر", ShippingPrice = 25000m, ExpectedDays = "24-48 ساعة" }
        );
    }
}