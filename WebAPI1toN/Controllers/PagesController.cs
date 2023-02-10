using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebAPI1toN.Models;

namespace WebAPI1toN.Controllers
{
    public class PagesController : Controller
    {
        private readonly ILogger<PagesController> _logger;

        public PagesController(ILogger<PagesController> logger)
        {
            _logger = logger;
        }

        public IActionResult Home()
        {
            return View();
        }

        public IActionResult Downloads()
        {
            return View();
        }

        public IActionResult SingleScan()
        {
            return View();
        }

        public IActionResult AdvancedScan()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}