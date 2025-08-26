using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data
{
    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients"); // Table name in DB
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ClientId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.ClientSecret)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.DisplayName)
                      .HasMaxLength(200);
            });
        }
    }
}
