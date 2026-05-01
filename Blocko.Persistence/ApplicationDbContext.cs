using Bolcko.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Tender> Tenders { get; set; }
        public DbSet<TenderItem> TenderItems { get; set; }
        public DbSet<MarketPrice> MarketPrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Address configuration
            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Addresses)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasOne(d => d.ParentCategory)
                    .WithMany(p => p.SubCategories)
                    .HasForeignKey(d => d.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.RetailPrice).HasPrecision(18, 2);
                entity.Property(e => e.Weight).HasPrecision(18, 2);
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

                entity.HasOne(d => d.ShippingAddress)
                    .WithMany()
                    .HasForeignKey(d => d.ShippingAddressId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.BillingAddress)
                    .WithMany()
                    .HasForeignKey(d => d.BillingAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Tender configuration
            modelBuilder.Entity<Tender>(entity =>
            {
                entity.Property(e => e.TotalQuotedAmount).HasPrecision(18, 2);
            });

            // TenderItem configuration
            modelBuilder.Entity<TenderItem>(entity =>
            {
                entity.Property(e => e.RequestedQuantity).HasPrecision(18, 2);
                entity.Property(e => e.ProposedPricePerUnit).HasPrecision(18, 2);
                entity.Property(e => e.SubtotalItem).HasPrecision(18, 2);
            });

            // MarketPrice configuration
            modelBuilder.Entity<MarketPrice>(entity =>
            {
                entity.Property(e => e.Price).HasPrecision(18, 2);
            });
        }
    }
}