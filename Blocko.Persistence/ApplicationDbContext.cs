using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Project;
using Bolcko.Domain.Entities.Tender;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Bolcko.Domain.Entities.User.User> Users { get; set; }
        public DbSet<Bolcko.Domain.Entities.User.Address> Addresses { get; set; }
        public DbSet<Bolcko.Domain.Entities.Catalog.Category> Categories { get; set; }
        public DbSet<Bolcko.Domain.Entities.Product.Product> Products { get; set; }
        public DbSet<Bolcko.Domain.Entities.Order.Order> Orders { get; set; }
        public DbSet<Bolcko.Domain.Entities.Project.Project> Projects { get; set; }
        public DbSet<Bolcko.Domain.Entities.Tender.Tender> Tenders { get; set; }
        public DbSet<Bolcko.Domain.Entities.Tender.TenderItem> TenderItems { get; set; }
        public DbSet<Bolcko.Domain.Entities.Catalog.MarketPrice> MarketPrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.User.User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Address configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.User.Address>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Addresses)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Category configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Catalog.Category>(entity =>
            {
                entity.HasOne(d => d.ParentCategory)
                    .WithMany(p => p.SubCategories)
                    .HasForeignKey(d => d.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Product.Product>(entity =>
            {
                entity.Property(e => e.RetailPrice).HasPrecision(18, 2);
                entity.Property(e => e.Weight).HasPrecision(18, 2);
            });

            // Order configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Order.Order>(entity =>
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
            modelBuilder.Entity<Bolcko.Domain.Entities.Tender.Tender>(entity =>
            {
                entity.Property(e => e.TotalQuotedAmount).HasPrecision(18, 2);
            });

            // TenderItem configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Tender.TenderItem>(entity =>
            {
                entity.Property(e => e.RequestedQuantity).HasPrecision(18, 2);
                entity.Property(e => e.ProposedPricePerUnit).HasPrecision(18, 2);
                entity.Property(e => e.SubtotalItem).HasPrecision(18, 2);
            });

            // MarketPrice configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Catalog.MarketPrice>(entity =>
            {
                entity.Property(e => e.Price).HasPrecision(18, 2);
            });
        }
    }
}