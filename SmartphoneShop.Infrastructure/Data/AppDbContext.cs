using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;

namespace SmartphoneShop.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Smartphone> Smartphones => Set<Smartphone>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ComparisonList> ComparisonLists => Set<ComparisonList>();
    public DbSet<ComparisonItem> ComparisonItems => Set<ComparisonItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<ExpertOpinion> ExpertOpinions => Set<ExpertOpinion>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<RepairSparePart> RepairSpareParts => Set<RepairSparePart>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Smartphone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.OldPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ScreenSize).HasColumnType("decimal(3,1)");
            entity.Property(e => e.Weight).HasColumnType("decimal(5,1)");
            entity.HasIndex(e => e.Brand);
            entity.HasIndex(e => e.IsFeatured);
        });

        builder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            entity.HasIndex(e => e.SessionId);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Smartphone)
                .WithMany(s => s.CartItems)
                .HasForeignKey(e => e.SmartphoneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ComparisonList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithOne(u => u.ComparisonList)
                .HasForeignKey<ComparisonList>(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            entity.HasIndex(e => e.SessionId);
        });

        builder.Entity<ComparisonItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ComparisonList)
                .WithMany(cl => cl.Items)
                .HasForeignKey(e => e.ComparisonListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Smartphone)
                .WithMany(s => s.ComparisonItems)
                .HasForeignKey(e => e.SmartphoneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Smartphone)
                .WithMany(s => s.Favorites)
                .HasForeignKey(e => e.SmartphoneId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.SmartphoneId }).IsUnique();
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DeliveryType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceAtPurchase).HasColumnType("decimal(10,2)");
            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Smartphone)
                .WithMany(s => s.OrderItems)
                .HasForeignKey(e => e.SmartphoneId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RepairRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EstimatedPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ServicePrice).HasColumnType("decimal(10,2)");
            entity.HasOne(e => e.User)
                .WithMany(u => u.RepairRequests)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.MasterUser)
                .WithMany()
                .HasForeignKey(e => e.MasterUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.RepairSpareParts)
                .WithOne(r => r.RepairRequest)
                .HasForeignKey(r => r.RepairRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.MasterUserId);
            entity.HasIndex(e => e.Status);
        });

        builder.Entity<RepairSparePart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SparePartPrice).HasColumnType("decimal(10,2)");
            entity.HasOne(e => e.RepairRequest)
                .WithMany(r => r.RepairSpareParts)
                .HasForeignKey(e => e.RepairRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SparePart)
                .WithMany()
                .HasForeignKey(e => e.SparePartId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.RepairRequestId);
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Smartphone)
                .WithMany(s => s.Reviews)
                .HasForeignKey(e => e.SmartphoneId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.SmartphoneId);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SmartphoneName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.ContactEmail).HasMaxLength(100);
            entity.Ignore(e => e.SmartphoneName);
            entity.Ignore(e => e.Quantity);
            entity.Ignore(e => e.SpecialRequests);
            entity.Ignore(e => e.DeliveryAddress);
            entity.Ignore(e => e.ContactPhone);
            entity.Ignore(e => e.ContactEmail);
            entity.Ignore(e => e.AdminComment);
            entity.HasOne(e => e.User)
                .WithMany(u => u.PurchaseOrders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Smartphone)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(e => e.SmartphoneId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        builder.Entity<ExpertOpinion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Expert)
                .WithMany(u => u.ExpertOpinions)
                .HasForeignKey(e => e.ExpertId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Smartphone1)
                .WithMany(s => s.ExpertOpinions1)
                .HasForeignKey(e => e.SmartphoneId1)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Smartphone2)
                .WithMany(s => s.ExpertOpinions2)
                .HasForeignKey(e => e.SmartphoneId2)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.SmartphoneId1, e.SmartphoneId2 });
        });
    }
}
