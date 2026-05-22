using Microsoft.EntityFrameworkCore;

namespace Web_cham_diem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Roles> Roles { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Competitions> Competitions { get; set; }
        public DbSet<Registrations> Registrations { get; set; }
        public DbSet<Teams> Teams { get; set; }
        public DbSet<Submissions> Submissions { get; set; }
        public DbSet<Judges> Judges { get; set; }
        public DbSet<ScoringCriteria> ScoringCriteria { get; set; }
        public DbSet<Scores> Scores { get; set; }
        public DbSet<Notifications> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Roles
            modelBuilder.Entity<Roles>()
                .HasKey(r => r.RoleId);
            modelBuilder.Entity<Roles>()
                .Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(50);

            // Configure Users
            modelBuilder.Entity<Users>()
                .HasKey(u => u.UserId);
            modelBuilder.Entity<Users>()
                .Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);
            modelBuilder.Entity<Users>()
                .Property(u => u.PhoneNumber)
                .HasMaxLength(20);
            modelBuilder.Entity<Users>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure Competitions
            modelBuilder.Entity<Competitions>()
                .HasKey(c => c.CompetitionId);
            modelBuilder.Entity<Competitions>()
                .Property(c => c.CompetitionName)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<Competitions>()
                .Property(c => c.Status)
                .HasMaxLength(50);
            modelBuilder.Entity<Competitions>()
                .Property(c => c.MaxScore)
                .HasPrecision(18, 2);

            // Configure Registrations
            modelBuilder.Entity<Registrations>()
                .HasKey(r => r.RegistrationId);
            modelBuilder.Entity<Registrations>()
                .HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Registrations>()
                .HasOne(r => r.Competition)
                .WithMany(c => c.Registrations)
                .HasForeignKey(r => r.CompetitionId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Registrations>()
                .HasOne(r => r.Team)
                .WithMany(t => t.Registrations)
                .HasForeignKey(r => r.TeamId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Teams
            modelBuilder.Entity<Teams>()
                .HasKey(t => t.TeamId);
            modelBuilder.Entity<Teams>()
                .Property(t => t.TeamName)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<Teams>()
                .HasOne(t => t.Competition)
                .WithMany(c => c.Teams)
                .HasForeignKey(t => t.CompetitionId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Teams>()
                .HasOne(t => t.Leader)
                .WithMany()
                .HasForeignKey(t => t.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Submissions
            modelBuilder.Entity<Submissions>()
                .HasKey(s => s.SubmissionId);
            modelBuilder.Entity<Submissions>()
                .Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<Submissions>()
                .HasOne(s => s.Competition)
                .WithMany(c => c.Submissions)
                .HasForeignKey(s => s.CompetitionId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Submissions>()
                .HasOne(s => s.Registration)
                .WithMany()
                .HasForeignKey(s => s.RegistrationId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Submissions>()
                .HasOne(s => s.Team)
                .WithMany(t => t.Submissions)
                .HasForeignKey(s => s.TeamId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Judges
            modelBuilder.Entity<Judges>()
                .HasKey(j => j.JudgeId);
            modelBuilder.Entity<Judges>()
                .HasOne(j => j.User)
                .WithMany(u => u.JudgeAssignments)
                .HasForeignKey(j => j.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Judges>()
                .HasOne(j => j.Competition)
                .WithMany()
                .HasForeignKey(j => j.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ScoringCriteria
            modelBuilder.Entity<ScoringCriteria>()
                .HasKey(sc => sc.CriteriaId);
            modelBuilder.Entity<ScoringCriteria>()
                .Property(sc => sc.CriteriaName)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<ScoringCriteria>()
                .Property(sc => sc.MaxScore)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ScoringCriteria>()
                .Property(sc => sc.Weight)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ScoringCriteria>()
                .HasOne(sc => sc.Competition)
                .WithMany(c => c.ScoringCriteria)
                .HasForeignKey(sc => sc.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Scores
            modelBuilder.Entity<Scores>()
                .HasKey(s => s.ScoreId);
            modelBuilder.Entity<Scores>()
                .Property(s => s.Score)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Scores>()
                .HasOne(s => s.Submission)
                .WithMany(sub => sub.Scores)
                .HasForeignKey(s => s.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Scores>()
                .HasOne(s => s.Judge)
                .WithMany(j => j.Scores)
                .HasForeignKey(s => s.JudgeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Scores>()
                .HasOne(s => s.Criteria)
                .WithMany(sc => sc.Scores)
                .HasForeignKey(s => s.CriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Notifications
            modelBuilder.Entity<Notifications>()
                .HasKey(n => n.NotificationId);
            modelBuilder.Entity<Notifications>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed initial roles
            modelBuilder.Entity<Roles>().HasData(
                new Roles { RoleId = 1, RoleName = "Admin", Description = "Quản trị viên hệ thống", CreatedAt = DateTime.UtcNow },
                new Roles { RoleId = 2, RoleName = "Student", Description = "Sinh viên tham dự cuộc thi", CreatedAt = DateTime.UtcNow },
                new Roles { RoleId = 3, RoleName = "Organizer", Description = "Ban tổ chức cuộc thi", CreatedAt = DateTime.UtcNow },
                new Roles { RoleId = 4, RoleName = "Judge", Description = "Giám khảo", CreatedAt = DateTime.UtcNow },
                new Roles { RoleId = 5, RoleName = "Lecturer", Description = "Giảng viên hướng dẫn", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}
