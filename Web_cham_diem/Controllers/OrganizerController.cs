using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Services;
using Web_cham_diem.Models.ViewModels;
using System.Text;
using System.Security.Claims;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Web_cham_diem.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : Controller
    {
        private readonly ICompetitionService _competitionService;
        private readonly ISubmissionService _submissionService;
        private readonly IGradingService _gradingService;
        private readonly IResultsService _resultsService;
        private readonly INotificationsService _notificationsService;
        private readonly ILogger<OrganizerController> _logger;

        public OrganizerController(
            ICompetitionService competitionService,
            ISubmissionService submissionService,
            IGradingService gradingService,
            IResultsService resultsService,
            INotificationsService notificationsService,
            ILogger<OrganizerController> logger)
        {
            _competitionService   = competitionService;
            _submissionService    = submissionService;
            _gradingService       = gradingService;
            _resultsService       = resultsService;
            _notificationsService = notificationsService;
            _logger               = logger;
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }

        // Kiểm tra quyền sở hữu cuộc thi; nếu không phải chủ trả về Json lỗi
        private async Task<IActionResult?> RequireOwnerJson(int competitionId)
        {
            var isOwner = await _competitionService.IsCompetitionOwnerAsync(competitionId, GetCurrentUserId());
            if (!isOwner)
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            return null;
        }

        public IActionResult Index() => View();

        // 1. Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var data = await _competitionService.GetOrganizerDashboardDataAsync(GetCurrentUserId());
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
            var viewModel = await _competitionService.GetOrganizerContestsAsync(search, status, category, page, GetCurrentUserId());
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
                MaxParticipants    = 100,
                MaxTeamSize        = 5,
                RegistrationRounds = new List<RegistrationRoundCreateDto>
                {
                    new() { RoundName = "Đợt 1", StartDate = now.AddDays(1), EndDate = now.AddDays(10) }
                },
                CompetitionRounds = new List<CompetitionRoundCreateDto>()
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
                var competitionId = await _competitionService.CreateCompetitionAsync(model, GetCurrentUserId());
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
            try
            {
                var model = await _competitionService.GetCompetitionForEditAsync(id, GetCurrentUserId());
                if (model == null) return NotFound();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading competition {Id} for edit", id);
                TempData["SuccessMessage"] = "Cuộc thi đã được tạo thành công! Tuy nhiên có lỗi khi tải trang chỉnh sửa.";
                return RedirectToAction("Contests");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditCompetitionViewModel model)
        {
            if (model.CompetitionId != id) return BadRequest();

            var organizerId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                await ReloadReadonlyData(id, model, organizerId);
                return View(model);
            }

            try
            {
                var result = await _competitionService.UpdateCompetitionAsync(id, model, organizerId);
                if (!result) return NotFound();

                TempData["SuccessMessage"] = "Cập nhật cuộc thi thành công!";
                return RedirectToAction("Contests");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning("Validation error updating competition {Id}: {Message}", id, ex.Message);
                await ReloadReadonlyData(id, model, organizerId);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
                _logger.LogError(ex, "Error updating competition {Id}", id);
                await ReloadReadonlyData(id, model, organizerId);
                return View(model);
            }
        }

        private async Task ReloadReadonlyData(int competitionId, EditCompetitionViewModel model, int organizerId)
        {
            try
            {
                var fresh = await _competitionService.GetCompetitionForEditAsync(competitionId, organizerId);
                if (fresh == null) return;
                model.ExistingRounds              = fresh.ExistingRounds;
                model.Images                      = fresh.Images;
                model.Documents                   = fresh.Documents;
                model.ExistingCompetitionSponsors = fresh.ExistingCompetitionSponsors;
                model.RegistrationCount           = fresh.RegistrationCount;
                model.SubmissionCount             = fresh.SubmissionCount;
                model.HasStarted                  = fresh.HasStarted;
                model.HasSubmissions              = fresh.HasSubmissions;
            }
            catch { /* không làm hỏng luồng chính */ }
        }

        // === CẬP NHẬT LỊCH TRÌNH CHUNG (API) ===
        [HttpPost]
        public async Task<IActionResult> UpdateScheduleDates(int competitionId, [FromBody] UpdateScheduleDatesDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                var (ok, msg) = await _competitionService.UpdateScheduleDatesAsync(competitionId, dto, GetCurrentUserId());
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule dates for competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // === THÊM ĐỢT ĐĂNG KÝ MỚI (API) ===
        [HttpPost]
        public async Task<IActionResult> AddRegistrationRound(int competitionId, DateTime? pendingStartDate, [FromBody] RegistrationRoundCreateDto roundDto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (roundDto == null || string.IsNullOrWhiteSpace(roundDto.RoundName))
                    return Json(new { success = false, message = "Thông tin đợt đăng ký không hợp lệ." });

                var result = await _competitionService.AddRegistrationRoundAsync(competitionId, roundDto, pendingStartDate);
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

        // === CẬP NHẬT ĐỢT ĐĂNG KÝ (API) ===
        [HttpPost]
        public async Task<IActionResult> UpdateRegistrationRound(int roundId, int competitionId, [FromBody] RegistrationRoundUpdateDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.RoundName))
                    return Json(new { success = false, message = "Tên đợt đăng ký không được để trống." });

                var result = await _competitionService.UpdateRegistrationRoundAsync(roundId, competitionId, dto);
                if (!result) return Json(new { success = false, message = "Đợt đăng ký không tìm thấy." });

                return Json(new { success = true, message = "Cập nhật đợt đăng ký thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Validation error updating round {RoundId}: {Message}", roundId, ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating registration round {RoundId}", roundId);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // === XÓA CUỘC THI ===
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _competitionService.DeleteCompetitionAsync(id, GetCurrentUserId());
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
                var result = await _competitionService.ChangeCompetitionStatusAsync(id, status, GetCurrentUserId());
                if (!result) return NotFound();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing competition status {Id}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Chi tiết hồ sơ đăng ký (dùng cho modal, bao gồm custom fields)
        [HttpGet]
        public async Task<IActionResult> GetRegistrationDetail(int registrationId)
        {
            var detail = await _submissionService.GetRegistrationDetailAsync(registrationId);
            if (detail == null) return NotFound();
            return Json(detail);
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
            string? status = null, string? department = null, string? type = null)
        {
            try
            {
                var viewModel = await _submissionService.GetSubmissionsViewAsync(GetCurrentUserId(), competitionId, search, status, department, type);
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
                var result = await _submissionService.ApproveRegistrationAsync(registrationId, GetCurrentUserId(), feedback);
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
                var result = await _submissionService.RejectRegistrationAsync(registrationId, GetCurrentUserId(), reason);
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
                var result = await _submissionService.RequestSupplementAsync(registrationId, GetCurrentUserId(), feedback);
                if (!result) return Json(new { success = false, message = "Hồ sơ không tìm thấy." });
                return Json(new { success = true, message = "Yêu cầu bổ sung đã gửi!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting supplement");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ====== ROUNDS ======

        [HttpPost]
        public async Task<IActionResult> DeleteRound(int roundId, int competitionId)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                var result = await _competitionService.DeleteRegistrationRoundAsync(roundId, competitionId);
                if (!result) return Json(new { success = false, message = "Đợt không tìm thấy." });
                return Json(new { success = true, message = "Đã xóa đợt đăng ký." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting round {RoundId}", roundId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        // ====== COMPETITION ROUNDS (VÒNG THI) ======

        [HttpPost]
        public async Task<IActionResult> AddCompetitionRound(int competitionId, [FromBody] CompetitionRoundCreateDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.RoundName))
                    return Json(new { success = false, message = "Tên vòng thi không được để trống." });

                var result = await _competitionService.AddCompetitionRoundAsync(competitionId, dto);
                if (!result) return Json(new { success = false, message = "Cuộc thi không tìm thấy." });
                return Json(new { success = true, message = "Thêm vòng thi thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding competition round");
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCompetitionRound(int roundId, int competitionId, [FromBody] UpdateCompetitionRoundDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.RoundName))
                    return Json(new { success = false, message = "Tên vòng thi không được để trống." });

                var result = await _competitionService.UpdateCompetitionRoundAsync(roundId, competitionId, dto);
                if (!result) return Json(new { success = false, message = "Vòng thi không tìm thấy." });
                return Json(new { success = true, message = "Cập nhật vòng thi thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating competition round {RoundId}", roundId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCompetitionRound(int roundId, int competitionId)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                var result = await _competitionService.DeleteCompetitionRoundAsync(roundId, competitionId);
                if (!result) return Json(new { success = false, message = "Vòng thi không tìm thấy." });
                return Json(new { success = true, message = "Đã xóa vòng thi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting competition round {RoundId}", roundId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        // ====== IMAGES ======

        [HttpPost]
        public async Task<IActionResult> UploadImages(int competitionId, [FromBody] List<string> imageDataList)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

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

        // ====== GRADING ======

        public async Task<IActionResult> Grading(int? competitionId)
        {
            try
            {
                var vm = await _gradingService.GetGradingViewAsync(GetCurrentUserId(), competitionId);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading grading page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chấm điểm.";
                return View(new OrganizerGradingViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsersForJudge(int competitionId, string search, int? roundId = null)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
                return Json(new List<object>());
            var results = await _gradingService.SearchUsersForJudgeAsync(competitionId, search, roundId);
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsersForJudgePool(string search)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
                return Json(new List<object>());
            var results = await _gradingService.SearchUsersForJudgePoolAsync(search);
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> SearchJudgesForRound(int competitionId, int roundId, string search)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
                return Json(new List<object>());
            var results = await _gradingService.SearchJudgesForRoundAsync(competitionId, roundId, search);
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> CheckJudgeConflict(int judgeUserId, int roundId)
        {
            var conflicts = await _gradingService.CheckTimeConflictsAsync(judgeUserId, roundId);
            return Json(conflicts);
        }

        [HttpPost]
        public async Task<IActionResult> AddUserToJudgePool([FromBody] AddToJudgePoolDto dto)
        {
            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                var (ok, msg) = await _gradingService.AddUserToJudgePoolAsync(dto.UserId);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to judge pool", dto?.UserId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUserFromJudgePool(int userId)
        {
            try
            {
                var (ok, msg) = await _gradingService.RemoveUserFromJudgePoolAsync(userId);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from judge pool", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddJudge(int competitionId, [FromBody] AddJudgeDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                var (ok, msg) = await _gradingService.AddJudgeAsync(competitionId, dto);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding judge to competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveJudge(int judgeId, int competitionId)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                var (ok, msg) = await _gradingService.RemoveJudgeAsync(judgeId, competitionId);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing judge {JudgeId}", judgeId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateJudgeRole(int judgeId, int competitionId, [FromBody] UpdateJudgeRoleDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                var (ok, msg) = await _gradingService.UpdateJudgeRoleAsync(judgeId, competitionId, dto);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating judge role {JudgeId}", judgeId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignSubmissions(int competitionId, [FromBody] AssignSubmissionsDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null || !dto.SubmissionIds.Any())
                    return Json(new { success = false, message = "Chưa chọn bài thi nào." });
                var (ok, msg) = await _gradingService.AssignSubmissionsAsync(competitionId, dto, GetCurrentUserId());
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning submissions for competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RevokeAssignment(int assignmentId, int competitionId)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                var (ok, msg) = await _gradingService.RevokeAssignmentAsync(assignmentId, competitionId);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking assignment {AssignmentId}", assignmentId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public IActionResult SendJudgeReminder(int judgeId)
        {
            _logger.LogInformation("Reminder sent for judge {JudgeId}", judgeId);
            return Json(new { success = true, message = "Đã gửi nhắc nhở đến giám khảo." });
        }

        public async Task<IActionResult> Results(int? competitionId = null)
        {
            try
            {
                var vm = await _resultsService.GetResultsViewAsync(GetCurrentUserId(), competitionId);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading results page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang kết quả.";
                return View(new ResultsPageViewModel());
            }
        }

        // === CÔNG BỐ / ẨN KẾT QUẢ THEO VÒNG THI (trang công khai /Results) ===
        [HttpPost]
        public async Task<IActionResult> PublishRoundResults(int roundId, int competitionId, bool publish)
        {
            if (!await _competitionService.IsCompetitionOwnerAsync(competitionId, GetCurrentUserId()))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction("Results", new { competitionId });
            }

            try
            {
                var (ok, msg) = await _resultsService.SetRoundResultsPublishedAsync(roundId, competitionId, publish);
                TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling results publish for round {RoundId}", roundId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            }

            return RedirectToAction("Results", new { competitionId });
        }

        // Gom logic kiểm tra quyền + tải dữ liệu dùng chung cho cả 3 định dạng xuất (CSV/Excel/PDF)
        private async Task<(CompetitionResultDetailDto? Detail, List<RoundResultDetailDto> Rounds, IActionResult? Error)>
            LoadExportDataAsync(int competitionId, int? roundId)
        {
            if (!await _competitionService.IsCompetitionOwnerAsync(competitionId, GetCurrentUserId()))
                return (null, new(), Forbid());

            var vm = await _resultsService.GetResultsViewAsync(GetCurrentUserId(), competitionId);
            var detail = vm.SelectedDetail;
            if (detail == null) return (null, new(), NotFound());

            var rounds = roundId.HasValue
                ? detail.Rounds.Where(r => r.RoundId == roundId).ToList()
                : detail.Rounds;

            return (detail, rounds, null);
        }

        private static string BuildExportFileName(string competitionName, string extension)
        {
            var slug = string.Concat(competitionName.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            return $"ketqua_{slug}_{DateTime.Now:yyyyMMdd}.{extension}";
        }

        // Excel sheet name: tối đa 31 ký tự, không chứa \ / ? * [ ] : , và phải là duy nhất trong workbook
        private static string SanitizeSheetName(string name, HashSet<string> usedNames)
        {
            var invalid = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            var cleaned = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "VongThi";
            if (cleaned.Length > 28) cleaned = cleaned[..28];

            var candidate = cleaned;
            int suffix = 2;
            while (!usedNames.Add(candidate))
                candidate = $"{cleaned}_{suffix++}";

            return candidate;
        }

        private static List<string> BuildExportHeaders(RoundResultDetailDto round)
        {
            var headers = new List<string> { "Hạng", "Giải", "Thí sinh/Đội", "Mã SV", "Tên đề tài" };
            headers.AddRange(round.Criteria.Select(c => $"{c.CriteriaName} (/{c.MaxScore})"));
            headers.AddRange(new[] { "Tổng điểm", "% Điểm", "Số giám khảo" });
            return headers;
        }

        // === XUẤT CSV (dùng dấu ; để tương thích Excel bản Việt hoá — mặc định coi ; là dấu phân tách cột) ===
        [HttpGet]
        public async Task<IActionResult> ExportResultsCsv(int competitionId, int? roundId = null)
        {
            try
            {
                var (detail, rounds, error) = await LoadExportDataAsync(competitionId, roundId);
                if (error != null) return error;

                var sb = new StringBuilder();
                sb.AppendLine($"Báo cáo kết quả: {detail!.CompetitionName}");
                sb.AppendLine($"Xuất ngày: {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine();

                foreach (var round in rounds)
                {
                    sb.AppendLine($"Vòng thi: {round.RoundName}");
                    var headers = BuildExportHeaders(round);
                    sb.AppendLine(string.Join(";", headers.Select(h => $"\"{h}\"")));

                    foreach (var r in round.Rankings)
                    {
                        var row = new List<string>
                        {
                            r.Rank.ToString(),
                            string.IsNullOrEmpty(r.AwardLevel) ? "-" : $"Giải {r.AwardLevel}",
                            r.ParticipantName,
                            r.StudentId ?? "",
                            r.Title
                        };
                        foreach (var c in round.Criteria)
                        {
                            row.Add(r.CriteriaScores.TryGetValue(c.CriteriaId, out var s) ? s.ToString("F1") : "0");
                        }
                        row.Add(r.TotalScore.ToString("F1"));
                        row.Add($"{r.ScorePercentage}%");
                        row.Add(r.JudgeCount.ToString());
                        sb.AppendLine(string.Join(";", row.Select(v => $"\"{v}\"")));
                    }
                    sb.AppendLine();
                }

                var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                return File(bytes, "text/csv; charset=utf-8", BuildExportFileName(detail.CompetitionName, "csv"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting CSV for competition {Id}", competitionId);
                return StatusCode(500, "Có lỗi xảy ra khi xuất dữ liệu CSV.");
            }
        }

        // === XUẤT EXCEL (.xlsx) — mỗi vòng thi 1 sheet, có định dạng, tô màu top 3, freeze header ===
        [HttpGet]
        public async Task<IActionResult> ExportResultsExcel(int competitionId, int? roundId = null)
        {
            try
            {
                var (detail, rounds, error) = await LoadExportDataAsync(competitionId, roundId);
                if (error != null) return error;

                using var workbook = new XLWorkbook();
                var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Sheet tổng hợp khi xuất từ 2 vòng trở lên
                if (rounds.Count > 1)
                {
                    var overview = workbook.Worksheets.Add(SanitizeSheetName("Tong hop", usedSheetNames));
                    overview.Cell(1, 1).Value = $"Báo cáo kết quả: {detail!.CompetitionName}";
                    overview.Cell(1, 1).Style.Font.Bold = true;
                    overview.Cell(1, 1).Style.Font.FontSize = 14;
                    overview.Cell(2, 1).Value = $"Xuất ngày: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    overview.Cell(2, 1).Style.Font.Italic = true;

                    string[] ovHeaders = { "Vòng thi", "Số bài", "Đã chấm", "Điểm TB", "Số giải" };
                    for (int i = 0; i < ovHeaders.Length; i++)
                        StyleHeaderCell(overview.Cell(4, i + 1), ovHeaders[i]);

                    int ovRow = 5;
                    foreach (var round in rounds)
                    {
                        overview.Cell(ovRow, 1).Value = round.RoundName;
                        overview.Cell(ovRow, 2).Value = round.SubmissionCount;
                        overview.Cell(ovRow, 3).Value = round.EvaluatedCount;
                        overview.Cell(ovRow, 4).Value = round.AverageScore;
                        overview.Cell(ovRow, 5).Value = round.Rankings.Count(r => !string.IsNullOrEmpty(r.AwardLevel));
                        ovRow++;
                    }
                    if (ovRow > 5)
                        overview.Range(4, 1, ovRow - 1, ovHeaders.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    overview.Columns().AdjustToContents();
                }

                foreach (var round in rounds)
                {
                    var ws = workbook.Worksheets.Add(SanitizeSheetName(round.RoundName, usedSheetNames));

                    ws.Cell(1, 1).Value = $"Báo cáo kết quả: {detail!.CompetitionName}";
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 14;

                    ws.Cell(2, 1).Value = $"Vòng thi: {round.RoundName}    ·    Xuất ngày: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    ws.Cell(2, 1).Style.Font.Italic = true;

                    var headers = BuildExportHeaders(round);
                    const int headerRow = 4;
                    for (int i = 0; i < headers.Count; i++)
                        StyleHeaderCell(ws.Cell(headerRow, i + 1), headers[i]);

                    int row = headerRow + 1;
                    foreach (var r in round.Rankings)
                    {
                        int col = 1;
                        ws.Cell(row, col++).Value = r.Rank;
                        ws.Cell(row, col++).Value = string.IsNullOrEmpty(r.AwardLevel) ? "-" : $"Giải {r.AwardLevel}";
                        ws.Cell(row, col++).Value = r.ParticipantName;
                        ws.Cell(row, col++).Value = r.StudentId ?? "";
                        ws.Cell(row, col++).Value = r.Title;
                        foreach (var c in round.Criteria)
                        {
                            var score = r.CriteriaScores.TryGetValue(c.CriteriaId, out var s) ? s : 0;
                            ws.Cell(row, col).Value = score;
                            ws.Cell(row, col).Style.NumberFormat.Format = "0.0";
                            col++;
                        }
                        ws.Cell(row, col).Value = r.TotalScore;
                        ws.Cell(row, col).Style.NumberFormat.Format = "0.0";
                        ws.Cell(row, col).Style.Font.Bold = true;
                        col++;
                        ws.Cell(row, col).Value = r.ScorePercentage / 100m;
                        ws.Cell(row, col).Style.NumberFormat.Format = "0.0%";
                        col++;
                        ws.Cell(row, col++).Value = r.JudgeCount;

                        if (r.Rank <= 3)
                            ws.Range(row, 1, row, headers.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7E0");

                        row++;
                    }

                    if (round.Rankings.Any())
                    {
                        var dataRange = ws.Range(headerRow, 1, row - 1, headers.Count);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                        ws.Range(headerRow + 1, 1, row - 1, headers.Count).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // Thí sinh/Đội
                        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // Tên đề tài
                    }
                    else
                    {
                        ws.Cell(headerRow + 1, 1).Value = "Vòng thi này chưa có bài được chấm điểm.";
                        ws.Cell(headerRow + 1, 1).Style.Font.Italic = true;
                    }

                    ws.SheetView.FreezeRows(headerRow);
                    ws.Columns().AdjustToContents();
                    if (ws.Column(5).Width > 45) ws.Column(5).Width = 45; // giới hạn độ rộng cột "Tên đề tài"
                }

                if (workbook.Worksheets.Count == 0)
                    workbook.Worksheets.Add("Kết quả");

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    BuildExportFileName(detail!.CompetitionName, "xlsx"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel for competition {Id}", competitionId);
                return StatusCode(500, "Có lỗi xảy ra khi xuất dữ liệu Excel.");
            }
        }

        private static void StyleHeaderCell(IXLCell cell, string text)
        {
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // === XUẤT PDF — mỗi vòng thi 1 trang (khổ ngang), bảng có định dạng, tô màu top 3 ===
        [HttpGet]
        public async Task<IActionResult> ExportResultsPdf(int competitionId, int? roundId = null)
        {
            try
            {
                var (detail, rounds, error) = await LoadExportDataAsync(competitionId, roundId);
                if (error != null) return error;

                var exportedAt = DateTime.Now;
                var document = Document.Create(container =>
                {
                    if (!rounds.Any())
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(30);
                            page.DefaultTextStyle(x => x.FontFamily("Arial"));
                            page.Content().AlignCenter().AlignMiddle()
                                .Text("Chưa có dữ liệu để xuất.").FontSize(14);
                        });
                    }

                    foreach (var round in rounds)
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4.Landscape());
                            page.Margin(25);
                            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                            page.Header().Column(col =>
                            {
                                col.Item().Text(detail!.CompetitionName).FontSize(15).Bold();
                                col.Item().Text($"Vòng thi: {round.RoundName}    ·    Xuất ngày: {exportedAt:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                                col.Item().PaddingTop(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
                            });

                            page.Content().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(32);   // Hạng
                                    columns.ConstantColumn(55);   // Giải
                                    columns.RelativeColumn(2.2f); // Thí sinh/Đội
                                    columns.ConstantColumn(60);   // MSSV
                                    columns.RelativeColumn(2.6f); // Tên đề tài
                                    foreach (var _ in round.Criteria) columns.ConstantColumn(55);
                                    columns.ConstantColumn(55); // Tổng điểm
                                    columns.ConstantColumn(45); // % điểm
                                    columns.ConstantColumn(28); // GK
                                });

                                var headers = BuildExportHeaders(round);
                                table.Header(header =>
                                {
                                    foreach (var h in headers)
                                    {
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(4)
                                            .Text(h).FontColor(Colors.White).Bold().FontSize(8);
                                    }
                                });

                                foreach (var r in round.Rankings)
                                {
                                    var bg = r.Rank <= 3 ? Colors.Amber.Lighten4 : Colors.White;
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text(r.Rank.ToString());
                                    table.Cell().Background(bg).Padding(4).AlignCenter()
                                        .Text(string.IsNullOrEmpty(r.AwardLevel) ? "-" : $"Giải {r.AwardLevel}");
                                    table.Cell().Background(bg).Padding(4).Text(r.ParticipantName);
                                    table.Cell().Background(bg).Padding(4).Text(r.StudentId ?? "");
                                    table.Cell().Background(bg).Padding(4).Text(r.Title);
                                    foreach (var c in round.Criteria)
                                    {
                                        var score = r.CriteriaScores.TryGetValue(c.CriteriaId, out var s) ? s : 0;
                                        table.Cell().Background(bg).Padding(4).AlignCenter().Text(score.ToString("F1"));
                                    }
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text(r.TotalScore.ToString("F1")).Bold();
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text($"{r.ScorePercentage}%");
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text(r.JudgeCount.ToString());
                                }

                                if (!round.Rankings.Any())
                                {
                                    table.Cell().ColumnSpan((uint)headers.Count).Padding(10).AlignCenter()
                                        .Text("Vòng thi này chưa có bài được chấm điểm.").Italic().FontColor(Colors.Grey.Darken1);
                                }
                            });

                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.Span("Trang ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    }
                });

                var bytes = document.GeneratePdf();
                return File(bytes, "application/pdf", BuildExportFileName(detail!.CompetitionName, "pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PDF for competition {Id}", competitionId);
                return StatusCode(500, "Có lỗi xảy ra khi xuất dữ liệu PDF.");
            }
        }

        // ====== THỐNG KÊ & BÁO CÁO ======

        public async Task<IActionResult> Statistics(int? competitionId = null)
        {
            try
            {
                var vm = await _resultsService.GetOrganizerStatisticsAsync(GetCurrentUserId(), competitionId);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statistics page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang thống kê.";
                return View(new OrganizerStatisticsViewModel());
            }
        }

        private static string BuildStatisticsFileName(string extension) => $"thongke_baocao_{DateTime.Now:yyyyMMdd}.{extension}";

        [HttpGet]
        public async Task<IActionResult> ExportStatisticsExcel(int? competitionId = null)
        {
            try
            {
                var vm = await _resultsService.GetOrganizerStatisticsAsync(GetCurrentUserId(), competitionId);

                using var workbook = new XLWorkbook();
                var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Sheet 1: Tổng quan
                var ov = workbook.Worksheets.Add(SanitizeSheetName("Tong quan", usedSheetNames));
                ov.Cell(1, 1).Value = "BÁO CÁO THỐNG KÊ TỔ CHỨC CUỘC THI";
                ov.Cell(1, 1).Style.Font.Bold = true;
                ov.Cell(1, 1).Style.Font.FontSize = 14;
                ov.Cell(2, 1).Value = $"Xuất ngày: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ov.Cell(2, 1).Style.Font.Italic = true;

                var kpis = new (string Label, string Value)[]
                {
                    ("Tổng số cuộc thi", vm.TotalCompetitions.ToString()),
                    ("Cuộc thi đang diễn ra", vm.ActiveCompetitions.ToString()),
                    ("Cuộc thi đã hoàn thành", vm.CompletedCompetitions.ToString()),
                    ("Tổng thí sinh đăng ký", vm.TotalRegistrations.ToString()),
                    ("Hồ sơ đã duyệt", vm.ApprovedRegistrations.ToString()),
                    ("Tổng bài dự thi", vm.TotalSubmissions.ToString()),
                    ("Bài đã chấm điểm", vm.EvaluatedSubmissions.ToString()),
                    ("Tỷ lệ hoàn thành chấm điểm", $"{vm.GradingCompletionRate}%"),
                    ("Tổng số lượt đạt giải", vm.TotalAwardedEntries.ToString())
                };
                int kr = 4;
                foreach (var (label, value) in kpis)
                {
                    ov.Cell(kr, 1).Value = label;
                    ov.Cell(kr, 1).Style.Font.Bold = true;
                    ov.Cell(kr, 2).Value = value;
                    kr++;
                }
                ov.Columns().AdjustToContents();

                // Sheet 2: Kết quả chấm điểm theo vòng
                var rs = workbook.Worksheets.Add(SanitizeSheetName("Ket qua theo vong", usedSheetNames));
                string[] rHeaders = { "Cuộc thi", "Vòng thi", "Công bố", "Bài nộp", "Đã chấm", "Tiến độ", "Điểm TB", "Điểm tối đa", "Số giải" };
                for (int i = 0; i < rHeaders.Length; i++) StyleHeaderCell(rs.Cell(1, i + 1), rHeaders[i]);
                int rr = 2;
                foreach (var row in vm.RoundResults)
                {
                    rs.Cell(rr, 1).Value = row.CompetitionName;
                    rs.Cell(rr, 2).Value = row.RoundName;
                    rs.Cell(rr, 3).Value = row.IsResultsPublished ? "Đã công bố" : "Chưa công bố";
                    rs.Cell(rr, 4).Value = row.SubmissionCount;
                    rs.Cell(rr, 5).Value = row.EvaluatedCount;
                    rs.Cell(rr, 6).Value = row.GradingProgress / 100.0;
                    rs.Cell(rr, 6).Style.NumberFormat.Format = "0.0%";
                    rs.Cell(rr, 7).Value = row.AverageScore;
                    rs.Cell(rr, 7).Style.NumberFormat.Format = "0.0";
                    rs.Cell(rr, 8).Value = row.MaxPossibleScore;
                    rs.Cell(rr, 9).Value = row.AwardedCount;
                    rr++;
                }
                if (rr > 2)
                {
                    rs.Range(1, 1, rr - 1, rHeaders.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rs.SheetView.FreezeRows(1);
                }
                rs.Columns().AdjustToContents();

                // Sheet 3: Danh sách đạt giải
                var aw = workbook.Worksheets.Add(SanitizeSheetName("Danh sach dat giai", usedSheetNames));
                string[] aHeaders = { "Hạng", "Giải", "Cuộc thi", "Vòng thi", "Thí sinh/Đội", "MSSV", "Đề tài", "Tổng điểm", "% điểm" };
                for (int i = 0; i < aHeaders.Length; i++) StyleHeaderCell(aw.Cell(1, i + 1), aHeaders[i]);
                int ar = 2;
                foreach (var row in vm.AwardWinners)
                {
                    aw.Cell(ar, 1).Value = row.Rank;
                    aw.Cell(ar, 2).Value = $"Giải {row.AwardLevel}";
                    aw.Cell(ar, 3).Value = row.CompetitionName;
                    aw.Cell(ar, 4).Value = row.RoundName;
                    aw.Cell(ar, 5).Value = row.ParticipantName;
                    aw.Cell(ar, 6).Value = row.StudentId ?? "";
                    aw.Cell(ar, 7).Value = row.Title;
                    aw.Cell(ar, 8).Value = row.TotalScore;
                    aw.Cell(ar, 8).Style.NumberFormat.Format = "0.0";
                    aw.Cell(ar, 9).Value = row.ScorePercentage / 100m;
                    aw.Cell(ar, 9).Style.NumberFormat.Format = "0.0%";
                    if (row.Rank <= 3)
                        aw.Range(ar, 1, ar, aHeaders.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7E0");
                    ar++;
                }
                if (ar > 2)
                {
                    aw.Range(1, 1, ar - 1, aHeaders.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    aw.SheetView.FreezeRows(1);
                }
                aw.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    BuildStatisticsFileName("xlsx"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting statistics Excel");
                return StatusCode(500, "Có lỗi xảy ra khi xuất dữ liệu Excel.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportStatisticsPdf(int? competitionId = null)
        {
            try
            {
                var vm = await _resultsService.GetOrganizerStatisticsAsync(GetCurrentUserId(), competitionId);
                var exportedAt = DateTime.Now;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                        page.Header().Column(col =>
                        {
                            col.Item().Text("BÁO CÁO THỐNG KÊ TỔ CHỨC CUỘC THI").FontSize(16).Bold();
                            col.Item().Text($"Xuất ngày: {exportedAt:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
                        });

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Spacing(14);

                            col.Item().Text("Tổng quan").FontSize(12).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                                void Kpi(string label, string value)
                                {
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(kc =>
                                    {
                                        kc.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                                        kc.Item().Text(value).FontSize(14).Bold();
                                    });
                                }

                                Kpi("Tổng cuộc thi", vm.TotalCompetitions.ToString());
                                Kpi("Đang diễn ra", vm.ActiveCompetitions.ToString());
                                Kpi("Đã hoàn thành", vm.CompletedCompetitions.ToString());
                                Kpi("Thí sinh đăng ký", vm.TotalRegistrations.ToString());
                                Kpi("Bài dự thi", vm.TotalSubmissions.ToString());
                                Kpi("Đã chấm điểm", $"{vm.EvaluatedSubmissions} ({vm.GradingCompletionRate}%)");
                            });

                            col.Item().PaddingTop(6).Text("Kết quả chấm điểm theo vòng thi").FontSize(12).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2.4f); c.RelativeColumn(1.6f); c.ConstantColumn(60);
                                    c.ConstantColumn(50); c.ConstantColumn(50); c.ConstantColumn(55); c.ConstantColumn(45);
                                });
                                string[] headers = { "Cuộc thi", "Vòng thi", "Công bố", "Bài nộp", "Đã chấm", "Điểm TB", "Số giải" };
                                table.Header(h =>
                                {
                                    foreach (var hd in headers)
                                        h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(hd).FontColor(Colors.White).Bold().FontSize(8);
                                });
                                foreach (var row in vm.RoundResults)
                                {
                                    table.Cell().Padding(4).Text(row.CompetitionName);
                                    table.Cell().Padding(4).Text(row.RoundName);
                                    table.Cell().Padding(4).AlignCenter().Text(row.IsResultsPublished ? "Có" : "Chưa");
                                    table.Cell().Padding(4).AlignCenter().Text(row.SubmissionCount.ToString());
                                    table.Cell().Padding(4).AlignCenter().Text(row.EvaluatedCount.ToString());
                                    table.Cell().Padding(4).AlignCenter().Text(row.AverageScore.ToString("F1"));
                                    table.Cell().Padding(4).AlignCenter().Text(row.AwardedCount.ToString());
                                }
                                if (!vm.RoundResults.Any())
                                    table.Cell().ColumnSpan(7).Padding(10).AlignCenter().Text("Chưa có dữ liệu.").Italic();
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Trang "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages();
                        });
                    });

                    // Trang riêng, khổ ngang: Danh sách đạt giải
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(25);
                        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                        page.Header().Column(col =>
                        {
                            col.Item().Text("DANH SÁCH ĐẠT GIẢI").FontSize(15).Bold();
                            col.Item().Text($"Xuất ngày: {exportedAt:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
                        });

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(32); c.ConstantColumn(70); c.RelativeColumn(2f); c.RelativeColumn(1.3f);
                                c.RelativeColumn(1.8f); c.ConstantColumn(70); c.RelativeColumn(2f); c.ConstantColumn(55); c.ConstantColumn(45);
                            });
                            string[] headers = { "#", "Giải", "Cuộc thi", "Vòng thi", "Thí sinh/Đội", "MSSV", "Đề tài", "Điểm", "%" };
                            table.Header(h =>
                            {
                                foreach (var hd in headers)
                                    h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(hd).FontColor(Colors.White).Bold().FontSize(8);
                            });

                            foreach (var row in vm.AwardWinners)
                            {
                                var bg = row.Rank <= 3 ? Colors.Amber.Lighten4 : Colors.White;
                                table.Cell().Background(bg).Padding(4).AlignCenter().Text(row.Rank.ToString());
                                table.Cell().Background(bg).Padding(4).AlignCenter().Text($"Giải {row.AwardLevel}");
                                table.Cell().Background(bg).Padding(4).Text(row.CompetitionName);
                                table.Cell().Background(bg).Padding(4).Text(row.RoundName);
                                table.Cell().Background(bg).Padding(4).Text(row.ParticipantName);
                                table.Cell().Background(bg).Padding(4).Text(row.StudentId ?? "");
                                table.Cell().Background(bg).Padding(4).Text(row.Title);
                                table.Cell().Background(bg).Padding(4).AlignCenter().Text(row.TotalScore.ToString("F1")).Bold();
                                table.Cell().Background(bg).Padding(4).AlignCenter().Text($"{row.ScorePercentage}%");
                            }

                            if (!vm.AwardWinners.Any())
                                table.Cell().ColumnSpan(9).Padding(10).AlignCenter().Text("Chưa có thí sinh đạt giải.").Italic();
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Trang "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages();
                        });
                    });
                });

                var bytes = document.GeneratePdf();
                return File(bytes, "application/pdf", BuildStatisticsFileName("pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting statistics PDF");
                return StatusCode(500, "Có lỗi xảy ra khi xuất dữ liệu PDF.");
            }
        }

        public async Task<IActionResult> Notifications(int? competitionId = null)
        {
            try
            {
                var vm = await _notificationsService.GetOrganizerViewAsync(GetCurrentUserId(), competitionId);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang thông báo.";
                return View(new OrganizerNotificationsViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest req)
        {
            try
            {
                var (ok, msg, count) = await _notificationsService.SendNotificationAsync(req, GetCurrentUserId());
                return Json(new { ok, message = msg, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return Json(new { ok = false, message = "Lỗi hệ thống khi gửi thông báo." });
            }
        }

        [HttpPost]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
        public async Task<IActionResult> CreateAnnouncement(
            [FromForm] string title,
            [FromForm] string content,
            [FromForm] string type,
            [FromForm] int? relatedCompetitionId,
            IFormFile? imageFile,
            IFormFile? attachmentFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                    return Json(new { ok = false, message = "Tiêu đề và nội dung không được để trống." });

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "announcements");
                Directory.CreateDirectory(uploadsDir);

                string? imageUrl = null;
                if (imageFile != null && imageFile.Length > 0)
                {
                    var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    var allowedImg = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    if (!allowedImg.Contains(ext))
                        return Json(new { ok = false, message = "Chỉ chấp nhận ảnh JPG, PNG, GIF, WEBP." });

                    var imgName = $"img_{Guid.NewGuid():N}{ext}";
                    var imgPath = Path.Combine(uploadsDir, imgName);
                    await using var fs = System.IO.File.Create(imgPath);
                    await imageFile.CopyToAsync(fs);
                    imageUrl = $"/uploads/announcements/{imgName}";
                }

                string? attachUrl = null;
                string? attachName = null;
                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    var ext = Path.GetExtension(attachmentFile.FileName).ToLowerInvariant();
                    var allowedFile = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
                    if (!allowedFile.Contains(ext))
                        return Json(new { ok = false, message = "Chỉ chấp nhận file PDF, Word, Excel." });

                    var fileName = $"attach_{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    await using var fs = System.IO.File.Create(filePath);
                    await attachmentFile.CopyToAsync(fs);
                    attachUrl  = $"/uploads/announcements/{fileName}";
                    attachName = attachmentFile.FileName;
                }

                var announcement = new Web_cham_diem.Models.PublicAnnouncements
                {
                    Title                = title.Trim(),
                    Content              = content.Trim(),
                    Type                 = type ?? "Info",
                    ImageUrl             = imageUrl,
                    AttachmentUrl        = attachUrl,
                    AttachmentFileName   = attachName,
                    RelatedCompetitionId = relatedCompetitionId,
                    CreatedByUserId      = GetCurrentUserId(),
                    CreatedAt            = DateTime.UtcNow,
                    IsPublished          = true
                };

                var id = await _notificationsService.CreateAnnouncementAsync(announcement);
                return Json(new { ok = true, message = "Đã đăng thông báo thành công.", id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating announcement");
                return Json(new { ok = false, message = "Lỗi hệ thống khi tạo thông báo." });
            }
        }

        public IActionResult Progress() => View();

        // ====== REGISTRATION FORM FIELDS ======

        [HttpGet]
        public async Task<IActionResult> GetFormFields(int competitionId)
        {
            var fields = await _competitionService.GetFormFieldsAsync(competitionId);
            return Json(fields);
        }

        [HttpPost]
        public async Task<IActionResult> AddFormField(int competitionId, [FromBody] FormFieldCreateDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                var (ok, msg, fieldId) = await _competitionService.AddFormFieldAsync(competitionId, dto);
                return Json(new { success = ok, message = msg, fieldId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding form field to competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFormField(int fieldId, int competitionId, [FromBody] FormFieldCreateDto dto)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                var (ok, msg) = await _competitionService.UpdateFormFieldAsync(fieldId, competitionId, dto);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form field {FieldId}", fieldId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFormField(int fieldId, int competitionId)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                var (ok, msg) = await _competitionService.DeleteFormFieldAsync(fieldId, competitionId);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting form field {FieldId}", fieldId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReorderFormFields(int competitionId, [FromBody] List<int> orderedIds)
        {
            var guard = await RequireOwnerJson(competitionId);
            if (guard != null) return guard;

            try
            {
                if (orderedIds == null || !orderedIds.Any())
                    return Json(new { success = false, message = "Danh sách trống." });
                var (ok, msg) = await _competitionService.ReorderFormFieldsAsync(competitionId, orderedIds);
                return Json(new { success = ok, message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering form fields for competition {Id}", competitionId);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }
    }

    // Request DTO cho upload documents
    public class UploadDocumentsRequest
    {
        public List<string> DataList { get; set; } = new();
        public List<string> FileNames { get; set; } = new();
    }
}
