namespace Web_cham_diem.Models.ViewModels;

public class ResultsPageViewModel
{
    public int? SelectedCompetitionId { get; set; }

    // Overall summary across all competitions
    public int TotalCompetitions { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalEvaluatedSubmissions { get; set; }
    public int TotalRankedRounds { get; set; }

    // Competition list for dropdown selector
    public List<CompetitionSelectorDto> Competitions { get; set; } = new();

    // Summary list of all competitions (for the aggregate report tab)
    public List<CompetitionResultSummaryDto> CompetitionSummaries { get; set; } = new();

    // Detailed data for the selected competition
    public CompetitionResultDetailDto? SelectedDetail { get; set; }
}

public class CompetitionSelectorDto
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CompetitionResultSummaryDto
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalSubmissions { get; set; }
    public int EvaluatedSubmissions { get; set; }
    public decimal AverageScore { get; set; }
    public int TotalRounds { get; set; }
    public int CompletedRounds { get; set; }
    public double GradingCompletionRate { get; set; }
    public int TotalAwardedEntries { get; set; }
}

public class CompetitionResultDetailDto
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Prize { get; set; }

    public int TotalRegistrations { get; set; }
    public int ApprovedRegistrations { get; set; }
    public int TotalSubmissions { get; set; }
    public int EvaluatedSubmissions { get; set; }
    public int TotalJudges { get; set; }
    public decimal AverageScore { get; set; }
    public double GradingCompletionRate { get; set; }
    public int TotalAwardedEntries { get; set; }

    // Score distribution (% of max score bands)
    public int ScoreBelow50 { get; set; }
    public int Score50To65 { get; set; }
    public int Score65To80 { get; set; }
    public int Score80To90 { get; set; }
    public int ScoreAbove90 { get; set; }

    // Award breakdown
    public int FirstPrizeCount { get; set; }
    public int SecondPrizeCount { get; set; }
    public int ThirdPrizeCount { get; set; }
    public int EncouragementPrizeCount { get; set; }
    public int NoAwardCount { get; set; }

    public List<RoundResultDetailDto> Rounds { get; set; } = new();
}

public class RoundResultDetailDto
{
    public int RoundId { get; set; }
    public string RoundName { get; set; } = string.Empty;
    public int RoundOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsResultsPublished { get; set; }
    public DateTime? ResultsPublishedAt { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int SubmissionCount { get; set; }
    public int EvaluatedCount { get; set; }
    public decimal AverageScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public List<ScoringCriteriaResultDto> Criteria { get; set; } = new();
    public List<SubmissionRankingDetailDto> Rankings { get; set; } = new();
}

public class ScoringCriteriaResultDto
{
    public int CriteriaId { get; set; }
    public string CriteriaName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
}

public class SubmissionRankingDetailDto
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
    public string AwardLevel { get; set; } = "";
}

// Một dòng trong lịch sử công bố / chỉnh sửa kết quả (mục "Lịch sử" trên trang Thống kê & Báo cáo)
public class ResultHistoryItemDto
{
    public int HistoryId { get; set; }
    public DateTime EditedAt { get; set; }
    public string ActionType { get; set; } = string.Empty; // Publish | Unpublish | ScoreEdited | ScoreApproved | ScoreRejected
    public string ChangesSummary { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string EditorName { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string? RoundName { get; set; }
    public string? SubmissionTitle { get; set; }
}
