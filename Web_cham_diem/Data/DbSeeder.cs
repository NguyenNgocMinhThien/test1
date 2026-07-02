using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;

namespace Web_cham_diem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await db.Users.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var hash = BCrypt.Net.BCrypt.HashPassword("Password123");

        // ── Users ──────────────────────────────────────────────────────────────
        var admin     = new Users { FullName = "Admin Hệ thống",          Email = "admin@test.com",      PhoneNumber = "0900000001", PasswordHash = hash, IsActive = true, CreatedAt = now };
        var organizer = new Users { FullName = "Nguyễn Thị Tổ Chức",      Email = "organizer@test.com",  PhoneNumber = "0900000002", PasswordHash = hash, IsActive = true, CreatedAt = now };
        var judge1    = new Users { FullName = "Trần Văn Giám",            Email = "judge1@test.com",     PhoneNumber = "0900000003", PasswordHash = hash, IsActive = true, CreatedAt = now };
        var judge2    = new Users { FullName = "Lê Thị Khảo",             Email = "judge2@test.com",     PhoneNumber = "0900000004", PasswordHash = hash, IsActive = true, CreatedAt = now };
        var judge3    = new Users { FullName = "Phạm Minh Đánh Giá",      Email = "judge3@test.com",     PhoneNumber = "0900000005", PasswordHash = hash, IsActive = true, CreatedAt = now };
        var lecturer  = new Users { FullName = "TS. Nguyễn Hướng Dẫn",    Email = "lecturer@test.com",   PhoneNumber = "0900000006", PasswordHash = hash, IsActive = true, CreatedAt = now };
        var student1  = new Users { FullName = "Vũ Minh Anh",             Email = "student1@test.com",   PhoneNumber = "0900000007", PasswordHash = hash, StudentId = "SV001", IsActive = true, CreatedAt = now };
        var student2  = new Users { FullName = "Đặng Thị Bình",           Email = "student2@test.com",   PhoneNumber = "0900000008", PasswordHash = hash, StudentId = "SV002", IsActive = true, CreatedAt = now };
        var student3  = new Users { FullName = "Hoàng Văn Cường",         Email = "student3@test.com",   PhoneNumber = "0900000009", PasswordHash = hash, StudentId = "SV003", IsActive = true, CreatedAt = now };
        var student4  = new Users { FullName = "Bùi Thị Dung",            Email = "student4@test.com",   PhoneNumber = "0900000010", PasswordHash = hash, StudentId = "SV004", IsActive = true, CreatedAt = now };
        var student5  = new Users { FullName = "Ngô Quang Hải",           Email = "student5@test.com",   PhoneNumber = "0900000011", PasswordHash = hash, StudentId = "SV005", IsActive = true, CreatedAt = now };
        var student6  = new Users { FullName = "Lý Thị Hương",            Email = "student6@test.com",   PhoneNumber = "0900000012", PasswordHash = hash, StudentId = "SV006", IsActive = true, CreatedAt = now };

        db.Users.AddRange(admin, organizer, judge1, judge2, judge3, lecturer,
                          student1, student2, student3, student4, student5, student6);
        await db.SaveChangesAsync();

        // ── UserRoles ──────────────────────────────────────────────────────────
        // RoleId: 1=Admin 2=Student 3=Organizer 4=Judge 5=Lecturer
        db.UserRoles.AddRange(
            new UserRoles { UserId = admin.UserId,     RoleId = 1 },
            new UserRoles { UserId = organizer.UserId, RoleId = 3 },
            new UserRoles { UserId = judge1.UserId,    RoleId = 4 },
            new UserRoles { UserId = judge2.UserId,    RoleId = 4 },
            new UserRoles { UserId = judge3.UserId,    RoleId = 4 },
            new UserRoles { UserId = lecturer.UserId,  RoleId = 5 },
            new UserRoles { UserId = student1.UserId,  RoleId = 2 },
            new UserRoles { UserId = student2.UserId,  RoleId = 2 },
            new UserRoles { UserId = student3.UserId,  RoleId = 2 },
            new UserRoles { UserId = student4.UserId,  RoleId = 2 },
            new UserRoles { UserId = student5.UserId,  RoleId = 2 },
            new UserRoles { UserId = student6.UserId,  RoleId = 2 }
        );
        await db.SaveChangesAsync();

        // ── Sponsors ───────────────────────────────────────────────────────────
        var sponsor1 = new Sponsors
        {
            SponsorName = "Công ty TNHH TechVision",
            Email       = "contact@techvision.vn",
            PhoneNumber = "0281234567",
            Website     = "https://techvision.vn",
            Description = "Công ty công nghệ hàng đầu tại Việt Nam",
            Status      = "Active",
            CreatedAt   = now
        };
        var sponsor2 = new Sponsors
        {
            SponsorName = "Tập đoàn InnoGroup",
            Email       = "info@innogroup.vn",
            PhoneNumber = "0289876543",
            Website     = "https://innogroup.vn",
            Description = "Tập đoàn đầu tư và ươm mầm khởi nghiệp",
            Status      = "Active",
            CreatedAt   = now
        };
        db.Sponsors.AddRange(sponsor1, sponsor2);
        await db.SaveChangesAsync();

        // ── Competitions ───────────────────────────────────────────────────────
        var comp1 = new Competitions
        {
            CompetitionName    = "Cuộc thi Khởi nghiệp Sáng tạo 2026",
            Description        = "Cuộc thi tìm kiếm các dự án khởi nghiệp sáng tạo trong lĩnh vực công nghệ và kinh doanh dành cho sinh viên toàn quốc.",
            Category           = "Khởi nghiệp",
            StartDate          = new DateTime(2026, 3,  1,  0,  0,  0, DateTimeKind.Utc),
            EndDate            = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc),
            SubmissionDeadline = new DateTime(2026, 9, 15, 23, 59, 59, DateTimeKind.Utc),
            MinParticipants    = 2,
            MaxParticipants    = 200,
            MaxTeamSize        = 5,
            MaxScore           = 100,
            Status             = "Active",
            IsTeamBased        = true,
            Rules              = "1. Mỗi đội 2–5 thành viên là sinh viên đang theo học.\n2. Dự án phải mang tính đổi mới sáng tạo, chưa được giải thưởng tại cuộc thi khác.\n3. Nghiêm cấm sao chép ý tưởng của đội khác.",
            Prize              = "🥇 Giải Nhất: 50.000.000 VND\n🥈 Giải Nhì: 30.000.000 VND\n🥉 Giải Ba: 20.000.000 VND\n🏅 Giải Khuyến khích (×2): 5.000.000 VND/giải",
            CreatedAt          = now,
            CreatedByUserId    = organizer.UserId
        };
        var comp2 = new Competitions
        {
            CompetitionName    = "Hackathon Công nghệ 2026",
            Description        = "Sự kiện lập trình 48 giờ liên tục, tìm kiếm giải pháp công nghệ cho các vấn đề thực tiễn.",
            Category           = "Công nghệ",
            StartDate          = new DateTime(2026, 10,  1,  0,  0,  0, DateTimeKind.Utc),
            EndDate            = new DateTime(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc),
            SubmissionDeadline = new DateTime(2026, 11, 20, 23, 59, 59, DateTimeKind.Utc),
            MinParticipants    = 2,
            MaxParticipants    = 100,
            MaxTeamSize        = 4,
            MaxScore           = 100,
            Status             = "Draft",
            IsTeamBased        = true,
            Rules              = "1. Mỗi đội 2–4 thành viên.\n2. Phát triển sản phẩm trong 48 giờ.\n3. Ưu tiên sử dụng công nghệ mã nguồn mở.",
            Prize              = "🥇 Giải Nhất: 30.000.000 VND\n🥈 Giải Nhì: 20.000.000 VND",
            CreatedAt          = now,
            CreatedByUserId    = organizer.UserId
        };
        db.Competitions.AddRange(comp1, comp2);
        await db.SaveChangesAsync();

        // ── CompetitionSponsors ────────────────────────────────────────────────
        db.CompetitionSponsors.AddRange(
            new CompetitionSponsors
            {
                CompetitionId      = comp1.CompetitionId,
                SponsorId          = sponsor1.SponsorId,
                SponsorshipLevel   = "Gold",
                ContributionAmount = 50_000_000,
                Currency           = "VND",
                IsDisplayed        = true,
                DisplayOrder       = 1,
                SponsoredAt        = now
            },
            new CompetitionSponsors
            {
                CompetitionId      = comp1.CompetitionId,
                SponsorId          = sponsor2.SponsorId,
                SponsorshipLevel   = "Silver",
                ContributionAmount = 20_000_000,
                Currency           = "VND",
                IsDisplayed        = true,
                DisplayOrder       = 2,
                SponsoredAt        = now
            }
        );
        await db.SaveChangesAsync();

        // ── RegistrationRounds (đợt đăng ký tham gia) ─────────────────────────
        var regRound1 = new RegistrationRounds
        {
            CompetitionId = comp1.CompetitionId,
            RoundName     = "Đợt 1 – Đăng ký chính thức",
            StartDate     = new DateTime(2026, 2,  1,  0,  0,  0, DateTimeKind.Utc),
            EndDate       = new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc),
            CreatedAt     = now
        };
        db.RegistrationRounds.Add(regRound1);
        await db.SaveChangesAsync();

        // ── CompetitionRounds (vòng thi) ───────────────────────────────────────
        var roundSoKhao = new CompetitionRounds
        {
            CompetitionId      = comp1.CompetitionId,
            RoundName          = "Sơ khảo",
            RoundOrder         = 1,
            Description        = "Nộp báo cáo ý tưởng và slide thuyết trình. Top 10 đội xuất sắc vào bán kết.",
            StartDate          = new DateTime(2026, 3,  1,  0,  0,  0, DateTimeKind.Utc),
            EndDate            = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc),
            SubmissionDeadline = new DateTime(2026, 4, 20, 23, 59, 59, DateTimeKind.Utc),
            MaxAdvancing       = 10,
            Status             = "Completed",
            RequiresFile       = true,
            RequiresVideo      = false,
            RequiresOtherLink  = false,
            IsOnline           = false,
            Location           = "Hội trường A, Đại học Bách Khoa TP.HCM",
            MeetingTime        = new DateTime(2026, 4, 25,  8,  0,  0, DateTimeKind.Utc),
            CreatedAt          = now
        };
        var roundBanKet = new CompetitionRounds
        {
            CompetitionId      = comp1.CompetitionId,
            RoundName          = "Bán kết",
            RoundOrder         = 2,
            Description        = "Thuyết trình và demo sản phẩm trực tuyến trước ban giám khảo. Top 5 vào chung kết.",
            StartDate          = new DateTime(2026, 5,  1,  0,  0,  0, DateTimeKind.Utc),
            EndDate            = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc),
            SubmissionDeadline = new DateTime(2026, 7, 15, 23, 59, 59, DateTimeKind.Utc),
            MaxAdvancing       = 5,
            Status             = "Active",
            RequiresFile       = true,
            RequiresVideo      = true,
            RequiresOtherLink  = true,
            IsOnline           = true,
            MeetingLink        = "https://meet.google.com/seed-banket-2026",
            MeetingTime        = new DateTime(2026, 7, 20,  8,  0,  0, DateTimeKind.Utc),
            CreatedAt          = now
        };
        var roundChungKet = new CompetitionRounds
        {
            CompetitionId      = comp1.CompetitionId,
            RoundName          = "Chung kết",
            RoundOrder         = 3,
            Description        = "Thuyết trình, demo sản phẩm và trả lời câu hỏi từ ban giám khảo và nhà đầu tư.",
            StartDate          = new DateTime(2026, 8,  1,  0,  0,  0, DateTimeKind.Utc),
            EndDate            = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc),
            SubmissionDeadline = new DateTime(2026, 9, 15, 23, 59, 59, DateTimeKind.Utc),
            MaxAdvancing       = null,
            Status             = "Upcoming",
            RequiresFile       = true,
            RequiresVideo      = true,
            RequiresOtherLink  = true,
            IsOnline           = false,
            Location           = "Nhà văn hóa Sinh viên TP.HCM, 1 Phù Đổng Thiên Vương, Q.3",
            MeetingTime        = new DateTime(2026, 9, 25,  8,  0,  0, DateTimeKind.Utc),
            CreatedAt          = now
        };
        db.CompetitionRounds.AddRange(roundSoKhao, roundBanKet, roundChungKet);
        await db.SaveChangesAsync();

        // ── ScoringCriteria (per round) ────────────────────────────────────────
        db.ScoringCriteria.AddRange(
            // Sơ khảo
            new ScoringCriteria { RoundId = roundSoKhao.RoundId, CriteriaName = "Tính sáng tạo & đổi mới",      Description = "Mức độ mới lạ và đột phá của ý tưởng",                      MaxScore = 30, Weight = 1.0m, Order = 1, CreatedAt = now },
            new ScoringCriteria { RoundId = roundSoKhao.RoundId, CriteriaName = "Tính khả thi",                  Description = "Khả năng triển khai ý tưởng trong thực tế",                   MaxScore = 40, Weight = 1.0m, Order = 2, CreatedAt = now },
            new ScoringCriteria { RoundId = roundSoKhao.RoundId, CriteriaName = "Chất lượng thuyết trình",       Description = "Slide rõ ràng, thuyết trình lưu loát, thu hút",               MaxScore = 30, Weight = 1.0m, Order = 3, CreatedAt = now },

            // Bán kết
            new ScoringCriteria { RoundId = roundBanKet.RoundId, CriteriaName = "Tính sáng tạo & tác động",     Description = "Mức độ mới lạ và giá trị đem lại cho xã hội",                 MaxScore = 25, Weight = 1.0m, Order = 1, CreatedAt = now },
            new ScoringCriteria { RoundId = roundBanKet.RoundId, CriteriaName = "Demo sản phẩm",                 Description = "Chất lượng prototype, tính hoàn thiện và ổn định",            MaxScore = 35, Weight = 1.0m, Order = 2, CreatedAt = now },
            new ScoringCriteria { RoundId = roundBanKet.RoundId, CriteriaName = "Mô hình kinh doanh",            Description = "Kế hoạch doanh thu, chi phí và thị trường mục tiêu",          MaxScore = 25, Weight = 1.0m, Order = 3, CreatedAt = now },
            new ScoringCriteria { RoundId = roundBanKet.RoundId, CriteriaName = "Trả lời câu hỏi ban giám khảo", Description = "Tự tin, am hiểu sâu về sản phẩm khi trả lời phản biện",      MaxScore = 15, Weight = 1.0m, Order = 4, CreatedAt = now },

            // Chung kết
            new ScoringCriteria { RoundId = roundChungKet.RoundId, CriteriaName = "Tầm nhìn & chiến lược",       Description = "Lộ trình phát triển dài hạn và khả năng mở rộng",            MaxScore = 20, Weight = 1.0m, Order = 1, CreatedAt = now },
            new ScoringCriteria { RoundId = roundChungKet.RoundId, CriteriaName = "Sản phẩm hoàn thiện",         Description = "Chất lượng kỹ thuật và trải nghiệm người dùng",              MaxScore = 35, Weight = 1.0m, Order = 2, CreatedAt = now },
            new ScoringCriteria { RoundId = roundChungKet.RoundId, CriteriaName = "Tác động xã hội",             Description = "Giá trị thực tế mang lại cho cộng đồng",                     MaxScore = 20, Weight = 1.0m, Order = 3, CreatedAt = now },
            new ScoringCriteria { RoundId = roundChungKet.RoundId, CriteriaName = "Pitch & Mô hình kinh doanh",  Description = "Thuyết phục nhà đầu tư, kế hoạch go-to-market cụ thể",       MaxScore = 25, Weight = 1.0m, Order = 4, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Teams ──────────────────────────────────────────────────────────────
        var teamAlpha = new Teams
        {
            TeamName      = "Team Alpha – EcoSmart",
            CompetitionId = comp1.CompetitionId,
            LeaderId      = student1.UserId,
            Description   = "Giải pháp quản lý rác thải thông minh sử dụng IoT và AI",
            Status        = "Active",
            CreatedAt     = now
        };
        var teamBeta = new Teams
        {
            TeamName      = "Team Beta – MediLink",
            CompetitionId = comp1.CompetitionId,
            LeaderId      = student4.UserId,
            Description   = "Nền tảng kết nối bệnh nhân với bác sĩ từ xa",
            Status        = "Active",
            CreatedAt     = now
        };
        db.Teams.AddRange(teamAlpha, teamBeta);
        await db.SaveChangesAsync();

        // ── TeamMembers ────────────────────────────────────────────────────────
        db.TeamMembers.AddRange(
            new TeamMembers { TeamId = teamAlpha.TeamId, UserId = student1.UserId, Role = "Leader", Status = "Active", JoinedAt = now },
            new TeamMembers { TeamId = teamAlpha.TeamId, UserId = student2.UserId, Role = "Member", Status = "Active", JoinedAt = now, InvitedBy = student1.UserId },
            new TeamMembers { TeamId = teamAlpha.TeamId, UserId = student3.UserId, Role = "Member", Status = "Active", JoinedAt = now, InvitedBy = student1.UserId },
            new TeamMembers { TeamId = teamBeta.TeamId,  UserId = student4.UserId, Role = "Leader", Status = "Active", JoinedAt = now },
            new TeamMembers { TeamId = teamBeta.TeamId,  UserId = student5.UserId, Role = "Member", Status = "Active", JoinedAt = now, InvitedBy = student4.UserId },
            new TeamMembers { TeamId = teamBeta.TeamId,  UserId = student6.UserId, Role = "Member", Status = "Active", JoinedAt = now, InvitedBy = student4.UserId }
        );
        await db.SaveChangesAsync();

        // ── Registrations ──────────────────────────────────────────────────────
        var reg1 = new Registrations
        {
            UserId           = student1.UserId,
            CompetitionId    = comp1.CompetitionId,
            TeamId           = teamAlpha.TeamId,
            RoundId          = regRound1.RoundId,
            AdvisorId        = lecturer.UserId,
            RegistrationType = "Team",
            Status           = "Approved",
            Notes            = "Hồ sơ đầy đủ, đã xét duyệt",
            RegistrationDate = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            ApprovalDate     = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc)
        };
        var reg2 = new Registrations
        {
            UserId           = student4.UserId,
            CompetitionId    = comp1.CompetitionId,
            TeamId           = teamBeta.TeamId,
            RoundId          = regRound1.RoundId,
            AdvisorId        = lecturer.UserId,
            RegistrationType = "Team",
            Status           = "Approved",
            Notes            = "Hồ sơ đầy đủ, đã xét duyệt",
            RegistrationDate = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            ApprovalDate     = new DateTime(2026, 2, 16, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Registrations.AddRange(reg1, reg2);
        await db.SaveChangesAsync();

        // ── Submissions ────────────────────────────────────────────────────────
        var subAlpha_sk = new Submissions
        {
            CompetitionId     = comp1.CompetitionId,
            RegistrationId    = reg1.RegistrationId,
            TeamId            = teamAlpha.TeamId,
            CompetitionRoundId = roundSoKhao.RoundId,
            Title             = "EcoSmart – Hệ thống phân loại rác thải thông minh",
            Description       = "Hệ thống IoT kết hợp AI để tự động phân loại rác tại nguồn, tích hợp ứng dụng điện thoại cho người dùng và dashboard quản lý cho chính quyền địa phương.",
            FileUrl           = "/uploads/submissions/alpha_sokhao_report.pdf",
            Status            = "Graded",
            SubmissionDate    = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc)
        };
        var subAlpha_bk = new Submissions
        {
            CompetitionId      = comp1.CompetitionId,
            RegistrationId     = reg1.RegistrationId,
            TeamId             = teamAlpha.TeamId,
            CompetitionRoundId = roundBanKet.RoundId,
            Title              = "EcoSmart v2 – Demo sản phẩm Bán kết",
            Description        = "Demo prototype thiết bị phân loại rác thông minh kết hợp với dashboard quản lý thành phố theo thời gian thực.",
            FileUrl            = "/uploads/submissions/alpha_banket_report.pdf",
            VideoUrl           = "https://youtu.be/ecosmart-demo-2026",
            ProjectLink        = "https://github.com/ecosmart-team/ecosmart",
            Status             = "Submitted",
            SubmissionDate     = new DateTime(2026, 7, 12, 14, 30, 0, DateTimeKind.Utc)
        };
        var subBeta_sk = new Submissions
        {
            CompetitionId      = comp1.CompetitionId,
            RegistrationId     = reg2.RegistrationId,
            TeamId             = teamBeta.TeamId,
            CompetitionRoundId = roundSoKhao.RoundId,
            Title              = "MediLink – Nền tảng telehealth cho vùng sâu vùng xa",
            Description        = "Kết nối bệnh nhân tại vùng sâu với bác sĩ thành phố qua ứng dụng di động, hỗ trợ AI chẩn đoán sơ bộ và lưu trữ hồ sơ y tế điện tử.",
            FileUrl            = "/uploads/submissions/beta_sokhao_report.pdf",
            Status             = "Graded",
            SubmissionDate     = new DateTime(2026, 4, 15,  9,  0, 0, DateTimeKind.Utc)
        };
        db.Submissions.AddRange(subAlpha_sk, subAlpha_bk, subBeta_sk);
        await db.SaveChangesAsync();

        // ── Judges (per round) ─────────────────────────────────────────────────
        // Sơ khảo:   judge1=HeadJudge | judge2=ViceHead | judge3=Member
        // Bán kết:   judge2=HeadJudge | judge1=ViceHead | judge3=Member
        // Chung kết: judge3=HeadJudge | judge1=Member
        var j1_sk = new Judges { UserId = judge1.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundSoKhao.RoundId,   JudgeRole = "HeadJudge", Expertise = "Công nghệ IoT & AI ứng dụng",          Priority = 1, Status = "Active", AssignedDate = now };
        var j2_sk = new Judges { UserId = judge2.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundSoKhao.RoundId,   JudgeRole = "ViceHead",  Expertise = "Quản trị kinh doanh khởi nghiệp",      Priority = 2, Status = "Active", AssignedDate = now };
        var j3_sk = new Judges { UserId = judge3.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundSoKhao.RoundId,   JudgeRole = "Member",    Expertise = "Đầu tư mạo hiểm & định giá startup",   Priority = 3, Status = "Active", AssignedDate = now };
        var j2_bk = new Judges { UserId = judge2.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundBanKet.RoundId,   JudgeRole = "HeadJudge", Expertise = "Quản trị kinh doanh khởi nghiệp",      Priority = 1, Status = "Active", AssignedDate = now };
        var j1_bk = new Judges { UserId = judge1.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundBanKet.RoundId,   JudgeRole = "ViceHead",  Expertise = "Công nghệ IoT & AI ứng dụng",          Priority = 2, Status = "Active", AssignedDate = now };
        var j3_bk = new Judges { UserId = judge3.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundBanKet.RoundId,   JudgeRole = "Member",    Expertise = "Đầu tư mạo hiểm & định giá startup",   Priority = 3, Status = "Active", AssignedDate = now };
        var j3_ck = new Judges { UserId = judge3.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundChungKet.RoundId, JudgeRole = "HeadJudge", Expertise = "Đầu tư mạo hiểm & định giá startup",   Priority = 1, Status = "Active", AssignedDate = now };
        var j1_ck = new Judges { UserId = judge1.UserId, CompetitionId = comp1.CompetitionId, RoundId = roundChungKet.RoundId, JudgeRole = "Member",    Expertise = "Công nghệ IoT & AI ứng dụng",          Priority = 2, Status = "Active", AssignedDate = now };

        db.Judges.AddRange(j1_sk, j2_sk, j3_sk, j2_bk, j1_bk, j3_bk, j3_ck, j1_ck);
        await db.SaveChangesAsync();

        // ── JudgeAssignments ───────────────────────────────────────────────────
        db.JudgeAssignments.AddRange(
            // Sơ khảo – Alpha: judge1 & judge2 chấm (completed)
            new JudgeAssignments
            {
                JudgeId = j1_sk.JudgeId, SubmissionId = subAlpha_sk.SubmissionId,
                CompetitionId = comp1.CompetitionId, RoundId = roundSoKhao.RoundId,
                AssignedByUserId = organizer.UserId,
                AssignedDate     = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                GradingDeadline  = new DateTime(2026, 4, 25, 23, 59, 59, DateTimeKind.Utc),
                Status = "Completed", CompletedAt = new DateTime(2026, 4, 22, 10, 0, 0, DateTimeKind.Utc)
            },
            new JudgeAssignments
            {
                JudgeId = j2_sk.JudgeId, SubmissionId = subAlpha_sk.SubmissionId,
                CompetitionId = comp1.CompetitionId, RoundId = roundSoKhao.RoundId,
                AssignedByUserId = organizer.UserId,
                AssignedDate     = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                GradingDeadline  = new DateTime(2026, 4, 25, 23, 59, 59, DateTimeKind.Utc),
                Status = "Completed", CompletedAt = new DateTime(2026, 4, 23,  9, 0, 0, DateTimeKind.Utc)
            },
            // Sơ khảo – Beta: judge1 & judge3 chấm (completed)
            new JudgeAssignments
            {
                JudgeId = j1_sk.JudgeId, SubmissionId = subBeta_sk.SubmissionId,
                CompetitionId = comp1.CompetitionId, RoundId = roundSoKhao.RoundId,
                AssignedByUserId = organizer.UserId,
                AssignedDate     = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                GradingDeadline  = new DateTime(2026, 4, 25, 23, 59, 59, DateTimeKind.Utc),
                Status = "Completed", CompletedAt = new DateTime(2026, 4, 21, 14, 0, 0, DateTimeKind.Utc)
            },
            new JudgeAssignments
            {
                JudgeId = j3_sk.JudgeId, SubmissionId = subBeta_sk.SubmissionId,
                CompetitionId = comp1.CompetitionId, RoundId = roundSoKhao.RoundId,
                AssignedByUserId = organizer.UserId,
                AssignedDate     = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                GradingDeadline  = new DateTime(2026, 4, 25, 23, 59, 59, DateTimeKind.Utc),
                Status = "Completed", CompletedAt = new DateTime(2026, 4, 22, 16, 0, 0, DateTimeKind.Utc)
            },
            // Bán kết – Alpha: judge2 (InProgress) & judge1 (Pending)
            new JudgeAssignments
            {
                JudgeId = j2_bk.JudgeId, SubmissionId = subAlpha_bk.SubmissionId,
                CompetitionId = comp1.CompetitionId, RoundId = roundBanKet.RoundId,
                AssignedByUserId = organizer.UserId,
                AssignedDate    = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
                GradingDeadline = new DateTime(2026, 7, 22, 23, 59, 59, DateTimeKind.Utc),
                Status = "InProgress"
            },
            new JudgeAssignments
            {
                JudgeId = j1_bk.JudgeId, SubmissionId = subAlpha_bk.SubmissionId,
                CompetitionId = comp1.CompetitionId, RoundId = roundBanKet.RoundId,
                AssignedByUserId = organizer.UserId,
                AssignedDate    = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
                GradingDeadline = new DateTime(2026, 7, 22, 23, 59, 59, DateTimeKind.Utc),
                Status = "Pending"
            }
        );
        await db.SaveChangesAsync();

        // ── Scores ─────────────────────────────────────────────────────────────
        var criteria_sk = await db.ScoringCriteria
            .Where(sc => sc.RoundId == roundSoKhao.RoundId)
            .OrderBy(sc => sc.Order)
            .ToListAsync();

        if (criteria_sk.Count >= 3)
        {
            var c1 = criteria_sk[0].CriteriaId;
            var c2 = criteria_sk[1].CriteriaId;
            var c3 = criteria_sk[2].CriteriaId;

            db.Scores.AddRange(
                // judge1 chấm Alpha sơ khảo
                new Scores { SubmissionId = subAlpha_sk.SubmissionId, JudgeId = j1_sk.JudgeId, CriteriaId = c1, Score = 25, Comment = "Ý tưởng rất mới lạ, có tiềm năng thương mại cao",          ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 22, 10, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subAlpha_sk.SubmissionId, JudgeId = j1_sk.JudgeId, CriteriaId = c2, Score = 35, Comment = "Kế hoạch triển khai rõ ràng, đội đã nghiên cứu thị trường kỹ", ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 22, 10, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subAlpha_sk.SubmissionId, JudgeId = j1_sk.JudgeId, CriteriaId = c3, Score = 27, Comment = "Thuyết trình lưu loát, slide chuyên nghiệp",                ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 22, 10, 0, 0, DateTimeKind.Utc) },

                // judge2 chấm Alpha sơ khảo
                new Scores { SubmissionId = subAlpha_sk.SubmissionId, JudgeId = j2_sk.JudgeId, CriteriaId = c1, Score = 22, Comment = "Sáng tạo nhưng cần nghiên cứu thêm đối thủ cạnh tranh",       ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 23,  9, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subAlpha_sk.SubmissionId, JudgeId = j2_sk.JudgeId, CriteriaId = c2, Score = 30, Comment = "Cần cụ thể hóa nguồn vốn và chi phí vận hành hằng tháng",    ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 23,  9, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subAlpha_sk.SubmissionId, JudgeId = j2_sk.JudgeId, CriteriaId = c3, Score = 25, Comment = "Cần tự tin hơn khi đối mặt với câu hỏi phản biện",           ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 23,  9, 0, 0, DateTimeKind.Utc) },

                // judge1 chấm Beta sơ khảo
                new Scores { SubmissionId = subBeta_sk.SubmissionId, JudgeId = j1_sk.JudgeId, CriteriaId = c1, Score = 28, Comment = "Vấn đề xã hội lớn, giải pháp thực sự cần thiết",              ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 21, 14, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subBeta_sk.SubmissionId, JudgeId = j1_sk.JudgeId, CriteriaId = c2, Score = 36, Comment = "Đội đã có pilot tại địa phương, rất thuyết phục",              ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 21, 14, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subBeta_sk.SubmissionId, JudgeId = j1_sk.JudgeId, CriteriaId = c3, Score = 26, Comment = "Trình bày tốt nhưng phần Q&A còn lúng túng",                   ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 21, 14, 0, 0, DateTimeKind.Utc) },

                // judge3 chấm Beta sơ khảo
                new Scores { SubmissionId = subBeta_sk.SubmissionId, JudgeId = j3_sk.JudgeId, CriteriaId = c1, Score = 26, Comment = "Ý tưởng tốt nhưng thị trường cạnh tranh cao",                  ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 22, 16, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subBeta_sk.SubmissionId, JudgeId = j3_sk.JudgeId, CriteriaId = c2, Score = 32, Comment = "Mô hình kinh doanh cần bổ sung kế hoạch tài chính chi tiết hơn", ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 22, 16, 0, 0, DateTimeKind.Utc) },
                new Scores { SubmissionId = subBeta_sk.SubmissionId, JudgeId = j3_sk.JudgeId, CriteriaId = c3, Score = 24, Comment = "Slide đơn giản, cần cải thiện phần thiết kế",                   ApprovalStatus = "Approved", ApprovedBy = organizer.UserId, ApprovedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), ScoredDate = new DateTime(2026, 4, 22, 16, 0, 0, DateTimeKind.Utc) }
            );
            await db.SaveChangesAsync();
        }

        // ══════════════════════════════════════════════════════════════════════
        // DỮ LIỆU BỔ SUNG – test chức năng PHÂN CÔNG HÀNG LOẠT (Bulk Assign)
        // Tạo thêm 4 đội/thí sinh với bài nộp ở vòng Bán kết (chưa phân công)
        // ══════════════════════════════════════════════════════════════════════

        // ── Thêm 6 sinh viên mới ──────────────────────────────────────────────
        var student7  = new Users { FullName = "Phan Thị Lan",       Email = "student7@test.com",  PhoneNumber = "0900000013", PasswordHash = hash, StudentId = "SV007", IsActive = true, CreatedAt = now };
        var student8  = new Users { FullName = "Đinh Quốc Hùng",     Email = "student8@test.com",  PhoneNumber = "0900000014", PasswordHash = hash, StudentId = "SV008", IsActive = true, CreatedAt = now };
        var student9  = new Users { FullName = "Tô Minh Khoa",        Email = "student9@test.com",  PhoneNumber = "0900000015", PasswordHash = hash, StudentId = "SV009", IsActive = true, CreatedAt = now };
        var student10 = new Users { FullName = "Mai Thị Ngọc",        Email = "student10@test.com", PhoneNumber = "0900000016", PasswordHash = hash, StudentId = "SV010", IsActive = true, CreatedAt = now };
        var student11 = new Users { FullName = "Trịnh Văn Phúc",      Email = "student11@test.com", PhoneNumber = "0900000017", PasswordHash = hash, StudentId = "SV011", IsActive = true, CreatedAt = now };
        var student12 = new Users { FullName = "Nguyễn Thị Quỳnh",    Email = "student12@test.com", PhoneNumber = "0900000018", PasswordHash = hash, StudentId = "SV012", IsActive = true, CreatedAt = now };
        db.Users.AddRange(student7, student8, student9, student10, student11, student12);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRoles { UserId = student7.UserId,  RoleId = 2 },
            new UserRoles { UserId = student8.UserId,  RoleId = 2 },
            new UserRoles { UserId = student9.UserId,  RoleId = 2 },
            new UserRoles { UserId = student10.UserId, RoleId = 2 },
            new UserRoles { UserId = student11.UserId, RoleId = 2 },
            new UserRoles { UserId = student12.UserId, RoleId = 2 }
        );
        await db.SaveChangesAsync();

        // ── Thêm 2 đội mới ────────────────────────────────────────────────────
        var teamGamma = new Teams
        {
            TeamName      = "Team Gamma – SmartLearn",
            CompetitionId = comp1.CompetitionId,
            LeaderId      = student7.UserId,
            Description   = "Nền tảng học tập thích ứng dùng AI cá nhân hóa lộ trình học cho từng sinh viên",
            Status        = "Active",
            CreatedAt     = now
        };
        var teamDelta = new Teams
        {
            TeamName      = "Team Delta – GreenCity",
            CompetitionId = comp1.CompetitionId,
            LeaderId      = student9.UserId,
            Description   = "Giải pháp đô thị xanh thông minh: cảm biến môi trường + dự báo AI",
            Status        = "Active",
            CreatedAt     = now
        };
        db.Teams.AddRange(teamGamma, teamDelta);
        await db.SaveChangesAsync();

        db.TeamMembers.AddRange(
            new TeamMembers { TeamId = teamGamma.TeamId, UserId = student7.UserId,  Role = "Leader", Status = "Active", JoinedAt = now },
            new TeamMembers { TeamId = teamGamma.TeamId, UserId = student8.UserId,  Role = "Member", Status = "Active", JoinedAt = now, InvitedBy = student7.UserId },
            new TeamMembers { TeamId = teamDelta.TeamId, UserId = student9.UserId,  Role = "Leader", Status = "Active", JoinedAt = now },
            new TeamMembers { TeamId = teamDelta.TeamId, UserId = student10.UserId, Role = "Member", Status = "Active", JoinedAt = now, InvitedBy = student9.UserId }
        );
        await db.SaveChangesAsync();

        // ── Đăng ký tham dự (Approved) ────────────────────────────────────────
        var reg3 = new Registrations
        {
            UserId = student7.UserId, CompetitionId = comp1.CompetitionId, TeamId = teamGamma.TeamId,
            RoundId = regRound1.RoundId, AdvisorId = lecturer.UserId, RegistrationType = "Team",
            Status = "Approved", RegistrationDate = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc),
            ApprovalDate = new DateTime(2026, 2, 18, 0, 0, 0, DateTimeKind.Utc)
        };
        var reg4 = new Registrations
        {
            UserId = student9.UserId, CompetitionId = comp1.CompetitionId, TeamId = teamDelta.TeamId,
            RoundId = regRound1.RoundId, AdvisorId = lecturer.UserId, RegistrationType = "Team",
            Status = "Approved", RegistrationDate = new DateTime(2026, 2, 16, 0, 0, 0, DateTimeKind.Utc),
            ApprovalDate = new DateTime(2026, 2, 19, 0, 0, 0, DateTimeKind.Utc)
        };
        var reg5 = new Registrations
        {
            UserId = student11.UserId, CompetitionId = comp1.CompetitionId, TeamId = null,
            RoundId = regRound1.RoundId, AdvisorId = lecturer.UserId, RegistrationType = "Individual",
            Status = "Approved", RegistrationDate = new DateTime(2026, 2, 17, 0, 0, 0, DateTimeKind.Utc),
            ApprovalDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc)
        };
        var reg6 = new Registrations
        {
            UserId = student12.UserId, CompetitionId = comp1.CompetitionId, TeamId = null,
            RoundId = regRound1.RoundId, AdvisorId = lecturer.UserId, RegistrationType = "Individual",
            Status = "Approved", RegistrationDate = new DateTime(2026, 2, 18, 0, 0, 0, DateTimeKind.Utc),
            ApprovalDate = new DateTime(2026, 2, 21, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Registrations.AddRange(reg3, reg4, reg5, reg6);
        await db.SaveChangesAsync();

        // ── Bài nộp vòng Bán kết (5 bài CHƯA phân công – để test Bulk Assign) ─
        var subBeta_bk = new Submissions
        {
            CompetitionId = comp1.CompetitionId, RegistrationId = reg2.RegistrationId,
            TeamId = teamBeta.TeamId, CompetitionRoundId = roundBanKet.RoundId,
            Title = "MediLink v2 – Demo AI chẩn đoán và hồ sơ y tế điện tử",
            Description = "Demo ứng dụng telehealth tích hợp mô hình AI chẩn đoán sơ bộ từ ảnh chụp và triệu chứng, kết hợp lưu trữ hồ sơ y tế trên blockchain.",
            FileUrl = "/uploads/submissions/beta_banket_report.pdf",
            VideoUrl = "https://youtu.be/medilink-demo-2026",
            ProjectLink = "https://github.com/medilink-team/medilink",
            Status = "Submitted",
            SubmissionDate = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc)
        };
        var subGamma_bk = new Submissions
        {
            CompetitionId = comp1.CompetitionId, RegistrationId = reg3.RegistrationId,
            TeamId = teamGamma.TeamId, CompetitionRoundId = roundBanKet.RoundId,
            Title = "SmartLearn – Hệ thống học tập thích ứng dựa trên AI",
            Description = "Nền tảng phân tích hành vi học tập theo thời gian thực, tự động đề xuất bài tập và nội dung phù hợp với năng lực từng sinh viên.",
            FileUrl = "/uploads/submissions/gamma_banket_report.pdf",
            VideoUrl = "https://youtu.be/smartlearn-demo-2026",
            ProjectLink = "https://github.com/smartlearn-team/app",
            Status = "Submitted",
            SubmissionDate = new DateTime(2026, 7, 11, 10, 30, 0, DateTimeKind.Utc)
        };
        var subDelta_bk = new Submissions
        {
            CompetitionId = comp1.CompetitionId, RegistrationId = reg4.RegistrationId,
            TeamId = teamDelta.TeamId, CompetitionRoundId = roundBanKet.RoundId,
            Title = "GreenCity – Dashboard quản lý môi trường đô thị thông minh",
            Description = "Hệ thống thu thập dữ liệu từ 200+ cảm biến IoT phân bố trong thành phố, hiển thị trực quan và dự báo ô nhiễm bằng mô hình ML.",
            FileUrl = "/uploads/submissions/delta_banket_report.pdf",
            VideoUrl = "https://youtu.be/greencity-demo-2026",
            ProjectLink = "https://github.com/greencity-team/platform",
            Status = "Submitted",
            SubmissionDate = new DateTime(2026, 7, 13, 14, 0, 0, DateTimeKind.Utc)
        };
        var subInd1_bk = new Submissions
        {
            CompetitionId = comp1.CompetitionId, RegistrationId = reg5.RegistrationId,
            TeamId = null, CompetitionRoundId = roundBanKet.RoundId,
            Title = "FoodSafe – Ứng dụng kiểm soát an toàn thực phẩm bằng AI",
            Description = "Ứng dụng chụp ảnh thực phẩm, dùng computer vision phát hiện thực phẩm biến chất và truy xuất nguồn gốc thông qua QR code.",
            FileUrl = "/uploads/submissions/ind1_banket_report.pdf",
            Status = "Submitted",
            SubmissionDate = new DateTime(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc)
        };
        var subInd2_bk = new Submissions
        {
            CompetitionId = comp1.CompetitionId, RegistrationId = reg6.RegistrationId,
            TeamId = null, CompetitionRoundId = roundBanKet.RoundId,
            Title = "EduMap – Bản đồ học tập trực tuyến theo kỹ năng",
            Description = "Nền tảng trực quan hóa lộ trình học lập trình, kết nối với các khoá học mở và cộng đồng mentor để hỗ trợ sinh viên tự học.",
            FileUrl = "/uploads/submissions/ind2_banket_report.pdf",
            Status = "Under Review",
            SubmissionDate = new DateTime(2026, 7, 9, 16, 0, 0, DateTimeKind.Utc)
        };
        db.Submissions.AddRange(subBeta_bk, subGamma_bk, subDelta_bk, subInd1_bk, subInd2_bk);
        await db.SaveChangesAsync();

        // ── Thêm giám khảo vào pool (judge4) để có thêm lựa chọn khi bulk assign ──
        var judge4 = new Users { FullName = "Võ Thị Thẩm Định",  Email = "judge4@test.com", PhoneNumber = "0900000019", PasswordHash = hash, IsActive = true, CreatedAt = now };
        db.Users.Add(judge4);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new UserRoles { UserId = judge4.UserId, RoleId = 4 });
        await db.SaveChangesAsync();

        // Gán judge4 vào vòng Bán kết
        var j4_bk = new Judges
        {
            UserId = judge4.UserId, CompetitionId = comp1.CompetitionId,
            RoundId = roundBanKet.RoundId, JudgeRole = "Member",
            Expertise = "Phân tích dữ liệu & Machine Learning",
            Priority = 4, Status = "Active", AssignedDate = now
        };
        db.Judges.Add(j4_bk);
        await db.SaveChangesAsync();
    }
}
