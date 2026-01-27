using BrEconomy.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrEconomy.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<EconomicIndicator> EconomicIndicators { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Garante que o índice seja rápido para buscas por Nome
            modelBuilder.Entity<EconomicIndicator>()
                .HasIndex(e => e.Name);
        }
    }
}
