using Claims.Domain;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence;

public sealed class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Ignore(c => c.DomainEvents);

            entity.Property(c => c.PolicyNumber).HasMaxLength(50).IsRequired();
            entity.Property(c => c.ClaimantId).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(Claim.MaxDescriptionLength).IsRequired();
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);

            entity.OwnsOne(c => c.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            entity.OwnsOne(c => c.Decision, decision =>
            {
                decision.Property(d => d.Outcome).HasColumnName("DecisionOutcome").HasConversion<string>().HasMaxLength(20);
                decision.Property(d => d.Reason).HasColumnName("DecisionReason").HasMaxLength(500);
                decision.Property(d => d.DecidedBy).HasColumnName("DecidedBy").HasMaxLength(100);
                decision.Property(d => d.DecidedAt).HasColumnName("DecidedAt");
            });
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(o=> o.Type).HasMaxLength(200).IsRequired();
                entity.Property(o=> o.Payload).HasColumnType("jsonb").IsRequired();
                entity.Property(o=> o.Error).HasMaxLength(75);
                entity.HasIndex(o=> o.OccurredAt).HasFilter("\"ProcessedAt\" IS NULL");
            }
        );
    }
}
