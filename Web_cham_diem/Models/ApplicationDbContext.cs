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
        public DbSet<UserRoles> UserRoles { get; set; }
        public DbSet<CompetitionImages> CompetitionImages { get; set; }
        public DbSet<CompetitionDocuments> CompetitionDocuments { get; set; }
        public DbSet<RegistrationRounds> RegistrationRounds { get; set; }
        public DbSet<Sponsors> Sponsors { get; set; }
        public DbSet<CompetitionSponsors> CompetitionSponsors { get; set; }
        public DbSet<CompetitionRounds> CompetitionRounds { get; set; }
        public DbSet<JudgeAssignments> JudgeAssignments { get; set; }
        public DbSet<TeamMembers> TeamMembers { get; set; }
        public DbSet<TeamTasks> TeamTasks { get; set; }
        public DbSet<TaskCompletions> TaskCompletions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure Roles
            modelBuilder.Entity<Roles>()
                .HasKey(r => r.RoleId);
            modelBuilder.Entity<Roles>()
                .Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(50);

            // Configure UserRoles
            modelBuilder.Entity<UserRoles>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Configure Sponsors
            modelBuilder.Entity<Sponsors>()
                .HasKey(s => s.SponsorId);
            modelBuilder.Entity<Sponsors>()
                .Property(s => s.SponsorName)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<Sponsors>()
                .Property(s => s.Email)
                .HasMaxLength(100);
            modelBuilder.Entity<Sponsors>()
                .Property(s => s.PhoneNumber)
                .HasMaxLength(20);
            modelBuilder.Entity<Sponsors>()
                .HasIndex(s => s.Email)
                .IsUnique();

            // Configure CompetitionSponsors
            modelBuilder.Entity<CompetitionSponsors>()
                .HasKey(cs => cs.CompetitionSponsorId);
            modelBuilder.Entity<CompetitionSponsors>()
                .Property(cs => cs.SponsorshipLevel)
                .IsRequired()
                .HasMaxLength(50);
            modelBuilder.Entity<CompetitionSponsors>()
                .Property(cs => cs.Currency)
                .HasMaxLength(10);
            modelBuilder.Entity<CompetitionSponsors>()
                .Property(cs => cs.ContributionAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CompetitionSponsors>()
                .HasOne(cs => cs.Competition)
                .WithMany(c => c.CompetitionSponsors)
                .HasForeignKey(cs => cs.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompetitionSponsors>()
                .HasOne(cs => cs.Sponsor)
                .WithMany(s => s.CompetitionSponsors)
                .HasForeignKey(cs => cs.SponsorId)
                .OnDelete(DeleteBehavior.Cascade);

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
            modelBuilder.Entity<Registrations>()
                .HasOne(r => r.RegistrationRound)
                .WithMany(rr => rr.Registrations)
                .HasForeignKey(r => r.RoundId)
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
            modelBuilder.Entity<Submissions>()
                .HasOne(s => s.CompetitionRound)
                .WithMany(r => r.Submissions)
                .HasForeignKey(s => s.CompetitionRoundId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Judges
            modelBuilder.Entity<Judges>()
                .HasKey(j => j.JudgeId);
            modelBuilder.Entity<Judges>()
                .HasOne(j => j.User)
                .WithMany(u => u.Judges)
                .HasForeignKey(j => j.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Judges>()
                .HasOne(j => j.Competition)
                .WithMany(c => c.Judges)
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

            // Configure CompetitionImages
            modelBuilder.Entity<CompetitionImages>()
                .HasKey(ci => ci.ImageId);
            modelBuilder.Entity<CompetitionImages>()
                .Property(ci => ci.ImageUrl)
                .IsRequired();
            modelBuilder.Entity<CompetitionImages>()
                .HasOne(ci => ci.Competition)
                .WithMany(c => c.CompetitionImages)
                .HasForeignKey(ci => ci.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure CompetitionDocuments
            modelBuilder.Entity<CompetitionDocuments>()
                .HasKey(cd => cd.DocumentId);
            modelBuilder.Entity<CompetitionDocuments>()
                .Property(cd => cd.FileName)
                .IsRequired()
                .HasMaxLength(255);
            modelBuilder.Entity<CompetitionDocuments>()
                .Property(cd => cd.FileUrl)
                .IsRequired();
            modelBuilder.Entity<CompetitionDocuments>()
                .Property(cd => cd.FileType)
                .IsRequired()
                .HasMaxLength(50);
            modelBuilder.Entity<CompetitionDocuments>()
                .HasOne(cd => cd.Competition)
                .WithMany(c => c.CompetitionDocuments)
                .HasForeignKey(cd => cd.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure RegistrationRounds
            modelBuilder.Entity<RegistrationRounds>()
                .HasKey(rr => rr.RoundId);
            modelBuilder.Entity<RegistrationRounds>()
                .Property(rr => rr.RoundName)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<RegistrationRounds>()
                .HasOne(rr => rr.Competition)
                .WithMany(c => c.RegistrationRounds)
                .HasForeignKey(rr => rr.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure CompetitionRounds
            modelBuilder.Entity<CompetitionRounds>()
                .HasKey(cr => cr.RoundId);
            modelBuilder.Entity<CompetitionRounds>()
                .Property(cr => cr.RoundName)
                .IsRequired()
                .HasMaxLength(100);
            modelBuilder.Entity<CompetitionRounds>()
                .Property(cr => cr.Status)
                .HasMaxLength(50);
            modelBuilder.Entity<CompetitionRounds>()
                .HasOne(cr => cr.Competition)
                .WithMany(c => c.CompetitionRounds)
                .HasForeignKey(cr => cr.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure JudgeAssignments
            modelBuilder.Entity<JudgeAssignments>()
                .HasKey(ja => ja.AssignmentId);
            modelBuilder.Entity<JudgeAssignments>()
                .Property(ja => ja.Status)
                .HasMaxLength(50);
            // Unique: một giám khảo chỉ được giao một bài một lần
            modelBuilder.Entity<JudgeAssignments>()
                .HasIndex(ja => new { ja.JudgeId, ja.SubmissionId })
                .IsUnique();
            modelBuilder.Entity<JudgeAssignments>()
                .HasOne(ja => ja.Judge)
                .WithMany(j => j.JudgeAssignments)
                .HasForeignKey(ja => ja.JudgeId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<JudgeAssignments>()
                .HasOne(ja => ja.Submission)
                .WithMany(s => s.JudgeAssignments)
                .HasForeignKey(ja => ja.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<JudgeAssignments>()
                .HasOne(ja => ja.Competition)
                .WithMany(c => c.JudgeAssignments)
                .HasForeignKey(ja => ja.CompetitionId)
                .OnDelete(DeleteBehavior.NoAction);  // Tránh multiple cascade paths
            modelBuilder.Entity<JudgeAssignments>()
                .HasOne(ja => ja.Round)
                .WithMany(r => r.JudgeAssignments)
                .HasForeignKey(ja => ja.RoundId)
                .OnDelete(DeleteBehavior.SetNull);   // Giữ assignment nếu round bị xóa
            modelBuilder.Entity<JudgeAssignments>()
                .HasOne(ja => ja.AssignedByUser)
                .WithMany()
                .HasForeignKey(ja => ja.AssignedByUserId)
                .OnDelete(DeleteBehavior.NoAction);  // Không cascade khi user bị xóa

            // Configure Scores approval
            modelBuilder.Entity<Scores>()
                .Property(s => s.ApprovalStatus)
                .HasMaxLength(20);
            modelBuilder.Entity<Scores>()
                .HasOne(s => s.Approver)
                .WithMany()
                .HasForeignKey(s => s.ApprovedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Judges JudgeRole
            modelBuilder.Entity<Judges>()
                .Property(j => j.JudgeRole)
                .HasMaxLength(20);

            // Configure TeamMembers
            modelBuilder.Entity<TeamMembers>()
                .HasKey(tm => tm.TeamMemberId);
            modelBuilder.Entity<TeamMembers>()
                .Property(tm => tm.Role)
                .HasMaxLength(20);
            modelBuilder.Entity<TeamMembers>()
                .Property(tm => tm.Status)
                .HasMaxLength(20);
            // Một user chỉ thuộc một team một lần trong cùng team
            modelBuilder.Entity<TeamMembers>()
                .HasIndex(tm => new { tm.TeamId, tm.UserId })
                .IsUnique();
            modelBuilder.Entity<TeamMembers>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.TeamMembers)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TeamMembers>()
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TeamMembers>()
                .HasOne(tm => tm.Inviter)
                .WithMany()
                .HasForeignKey(tm => tm.InvitedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure TeamTasks
            modelBuilder.Entity<TeamTasks>()
                .HasKey(tt => tt.TaskId);
            modelBuilder.Entity<TeamTasks>()
                .Property(tt => tt.Title)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<TeamTasks>()
                .Property(tt => tt.Status)
                .HasMaxLength(20);
            modelBuilder.Entity<TeamTasks>()
                .HasOne(tt => tt.Team)
                .WithMany(t => t.TeamTasks)
                .HasForeignKey(tt => tt.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TeamTasks>()
                .HasOne(tt => tt.AssignedByUser)
                .WithMany()
                .HasForeignKey(tt => tt.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TaskCompletions
            modelBuilder.Entity<TaskCompletions>()
                .HasKey(tc => tc.CompletionId);
            // Một thành viên chỉ đánh dấu hoàn thành một task một lần
            modelBuilder.Entity<TaskCompletions>()
                .HasIndex(tc => new { tc.TaskId, tc.CompletedBy })
                .IsUnique();
            modelBuilder.Entity<TaskCompletions>()
                .HasOne(tc => tc.Task)
                .WithMany(tt => tt.TaskCompletions)
                .HasForeignKey(tc => tc.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TaskCompletions>()
                .HasOne(tc => tc.CompletedByUser)
                .WithMany(u => u.TaskCompletions)
                .HasForeignKey(tc => tc.CompletedBy)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TaskCompletions>()
                .HasOne(tc => tc.VerifiedByUser)
                .WithMany()
                .HasForeignKey(tc => tc.VerifiedBy)
                .OnDelete(DeleteBehavior.NoAction);

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
