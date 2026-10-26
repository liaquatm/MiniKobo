using Microsoft.EntityFrameworkCore;
using MiniKobo.Orders.Api.Features.Carts;

namespace MiniKobo.Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    // Only the cart is exposed: items are always loaded and changed through their cart.
    public DbSet<Cart> Carts => Set<Cart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(cart =>
        {
            cart.ToTable("carts");
            cart.HasKey(c => c.Id);
            cart.Property(c => c.Id).ValueGeneratedNever();

            cart.HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(item =>
        {
            item.ToTable("cart_items");

            // One row per book per cart.
            item.HasKey(i => new { i.CartId, i.Isbn });

            item.Property(i => i.Isbn).HasMaxLength(13);
            item.Property(i => i.Title).HasMaxLength(300);
            item.Property(i => i.UnitPrice).HasPrecision(10, 2);
        });
    }
}
