using Microsoft.EntityFrameworkCore;
using Akilli_Cihaz_Izleme_Sistemi_Server.Models;

namespace Akilli_Cihaz_Izleme_Sistemi_Server.Repository
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<User> Users => Set<User>();
        public DbSet<DeviceHistory> DeviceHistories => Set<DeviceHistory>();


        // EF Core, DB şemasını oluştururken bu metodu bir kere çağırır.
        // Migration oluştururken EF Core bu metoddan gelenlere göre gererkli SQL'i üretir.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // DbContext'in temel sınıfının kendi varsayılan model oluşturma mantığını
            // önce çalıştırır, sonradan yapılan ayarlar bunun üzerine eklenir.
            base.OnModelCreating(modelBuilder);

            // Tablolar burada oluşturulur.
            modelBuilder.Entity<DeviceHistory>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.HasIndex(h => new { h.DeviceId, h.Timestamp });
            });

            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
                entity.Property(d => d.Zone).HasMaxLength(50).IsRequired();
                entity.Property(d => d.Type).HasMaxLength(50).IsRequired();
                entity.Property(d => d.Status).HasMaxLength(20).IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Password).HasMaxLength(100).IsRequired();
            });
        }
    }
}