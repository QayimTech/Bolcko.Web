using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Project;
using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Entities.ShoppingCart;
using Bolcko.Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Blocko.Persistence
{
    public class BlockoDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public BlockoDbContext(DbContextOptions<BlockoDbContext> options) : base(options)
        {
        }

        public DbSet<Bolcko.Domain.Entities.User.Address> Addresses { get; set; }
        public DbSet<Bolcko.Domain.Entities.Catalog.Category> Categories { get; set; }
        public DbSet<Bolcko.Domain.Entities.Product.Product> Products { get; set; }
        public DbSet<Bolcko.Domain.Entities.Order.Order> Orders { get; set; }
        public DbSet<Bolcko.Domain.Entities.Project.Project> Projects { get; set; }
        public DbSet<Bolcko.Domain.Entities.Tender.Tender> Tenders { get; set; }
        public DbSet<Bolcko.Domain.Entities.Tender.TenderItem> TenderItems { get; set; }
        public DbSet<Bolcko.Domain.Entities.Catalog.MarketPrice> MarketPrices { get; set; }
        public DbSet<Bolcko.Domain.Entities.SEO.SEOMetadata> SEOMetadata { get; set; }
        public DbSet<Bolcko.Domain.Entities.ShoppingCart.ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<Bolcko.Domain.Entities.ShoppingCart.ShoppingCartItem> ShoppingCartItems { get; set; }  
        public DbSet<Bolcko.Domain.Entities.Order.OrderItem> OrderItems { get; set; }
        public DbSet<Bolcko.Domain.Entities.Product.ProductImage> ProductImages { get; set; }
        public DbSet<Bolcko.Domain.Entities.Product.ProductVariant> ProductVariants { get; set; }
        public DbSet<Bolcko.Domain.Entities.Setting.AppSetting> AppSettings { get; set; }
        public DbSet<Bolcko.Domain.Entities.Setting.ShippingRate> ShippingRates { get; set; }
        public DbSet<Bolcko.Domain.Entities.Setting.Coupon> Coupons { get; set; }
        public DbSet<Bolcko.Domain.Entities.User.Notification> Notifications { get; set; }
        
        // Delivery
        public DbSet<DeliveryCompany> DeliveryCompanies { get; set; }
        public DbSet<DeliveryDriver> DeliveryDrivers { get; set; }
        public DbSet<DeliveryJob> DeliveryJobs { get; set; }
        public DbSet<DeliveryBid> DeliveryBids { get; set; }
        public DbSet<DeliveryRating> DeliveryRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
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

            // OrderItem configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Order.OrderItem>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.Items)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Product)
                    .WithMany()
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ProductVariant)
                    .WithMany()
                    .HasForeignKey(d => d.ProductVariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ProductImage configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Product.ProductImage>(entity =>
            {
                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ProductVariant configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Product.ProductVariant>(entity =>
            {
                entity.Property(e => e.Price).HasPrecision(18, 2);

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Variants)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                entity.Property(e => e.TargetPricePerUnit).HasPrecision(18, 2);
                entity.Property(e => e.SubtotalItem).HasPrecision(18, 2);

                entity.HasOne(d => d.ProductVariant)
                    .WithMany()
                    .HasForeignKey(d => d.ProductVariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // MarketPrice configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.Catalog.MarketPrice>(entity =>
            {
                entity.Property(e => e.Price).HasPrecision(18, 2);
            });

            // ShoppingCart configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.ShoppingCart.ShoppingCart>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ShoppingCartItem configuration
            modelBuilder.Entity<Bolcko.Domain.Entities.ShoppingCart.ShoppingCartItem>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);

                entity.HasOne(d => d.ShoppingCart)
                    .WithMany(p => p.Items)
                    .HasForeignKey(d => d.ShoppingCartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Product)
                    .WithMany()
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ProductVariant)
                    .WithMany()
                    .HasForeignKey(d => d.ProductVariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Bolcko.Domain.Entities.Setting.ShippingRate>(entity =>
            {
                entity.Property(e => e.Rate).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Bolcko.Domain.Entities.Setting.Coupon>(entity =>
            {
                entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
            });

            // Delivery configurations
            modelBuilder.Entity<DeliveryCompany>(entity =>
            {
                entity.Property(e => e.BaseDeliveryRate).HasPrecision(18, 2);
            });

            modelBuilder.Entity<DeliveryDriver>(entity =>
            {
                entity.Property(e => e.AverageRating).HasPrecision(18, 2);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.Drivers)
                    .HasForeignKey(d => d.DeliveryCompanyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DeliveryJob>(entity =>
            {
                entity.Property(e => e.DeliveryFee).HasPrecision(18, 2);

                entity.HasOne(d => d.Order)
                    .WithMany()
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Driver)
                    .WithMany(p => p.Jobs)
                    .HasForeignKey(d => d.DriverId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DeliveryBid>(entity =>
            {
                entity.Property(e => e.BidAmount).HasPrecision(18, 2);

                entity.HasOne(d => d.DeliveryJob)
                    .WithMany(p => p.Bids)
                    .HasForeignKey(d => d.DeliveryJobId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Driver)
                    .WithMany(p => p.Bids)
                    .HasForeignKey(d => d.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DeliveryRating>(entity =>
            {
                entity.HasOne(d => d.DeliveryJob)
                    .WithOne(p => p.Rating)
                    .HasForeignKey<DeliveryRating>(d => d.DeliveryJobId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Driver)
                    .WithMany(p => p.Ratings)
                    .HasForeignKey(d => d.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Customer)
                    .WithMany()
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Bolcko.Domain.Entities.User.Notification>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}