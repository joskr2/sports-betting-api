using Microsoft.EntityFrameworkCore;
using SportsBetting.Api.Core.Entities;

namespace SportsBetting.Api.Infrastructure.Data
{

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Bet> Bets { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Email");
                
                entity.Property(u => u.Balance)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(1000.00m);
                
                entity.Property(u => u.Email)
                    .HasMaxLength(255)
                    .IsRequired();
                
                entity.Property(u => u.FullName)
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(u => u.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();
                
                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.Property(u => u.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
            
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.TeamAOdds)
                    .HasColumnType("decimal(10,2)");
                
                entity.Property(e => e.TeamBOdds)
                    .HasColumnType("decimal(10,2)");
                
                entity.Property(e => e.Name)
                    .HasMaxLength(200)
                    .IsRequired();
                
                entity.Property(e => e.TeamA)
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.TeamB)
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.HasIndex(e => e.EventDate)
                    .HasDatabaseName("IX_Events_EventDate");
                
                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Events_Status");

                entity.Property(e => e.Status)
                    .HasConversion<string>();
            });
            
            modelBuilder.Entity<Bet>(entity =>
            {
                entity.Property(b => b.Amount)
                    .HasColumnType("decimal(18,2)");
                
                entity.Property(b => b.Odds)
                    .HasColumnType("decimal(10,2)");
                
                entity.HasOne(b => b.User)
                    .WithMany(u => u.Bets)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(b => b.Event)
                    .WithMany(e => e.Bets)
                    .HasForeignKey(b => b.EventId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.Property(b => b.SelectedTeam)
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(b => b.Status)
                    .HasConversion<string>();
                
                entity.HasIndex(b => b.UserId)
                    .HasDatabaseName("IX_Bets_UserId");
                
                entity.HasIndex(b => b.EventId)
                    .HasDatabaseName("IX_Bets_EventId");
                
                entity.HasIndex(b => b.Status)
                    .HasDatabaseName("IX_Bets_Status");
                
                entity.HasIndex(b => b.CreatedAt)
                    .HasDatabaseName("IX_Bets_CreatedAt");
            });
            
            SeedData(modelBuilder);
            
            base.OnModelCreating(modelBuilder);
        }
        
        private void SeedData(ModelBuilder modelBuilder)
        {
            var events = new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Name = "Real Madrid vs Barcelona - El Clásico",
                    TeamA = "Real Madrid",
                    TeamB = "Barcelona",
                    TeamAOdds = 2.10m,
                    TeamBOdds = 1.95m,
                    EventDate = DateTime.UtcNow.AddDays(7),
                    Status = EventStatus.Upcoming,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Event
                {
                    Id = 2,
                    Name = "Manchester United vs Chelsea - Premier League",
                    TeamA = "Manchester United",
                    TeamB = "Chelsea",
                    TeamAOdds = 1.85m,
                    TeamBOdds = 2.00m,
                    EventDate = DateTime.UtcNow.AddDays(5),
                    Status = EventStatus.Upcoming,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Event
                {
                    Id = 3,
                    Name = "Liverpool vs Arsenal - Premier League",
                    TeamA = "Liverpool", 
                    TeamB = "Arsenal",
                    TeamAOdds = 1.75m,
                    TeamBOdds = 2.20m,
                    EventDate = DateTime.UtcNow.AddDays(10),
                    Status = EventStatus.Upcoming,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
            
            modelBuilder.Entity<Event>().HasData(events);
        }
        
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is User || e.Entity is Event || e.Entity is Bet)
                .Where(e => e.State == EntityState.Modified);
            
            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                var updateProperty = entity.GetType().GetProperty("UpdatedAt");
                updateProperty?.SetValue(entity, DateTime.UtcNow);
            }
            
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
