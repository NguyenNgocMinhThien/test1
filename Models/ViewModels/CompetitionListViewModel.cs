namespace Web_cham_diem.Models.ViewModels
{
    public class CompetitionListViewModel
    {
        public List<CompetitionCardDto> Competitions { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public string? CategoryFilter { get; set; }

        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class CompetitionCardDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public string? ShortDescription { get; set; }
        public string? BannerImageUrl { get; set; }
        public string? Category { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public string Status { get; set; } = string.Empty; // "Đang mở đăng ký", "Sắp diễn ra", "Đã kết thúc"
        public int ParticipantCount { get; set; }
        public bool IsUserRegistered { get; set; }
    }
}