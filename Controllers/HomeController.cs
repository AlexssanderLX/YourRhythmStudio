using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using YourRhythmStudio.Infrastructure.Data;
using YourRhythmStudio.Models;

namespace YourRhythmStudio.Controllers
{
    public class HomeController : Controller
    {
        private readonly YourRhythmDbContext _db;

        public HomeController(YourRhythmDbContext db) => _db = db;

        public IActionResult Index()
        {
            return View();
        }

        // Retorna lista de tracks para o player da home page
        [HttpGet("/api/landing/tracks")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> LandingTracks()
        {
            var tracks = await _db.LandingTracks.AsNoTracking()
                .OrderBy(t => t.SortOrder).ThenBy(t => t.UploadedAtUtc)
                .Select(t => new { id = t.Id, title = t.Title, url = $"/audio/landing-soundtrack/{t.FileName}" })
                .ToListAsync();
            return Ok(tracks);
        }

        [Route("privacidade")]
        public IActionResult Privacy()
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
