using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Data;
using PlayTogether.Web.Models;

namespace PlayTogether.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ProfileController(UserManager<AppUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = await _context.Users
            .Where(u => u.Id == user.Id)
            .Select(u => new AppUser
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Rating = u.Rating,
                GamesPlayed = u.GamesPlayed,
                Wins = u.Wins,
                Losses = u.Losses,
                MVPCount = u.MVPCount,
                JoinedAt = u.JoinedAt
            })
            .FirstAsync();

        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Leaderboard()
    {
        var players = await _context.Users
            .OrderByDescending(u => u.Rating)
            .ThenByDescending(u => u.Wins)
            .ToListAsync();

        return View(players);
    }
}
