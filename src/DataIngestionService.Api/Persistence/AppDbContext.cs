using DataIngestionService.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataIngestionService.Api.Persistence
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transactions");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.CustomerId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.TransactionDate)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .IsRequired();

                entity.Property(x => x.SourceChannel)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.DeduplicationHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => x.CustomerId);
                entity.HasIndex(x => x.TransactionDate);
                entity.HasIndex(x => x.Currency);
                entity.HasIndex(x => x.SourceChannel);

                entity.HasIndex(x => x.DeduplicationHash)
                    .IsUnique();
            });
        }
    }
}
