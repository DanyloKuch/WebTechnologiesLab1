using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebTechnologiesLab1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace WebTechnologiesLab1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> PremiumPage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && user.IsPremiumUser)
            {
                return View();
            }
            else
            {
                return Forbid();
            }
        }

    }
}
