namespace Web_cham_diem.Models.ViewModels;

// ViewModel cho trang tra cứu kết quả cá nhân/đội thi bằng mã dự thi (/TraCuuKetQua)
public class LookupResultViewModel
{
    public string? Code { get; set; }
    public bool Searched { get; set; }
    public bool Found { get; set; }

    // Thông tin hồ sơ đăng ký
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public bool IsTeam { get; set; }
    public string? RegistrationCode { get; set; }
    public string? TeamCode { get; set; }
    public string RegistrationStatus { get; set; } = string.Empty;

    // Thông tin bài dự thi
    public bool HasSubmission { get; set; }
    public string? SubmissionTitle { get; set; }
    public string? SubmissionStatus { get; set; }
    public DateTime? SubmissionDate { get; set; }

    // Có vòng thi được thiết lập cho cuộc thi này không (dùng để phân biệt "chưa có vòng" và "chưa gắn vòng")
    public bool CompetitionHasRounds { get; set; }

    // Kết quả vòng thi hiện tại của bài dự thi (0 hoặc 1 phần tử — mỗi bài chỉ gắn với 1 vòng tại một thời điểm)
    public List<LookupRoundResultDto> Rounds { get; set; } = new();
}

public class LookupRoundResultDto
{
    public string RoundName { get; set; } = string.Empty;
    public int RoundOrder { get; set; }
    public bool IsPublished { get; set; }
    public bool HasEntry { get; set; }
    public int Rank { get; set; }
    public int TotalParticipants { get; set; }
    public string? AwardLevel { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal ScorePercentage { get; set; }
    public List<PublicScoringCriteriaDto> Criteria { get; set; } = new();
    public Dictionary<int, decimal> CriteriaScores { get; set; } = new();
    public int JudgeCount { get; set; }
}
