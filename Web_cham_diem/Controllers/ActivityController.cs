using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web_cham_diem.Controllers
{
    [Authorize]
    public class ActivityController : Controller
    {
        // Đường dẫn: /Activity/Competitions -> Trả về Views/Activity/Competitions.cshtml
        public IActionResult Competitions()
        {
            return View();
        }

        // Đường dẫn: /Activity/Teams -> Trả về Views/Activity/Teams.cshtml
        public IActionResult Teams()
        {
            return View();
        }

        // Đường dẫn: /Activity/Submissions -> Trả về Views/Activity/Submissions.cshtml
        public IActionResult Submissions()
        {
            return View();
        }

        // Đường dẫn: /Activity/Scores -> Trả về Views/Activity/Scores.cshtml
        public IActionResult Scores()
        {
            return View();
        }
    }
}