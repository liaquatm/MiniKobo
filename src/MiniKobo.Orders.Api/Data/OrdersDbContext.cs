using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using MiniKobo.Orders.Api.Features.Carts;
using MiniKobo.Orders.Api.Features.Orders;

namespace MiniKobo.Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    // Only the aggregate roots are exposed: items are always loaded and changed through their parent.
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>
    /// A context that joins an existing connection and transaction. Message handlers use this with
    /// NServiceBus's storage session, so our changes and the outbox commit in one transaction.
    /// </summary>
    public static OrdersDbContext ForTransaction(DbConnection connection, DbTransaction transaction)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        var db = new OrdersDbContext(options);
        db.Database.UseTransaction(transaction);
        return db;
    }

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

        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.Id).ValueGeneratedNever();
            order.Property(o => o.CustomerName).HasMaxLength(200);
            order.Property(o => o.CustomerEmail).HasMaxLength(320);
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            order.Property(o => o.FailureReason).HasMaxLength(500);

            order.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.ToTable("order_items");
            item.HasKey(i => new { i.OrderId, i.Isbn });
            item.Property(i => i.Isbn).HasMaxLength(13);
            item.Property(i => i.Title).HasMaxLength(300);
            item.Property(i => i.UnitPrice).HasPrecision(10, 2);
        });
    }
}
