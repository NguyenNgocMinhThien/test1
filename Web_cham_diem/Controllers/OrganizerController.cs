using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Services;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly ICompetitionService _competitionService;
        private readonly ISubmissionService _submissionService;
        private readonly ILogger<OrganizerController> _logger;

        public OrganizerController(
            ICompetitionService competitionService,
            ISubmissionService submissionService,
            ILogger<OrganizerController> logger)
        {
            _competitionService = competitionService;
            _submissionService = submissionService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // 1. Dashboard - Tổng quan chung
        public IActionResult Dashboard()
        {
            return View();
        }

        // 2. Quản lý cuộc thi (Tạo mới, sửa, xóa cuộc thi)
        public async Task<IActionResult> Contests(string? search, string? status, string? category, int page = 1)
        {
            var viewModel = await _competitionService.GetOrganizerContestsAsync(search, status, category, page);
            return View(viewModel);
        }

        // === TẠO MỚI CUỘC THI ===
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateCompetitionViewModel
            {
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(31),
                RegistrationDeadline = DateTime.UtcNow.AddDays(10),
                SubmissionDeadline = DateTime.UtcNow.AddDays(20),
                MaxParticipants = 100,
                MaxTeamSize = 5
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCompetitionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var competitionId = await _competitionService.CreateCompetitionAsync(model);
                TempData["SuccessMessage"] = "Tạo cuộc thi thành công!";
                return RedirectToAction("Edit", new { id = competitionId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning("Validation error creating competition: {Message}", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
                _logger.LogError(ex, "Error creating competition");
                return View(model);
            }
        }

        // === CHỈNH SỬA CUỘC THI ===
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _competitionService.GetCompetitionForEditAsync(id);
            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditCompetitionViewModel model)
        {
            if (model.CompetitionId != id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _competitionService.UpdateCompetitionAsync(id, model);
                if (!result)
                    return NotFound();

                TempData["SuccessMessage"] = "Cập nhật cuộc thi thành công!";
                return RedirectToAction("Contests");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning("Validation error updating competition {CompetitionId}: {Message}", id, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
                _logger.LogError(ex, "Error updating competition {CompetitionId}", id);
                return View(model);
            }
        }

        // === XÓA CUỘC THI ===
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _competitionService.DeleteCompetitionAsync(id);
                if (!result)
                    return NotFound();

                TempData["SuccessMessage"] = "Xóa cuộc thi thành công!";
                return RedirectToAction("Contests");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogWarning("Cannot delete competition {CompetitionId}: {Message}", id, ex.Message);
                return RedirectToAction("Contests");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                _logger.LogError(ex, "Error deleting competition {CompetitionId}", id);
                return RedirectToAction("Contests");
            }
        }

        // === THAY ĐỔI TRẠNG THÁI ===
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            try
            {
                var result = await _competitionService.ChangeCompetitionStatusAsync(id, status);
                if (!result)
                    return NotFound();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing competition status {CompetitionId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API để lấy chi tiết cuộc thi (dùng cho modal)
        [HttpGet]
        public async Task<IActionResult> GetCompetitionDetail(int id)
        {
            var detail = await _competitionService.GetCompetitionDetailAsync(id);
            if (detail == null)
                return NotFound();

            return Json(detail);
        }

        // ============== SUBMISSIONS - HỒSƠ VÀ BÀI NỘP ==============

        // 3. Quản lý đăng ký & Bài dự thi (Duyệt hồ sơ, thu bài)
        public async Task<IActionResult> Submissions(
            int? competitionId = null,
            string? search = null,
            string? status = null,
            string? department = null)
        {
            try
            {
                var viewModel = await _submissionService.GetSubmissionsViewAsync(
                    competitionId, search, status, department);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading submissions page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang.";
                return View(new OrganizerSubmissionsViewModel());
            }
        }

        // API: Duyệt hồ sơ
        [HttpPost]
        public async Task<IActionResult> ApproveRegistration(int registrationId, string? feedback)
        {
            try
            {
                var result = await _submissionService.ApproveRegistrationAsync(registrationId, feedback);
                if (!result)
                    return Json(new { success = false, message = "Hồ sơ không tìm thấy" });

                return Json(new { success = true, message = "Duyệt hồ sơ thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving registration");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Từ chối hồ sơ
        [HttpPost]
        public async Task<IActionResult> RejectRegistration(int registrationId, string reason)
        {
            try
            {
                var result = await _submissionService.RejectRegistrationAsync(registrationId, reason);
                if (!result)
                    return Json(new { success = false, message = "Hồ sơ không tìm thấy" });

                return Json(new { success = true, message = "Từ chối hồ sơ thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting registration");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Yêu cầu bổ sung
        [HttpPost]
        public async Task<IActionResult> RequestSupplement(int registrationId, string feedback)
        {
            try
            {
                var result = await _submissionService.RequestSupplementAsync(registrationId, feedback);
                if (!result)
                    return Json(new { success = false, message = "Hồ sơ không tìm thấy" });

                return Json(new { success = true, message = "Yêu cầu bổ sung đã gửi!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting supplement");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 4. Quản lý chấm điểm (Phân công giám khảo, theo dõi điểm)
        public IActionResult Grading()
        {
            return View();
        }

        // 5. Kết quả & Báo cáo (Tổng hợp điểm, xuất file)
        public IActionResult Results()
        {
            return View();
        }

        // 6. Thông báo cuộc thi (Nhắn tin, nhắc nhở deadline)
        public IActionResult Notifications()
        {
            return View();
        }

        // 7. Theo dõi tiến độ (Tiến trình chung)
        public IActionResult Progress()
        {
            return View();
        }
    }
}