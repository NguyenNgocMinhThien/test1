namespace Web_cham_diem.Models.ViewModels;

// ViewModel cho trang "Thống kê & Báo cáo" (Views/Organizer/Statistics.cshtml)
public class OrganizerStatisticsViewModel
{
    public int? SelectedCompetitionId { get; set; }
    public List<CompetitionSelectorDto> Competitions { get; set; } = new();

    // ===== TỔNG QUAN =====
    public int TotalCompetitions { get; set; }
    public int ActiveCompetitions { get; set; }
    public int CompletedCompetitions { get; set; }
    public int DraftCompetitions { get; set; }

    public int TotalRegistrations { get; set; }
    public int ApprovedRegistrations { get; set; }
    public int TotalSubmissions { get; set; }
    public int EvaluatedSubmissions { get; set; }
    public decimal GradingCompletionRate { get; set; }
    public int TotalAwardedEntries { get; set; }

    // ===== KẾT QUẢ CHẤM ĐIỂM THEO CUỘC THI / VÒNG THI =====
    public List<StatCompetitionRoundRow> RoundResults { get; set; } = new();

    // ===== DANH SÁCH ĐẠT GIẢI =====
    public List<StatAwardRow> AwardWinners { get; set; } = new();
}

public class StatCompetitionRoundRow
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string CompetitionStatus { get; set; } = string.Empty;
    public string RoundName { get; set; } = string.Empty;
    public int RoundOrder { get; set; }
    public bool IsResultsPublished { get; set; }
    public int SubmissionCount { get; set; }
    public int EvaluatedCount { get; set; }
    public double GradingProgress { get; set; }
    public decimal AverageScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public int AwardedCount { get; set; }
}

public class StatAwardRow
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string RoundName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public string AwardLevel { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public bool IsTeam { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal ScorePercentage { get; set; }
}
