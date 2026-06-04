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
            _submissionService  = submissionService;
            _logger             = logger;
        }

        public IActionResult Index() => View();

        // 1. Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var data = await _competitionService.GetOrganizerDashboardDataAsync();
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard.";
                return View(new OrganizerDashboardViewModel());
            }
        }

        // 2. Danh sách cuộc thi
        public async Task<IActionResult> Contests(string? search, string? status, string? category, int page = 1)
        {
            var viewModel = await _competitionService.GetOrganizerContestsAsync(search, status, category, page);
            return View(viewModel);
        }

        // === TẠO MỚI CUỘC THI ===
        [HttpGet]
        public IActionResult Create()
        {
            var now   = DateTime.Now;
            var model = new CreateCompetitionViewModel
            {
                StartDate          = now.AddDays(15),
                EndDate            = now.AddDays(45),
                SubmissionDeadline = now.AddDays(40),
                MinParticipants    = 0,
                MaxParticipants    = 100,
                MaxTeamSize        = 1,
                RegistrationRounds = new List<RegistrationRoundCreateDto>
                {
                    new() { RoundName = "Đợt 1", StartDate = now.AddDays(1), EndDate = now.AddDays(10) }
                }
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCompetitionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

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
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditCompetitionViewModel model)
        {
            if (model.CompetitionId != id) return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _competitionService.UpdateCompetitionAsync(id, model);
                if (!result) return NotFound();

                TempData["SuccessMessage"] = "Cập nhật cuộc thi thành công!";
                return RedirectToAction("Contests");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning("Validation error updating competition {Id}: {Message}", id, ex.Message);
                // Reload readonly data
                var reloaded = await _competitionService.GetCompetitionForEditAsync(id);
                if (reloaded != null)
                {
                    model.ExistingRounds     = reloaded.ExistingRounds;
                    model.RegistrationCount  = reloaded.RegistrationCount;
                    model.SubmissionCount    = reloaded.SubmissionCount;
                    model.HasStarted         = reloaded.HasStarted;
                    model.HasSubmissions     = reloaded.HasSubmissions;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
                _logger.LogError(ex, "Error updating competition {Id}", id);
                return View(model);
            }
        }

        // === THÊM ĐỢT ĐĂNG KÝ MỚI (API) ===
        [HttpPost]
        public async Task<IActionResult> AddRegistrationRound(int competitionId, [FromBody] RegistrationRoundCreateDto roundDto)
        {
            try
            {
                if (roundDto == null || string.IsNullOrWhiteSpace(roundDto.RoundName))
                    return Json(new { success = false, message = "Thông tin đợt đăng ký không hợp lệ." });

                var result = await _competitionService.AddRegistrationRoundAsync(competitionId, roundDto);
                if (!result) return Json(new { success = false, message = "Cuộc thi không tìm thấy." });

                return Json(new { success = true, message = "Thêm đợt đăng ký thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Validation error adding round to competition {Id}: {Message}", competitionId, ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding registration round to competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // === XÓA CUỘC THI ===
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _competitionService.DeleteCompetitionAsync(id);
                if (!result) return NotFound();

                TempData["SuccessMessage"] = "Xóa cuộc thi thành công!";
                return RedirectToAction("Contests");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogWarning("Cannot delete competition {Id}: {Message}", id, ex.Message);
                return RedirectToAction("Contests");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                _logger.LogError(ex, "Error deleting competition {Id}", id);
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
                if (!result) return NotFound();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing competition status {Id}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Chi tiết cuộc thi (dùng cho modal)
        [HttpGet]
        public async Task<IActionResult> GetCompetitionDetail(int id)
        {
            var detail = await _competitionService.GetCompetitionDetailAsync(id);
            if (detail == null) return NotFound();
            return Json(detail);
        }

        // ====== SUBMISSIONS ======
        public async Task<IActionResult> Submissions(
            int? competitionId = null, string? search = null,
            string? status = null, string? department = null)
        {
            try
            {
                var viewModel = await _submissionService.GetSubmissionsViewAsync(competitionId, search, status, department);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading submissions page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang.";
                return View(new OrganizerSubmissionsViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRegistration(int registrationId, string? feedback)
        {
            try
            {
                var result = await _submissionService.ApproveRegistrationAsync(registrationId, feedback);
                if (!result) return Json(new { success = false, message = "Hồ sơ không tìm thấy." });
                return Json(new { success = true, message = "Duyệt hồ sơ thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving registration");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectRegistration(int registrationId, string reason)
        {
            try
            {
                var result = await _submissionService.RejectRegistrationAsync(registrationId, reason);
                if (!result) return Json(new { success = false, message = "Hồ sơ không tìm thấy." });
                return Json(new { success = true, message = "Từ chối hồ sơ thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting registration");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RequestSupplement(int registrationId, string feedback)
        {
            try
            {
                var result = await _submissionService.RequestSupplementAsync(registrationId, feedback);
                if (!result) return Json(new { success = false, message = "Hồ sơ không tìm thấy." });
                return Json(new { success = true, message = "Yêu cầu bổ sung đã gửi!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting supplement");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ====== IMAGES ======

        [HttpPost]
        public async Task<IActionResult> UploadImages(int competitionId, [FromBody] List<string> imageDataList)
        {
            try
            {
                if (imageDataList == null || !imageDataList.Any())
                    return Json(new { success = false, message = "Không có ảnh nào được gửi." });

                await _competitionService.UploadImagesAsync(competitionId, imageDataList);
                return Json(new { success = true, message = "Upload ảnh thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading images for competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi upload ảnh." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId, int competitionId)
        {
            try
            {
                var result = await _competitionService.DeleteImageAsync(imageId, competitionId);
                if (!result) return Json(new { success = false, message = "Ảnh không tìm thấy." });
                return Json(new { success = true, message = "Xóa ảnh thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetThumbnail(int imageId, int competitionId)
        {
            try
            {
                var result = await _competitionService.SetThumbnailAsync(imageId, competitionId);
                if (!result) return Json(new { success = false, message = "Ảnh không tìm thấy." });
                return Json(new { success = true, message = "Đã đặt làm ảnh đại diện!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting thumbnail {ImageId}", imageId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        // ====== DOCUMENTS ======

        [HttpPost]
        public async Task<IActionResult> UploadDocuments(int competitionId, [FromBody] UploadDocumentsRequest request)
        {
            try
            {
                if (request?.DataList == null || !request.DataList.Any())
                    return Json(new { success = false, message = "Không có tài liệu nào được gửi." });

                await _competitionService.UploadDocumentsAsync(competitionId, request.DataList, request.FileNames ?? new());
                return Json(new { success = true, message = "Upload tài liệu thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading documents for competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi upload tài liệu." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int documentId, int competitionId)
        {
            try
            {
                var result = await _competitionService.DeleteDocumentAsync(documentId, competitionId);
                if (!result) return Json(new { success = false, message = "Tài liệu không tìm thấy." });
                return Json(new { success = true, message = "Xóa tài liệu thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int documentId, int competitionId)
        {
            try
            {
                var result = await _competitionService.GetDocumentFileAsync(documentId, competitionId);
                if (result == null) return NotFound("Tài liệu không tìm thấy.");
                return File(result.Value.data, result.Value.contentType, result.Value.fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                return StatusCode(500);
            }
        }

        // ====== SPONSORS ======

        [HttpGet]
        public async Task<IActionResult> GetAllSponsors()
        {
            var sponsors = await _competitionService.GetAllSponsorsForSearchAsync();
            return Json(sponsors);
        }

        [HttpPost]
        public async Task<IActionResult> AddSponsor(int competitionId, [FromBody] AddSponsorToCompetitionDto dto)
        {
            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                await _competitionService.AddSponsorToCompetitionAsync(competitionId, dto);
                return Json(new { success = true, message = "Thêm nhà tài trợ thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding sponsor to competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSponsor(int competitionId, [FromBody] SponsorCreateDto dto)
        {
            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                await _competitionService.CreateAndLinkSponsorAsync(competitionId, dto);
                return Json(new { success = true, message = "Tạo và liên kết nhà tài trợ thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sponsor for competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSponsor(int competitionSponsorId, int competitionId)
        {
            try
            {
                var result = await _competitionService.RemoveSponsorFromCompetitionAsync(competitionSponsorId, competitionId);
                if (!result) return Json(new { success = false, message = "Liên kết nhà tài trợ không tìm thấy." });
                return Json(new { success = true, message = "Xóa nhà tài trợ thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing sponsor {CsId}", competitionSponsorId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSponsorLink(int competitionSponsorId, [FromBody] AddSponsorToCompetitionDto dto)
        {
            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                var result = await _competitionService.UpdateSponsorLinkAsync(competitionSponsorId, dto);
                if (!result) return Json(new { success = false, message = "Liên kết không tìm thấy." });
                return Json(new { success = true, message = "Cập nhật nhà tài trợ thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sponsor link {CsId}", competitionSponsorId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        public IActionResult Grading()       => View();
        public IActionResult Results()       => View();
        public IActionResult Notifications() => View();
        public IActionResult Progress()      => View();
    }

    // Request DTO cho upload documents
    public class UploadDocumentsRequest
    {
        public List<string> DataList { get; set; } = new();
        public List<string> FileNames { get; set; } = new();
    }
}
