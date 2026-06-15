namespace Web_cham_diem.Models.ViewModels;

// ViewModel for the public-facing /Results page (Views/Pages/Results.cshtml)
// The organizer's results page uses ResultsPageViewModel instead.

public class PublicResultsViewModel
{
    public string? SearchQuery { get; set; }
    public string? StatusFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public List<string> AvailableCategories { get; set; } = new();
    public List<PublicCompetitionResultDto> Results { get; set; } = new();
    public int TotalCompetitions { get; set; }
    public int TotalRankedRounds { get; set; }
}

public class PublicCompetitionResultDto
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Prize { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int TotalEvaluatedSubmissions { get; set; }
    public List<PublicRoundResultDto> Rounds { get; set; } = new();
}

public class PublicRoundResultDto
{
    public int RoundId { get; set; }
    public string RoundName { get; set; } = string.Empty;
    public int RoundOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<PublicScoringCriteriaDto> Criteria { get; set; } = new();
    public List<PublicSubmissionRankingDto> Rankings { get; set; } = new();
}

public class PublicScoringCriteriaDto
{
    public int CriteriaId { get; set; }
    public string CriteriaName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
}

public class PublicSubmissionRankingDto
{
    public int Rank { get; set; }
    public int SubmissionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public bool IsTeam { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal ScorePercentage { get; set; }
    public Dictionary<int, decimal> CriteriaScores { get; set; } = new();
    public int JudgeCount { get; set; }
}
