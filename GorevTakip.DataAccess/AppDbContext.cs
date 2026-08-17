using GorevTakip.Entities;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.DataAccess
{
    // EF Core'un DbContext sınıfından miras alıyoruz.
    public class AppDbContext : DbContext
    {
        // Bu constructor (yapıcı metot), veritabanı bağlantı ayarlarını (Connection String) dışarıdan alabilmemizi sağlar.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veritabanındaki tablolarımızı temsil eden DbSet'ler. 
        // İsimleri çoğul yaparız ki tablolar "Users" ve "Tasks" olarak oluşsun.
        public DbSet<User> Users { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskComment> Comments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }

        // Tablo ilişkilerini ve kısıtlamalarını detaylı ayarladığımız (Fluent API) metot
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TaskItem tablosundaki AssignedUserId ile User tablosundaki Id'yi bağlıyoruz.
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedUser) // Bir TaskItem'ın bir AssignedUser'ı vardır.
                .WithMany(u => u.Tasks)      // Bir User'ın birden çok Task'i olabilir.
                .HasForeignKey(t => t.AssignedUserId); // Bağlantıyı kuran sütun (Foreign Key).
                
            modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);

            base.OnModelCreating(modelBuilder);
        }
    }
}