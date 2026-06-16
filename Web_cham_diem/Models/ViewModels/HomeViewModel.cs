namespace Web_cham_diem.Models.ViewModels
{
    public class HomeViewModel
    {
        public int ActiveCompetitionsCount { get; set; }
        public int TotalParticipantsCount { get; set; }
        public int TotalCategoriesCount { get; set; }
        public int TotalEvaluatedCount { get; set; }

        public List<HomeFeaturedCompetitionDto> FeaturedCompetitions { get; set; } = new();
        public List<HomeTopResultDto> TopResults { get; set; } = new();
        public List<HomeNotificationDto> LatestNotifications { get; set; } = new();
    }

    public class HomeFeaturedCompetitionDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Prize { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public int DaysRemaining { get; set; } // -1 = no open round
        public int MaxParticipants { get; set; }
        public bool IsTeamBased { get; set; }
        public int MaxTeamSize { get; set; }
        public string? BannerImageUrl { get; set; }
        public int TotalRegistrations { get; set; }
    }

    public class HomeTopResultDto
    {
        public string ParticipantName { get; set; } = string.Empty;
        public string CompetitionName { get; set; } = string.Empty;
        public string AwardLevel { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public int Rank { get; set; }
    }

    public class HomeNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
        public DateTime CreatedAt { get; set; }
        public string CategoryLabel { get; set; } = string.Empty;
        public int? RelatedEntityId { get; set; }
    }
}
