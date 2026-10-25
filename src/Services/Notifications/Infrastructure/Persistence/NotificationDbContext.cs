using Microsoft.EntityFrameworkCore;
using Notifications.Domain;

namespace Notifications.Infrastructure.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).ValueGeneratedNever();

            entity.Property(n => n.Recipient).HasMaxLength(100).IsRequired();
            entity.Property(n => n.Type).HasMaxLength(100).IsRequired();
            entity.Property(n => n.Subject).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Body).HasMaxLength(1000).IsRequired();
            entity.Property(n => n.Channel).HasMaxLength(30).IsRequired();

            // The duplicate guard: one notification per event, even if RabbitMQ delivers it twice.
            entity.HasIndex(n => n.EventId).IsUnique();
            entity.HasIndex(n => n.ClaimId);
        });
    }
}
