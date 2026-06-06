using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface ICompetitionService
{
    Task<List<Competitions>> GetAllCompetitionsAsync();
    Task<Competitions> GetCompetitionByIdAsync(int id);
    Task<List<Teams>> GetCompetitionTeamsAsync(int competitionId);
    Task<List<Registrations>> GetCompetitionRegistrationsAsync(int competitionId);
    Task<OrganizerContestsViewModel> GetOrganizerContestsAsync(string? searchQuery, string? statusFilter, string? categoryFilter, int pageNumber = 1);
    Task<CompetitionDetailViewModel> GetCompetitionDetailAsync(int competitionId);
    Task<int> CreateCompetitionAsync(CreateCompetitionViewModel model);
    Task<EditCompetitionViewModel> GetCompetitionForEditAsync(int competitionId);
    Task<bool> UpdateCompetitionAsync(int competitionId, EditCompetitionViewModel model);
    Task<bool> DeleteCompetitionAsync(int competitionId);
    Task<bool> ChangeCompetitionStatusAsync(int competitionId, string newStatus);
    Task<OrganizerDashboardViewModel> GetOrganizerDashboardDataAsync();

    /// <summary>Thêm đợt đăng ký mới. Chỉ được gọi khi cuộc thi chưa bắt đầu.</summary>
    Task<bool> AddRegistrationRoundAsync(int competitionId, RegistrationRoundCreateDto roundDto);

    /// <summary>Xóa đợt đăng ký. Chỉ được xóa khi round chưa có đăng ký nào và cuộc thi chưa bắt đầu.</summary>
    Task<bool> DeleteRegistrationRoundAsync(int roundId, int competitionId);

    // ─── Competition Rounds (Vòng thi) ─────────────────────────────────────
    Task<bool> AddCompetitionRoundAsync(int competitionId, CompetitionRoundCreateDto dto);
    Task<bool> UpdateCompetitionRoundAsync(int roundId, int competitionId, UpdateCompetitionRoundDto dto);
    Task<bool> DeleteCompetitionRoundAsync(int roundId, int competitionId);

    // ─── Images ────────────────────────────────────────────────────────────
    Task<bool> UploadImagesAsync(int competitionId, List<string> base64DataList);
    Task<bool> DeleteImageAsync(int imageId, int competitionId);
    Task<bool> SetThumbnailAsync(int imageId, int competitionId);

    // ─── Documents ─────────────────────────────────────────────────────────
    Task<bool> UploadDocumentsAsync(int competitionId, List<string> base64DataList, List<string> fileNames);
    Task<bool> DeleteDocumentAsync(int documentId, int competitionId);
    Task<(byte[] data, string fileName, string contentType)?> GetDocumentFileAsync(int documentId, int competitionId);

    // ─── Sponsors ──────────────────────────────────────────────────────────
    Task<List<SponsorSearchDto>> GetAllSponsorsForSearchAsync();
    Task<bool> AddSponsorToCompetitionAsync(int competitionId, AddSponsorToCompetitionDto dto);
    Task<bool> CreateAndLinkSponsorAsync(int competitionId, SponsorCreateDto dto);
    Task<bool> RemoveSponsorFromCompetitionAsync(int competitionSponsorId, int competitionId);
    Task<bool> UpdateSponsorLinkAsync(int competitionSponsorId, AddSponsorToCompetitionDto dto);
}
