namespace Web_cham_diem.Models.ViewModels
{
    public static class CompetitionMockData
    {
        public static List<CompetitionCardDto> GetMockCompetitions()
        {
            return new List<CompetitionCardDto>
            {
                new CompetitionCardDto
                {
                    CompetitionId = 1,
                    CompetitionName = "Cuộc thi Lập trình Web 2025",
                    Topic = "Công nghệ web hiện đại",
                    ShortDescription = "Thi thiết kế và phát triển ứng dụng web sáng tạo sử dụng HTML, CSS, JavaScript",
                    BannerImageUrl = "/images/competitions/web-dev-banner.jpg",
                    Category = "Công nghệ thông tin",
                    RegistrationDeadline = DateTime.Now.AddDays(15),
                    Status = "Đang mở đăng ký",
                    ParticipantCount = 45,
                    IsUserRegistered = false
                },
                new CompetitionCardDto
                {
                    CompetitionId = 2,
                    CompetitionName = "Cuộc thi Thiết kế Giao diện Người dùng",
                    Topic = "UX/UI Design",
                    ShortDescription = "Sáng tạo giao diện người dùng thân thiện và trực quan cho ứng dụng di động",
                    BannerImageUrl = "/images/competitions/ui-ux-banner.jpg",
                    Category = "Thiết kế",
                    RegistrationDeadline = DateTime.Now.AddDays(8),
                    Status = "Đang mở đăng ký",
                    ParticipantCount = 32,
                    IsUserRegistered = true
                },
                new CompetitionCardDto
                {
                    CompetitionId = 3,
                    CompetitionName = "Cuộc thi Khởi nghiệp Công nghệ",
                    Topic = "Startup Innovation",
                    ShortDescription = "Trình diễn ý tưởng kinh doanh công nghệ sáng tạo với tiềm năng thương mại",
                    BannerImageUrl = "/images/competitions/startup-banner.jpg",
                    Category = "Kinh tế số",
                    RegistrationDeadline = DateTime.Now.AddDays(25),
                    Status = "Đang mở đăng ký",
                    ParticipantCount = 28,
                    IsUserRegistered = false
                },
                new CompetitionCardDto
                {
                    CompetitionId = 4,
                    CompetitionName = "Cuộc thi Viết bài Khoa học",
                    Topic = "Nghiên cứu khoa học",
                    ShortDescription = "Viết bài khoa học chất lượng cao liên quan đến các lĩnh vực công nghệ",
                    BannerImageUrl = "/images/competitions/research-banner.jpg",
                    Category = "Khoa học",
                    RegistrationDeadline = DateTime.Now.AddDays(-5),
                    Status = "Sắp diễn ra",
                    ParticipantCount = 18,
                    IsUserRegistered = false
                },
                new CompetitionCardDto
                {
                    CompetitionId = 5,
                    CompetitionName = "Cuộc thi Phát triển Ứng dụng Di động",
                    Topic = "Mobile App Development",
                    ShortDescription = "Phát triển ứng dụng di động hữu ích cho nền tảng iOS hoặc Android",
                    BannerImageUrl = "/images/competitions/mobile-banner.jpg",
                    Category = "Công nghệ thông tin",
                    RegistrationDeadline = DateTime.Now.AddDays(-15),
                    Status = "Đã kết thúc",
                    ParticipantCount = 52,
                    IsUserRegistered = true
                },
                new CompetitionCardDto
                {
                    CompetitionId = 6,
                    CompetitionName = "Cuộc thi Trí tuệ Nhân tạo",
                    Topic = "AI & Machine Learning",
                    ShortDescription = "Ứng dụng AI và Machine Learning vào giải quyết các vấn đề thực tế",
                    BannerImageUrl = "/images/competitions/ai-banner.jpg",
                    Category = "Công nghệ thông tin",
                    RegistrationDeadline = DateTime.Now.AddDays(30),
                    Status = "Đang mở đăng ký",
                    ParticipantCount = 38,
                    IsUserRegistered = false
                },
                new CompetitionCardDto
                {
                    CompetitionId = 7,
                    CompetitionName = "Cuộc thi Thiết kế Đồ họa",
                    Topic = "Graphic Design",
                    ShortDescription = "Sáng tạo các thiết kế đồ họa độc đáo cho các nhãn hiệu hoặc dự án",
                    BannerImageUrl = "/images/competitions/graphic-banner.jpg",
                    Category = "Thiết kế",
                    RegistrationDeadline = DateTime.Now.AddDays(12),
                    Status = "Đang mở đăng ký",
                    ParticipantCount = 41,
                    IsUserRegistered = false
                },
                new CompetitionCardDto
                {
                    CompetitionId = 8,
                    CompetitionName = "Cuộc thi Bảo mật Thông tin",
                    Topic = "Cybersecurity",
                    ShortDescription = "Kiểm tra và cải thiện tính bảo mật của các hệ thống phần mềm",
                    BannerImageUrl = "/images/competitions/security-banner.jpg",
                    Category = "Công nghệ thông tin",
                    RegistrationDeadline = DateTime.Now.AddDays(20),
                    Status = "Đang mở đăng ký",
                    ParticipantCount = 25,
                    IsUserRegistered = false
                }
            };
        }
    }
}