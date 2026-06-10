namespace Web_cham_diem.Models.ViewModels;

public class ResultsPageViewModel
{
    public string? SearchQuery { get; set; }
    public string? StatusFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public List<string> AvailableCategories { get; set; } = new();
    public List<CompetitionResultDto> Results { get; set; } = new();
    public int TotalCompetitions { get; set; }
    public int TotalRankedRounds { get; set; }
}

public class CompetitionResultDto
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
    public List<RoundResultDto> Rounds { get; set; } = new();
}

public class RoundResultDto
{
    public int RoundId { get; set; }
    public string RoundName { get; set; } = string.Empty;
    public int RoundOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ScoringCriteriaResultDto> Criteria { get; set; } = new();
    public List<SubmissionRankingDto> Rankings { get; set; } = new();
}

public class ScoringCriteriaResultDto
{
    public int CriteriaId { get; set; }
    public string CriteriaName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
}

public class SubmissionRankingDto
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
