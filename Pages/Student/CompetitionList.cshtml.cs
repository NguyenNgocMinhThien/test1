using Microsoft.AspNetCore.Mvc.RazorPages;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Pages.Student
{
    public class CompetitionListModel : PageModel
    {
        private readonly ILogger<CompetitionListModel> _logger;

        public CompetitionListModel(ILogger<CompetitionListModel> logger)
        {
            _logger = logger;
        }

        public CompetitionListViewModel ViewModel { get; set; } = new();

        public void OnGet(int page = 1, string? searchQuery = null, string? statusFilter = null, string? categoryFilter = null)
        {
            try
            {
                ViewModel.PageNumber = Math.Max(1, page);
                ViewModel.SearchQuery = searchQuery;
                ViewModel.StatusFilter = statusFilter;
                ViewModel.CategoryFilter = categoryFilter;

                // Using mock data for testing
                var allCompetitions = CompetitionMockData.GetMockCompetitions();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    allCompetitions = allCompetitions
                        .Where(c => c.CompetitionName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   (c.ShortDescription?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    allCompetitions = allCompetitions
                        .Where(c => c.Status == statusFilter)
                        .ToList();
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    allCompetitions = allCompetitions
                        .Where(c => c.Category == categoryFilter)
                        .ToList();
                }

                ViewModel.TotalCount = allCompetitions.Count;

                // Apply pagination
                ViewModel.Competitions = allCompetitions
                    .OrderByDescending(c => c.RegistrationDeadline)
                    .Skip((ViewModel.PageNumber - 1) * ViewModel.PageSize)
                    .Take(ViewModel.PageSize)
                    .ToList();

                _logger.LogInformation($"Loaded competitions page {page} with filters: search='{searchQuery}', status='{statusFilter}', category='{categoryFilter}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading competitions");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tải danh sách cuộc thi");
            }
        }
    }
}