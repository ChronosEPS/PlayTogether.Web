using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Data;
using PlayTogether.Web.Models;
using PlayTogether.Web.Models.ViewModels;

namespace PlayTogether.Web.Controllers;

public class MatchesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public MatchesController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var matches = await _context.Matches
            .Include(m => m.MatchPlayers)
            .OrderBy(m => m.DateTime)
            .ToListAsync();

        return View(matches);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Match { DateTime = DateTime.UtcNow.AddDays(1) });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Match model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        model.OrganizerId = userId;
        model.Status = MatchStatus.Open;

        _context.Matches.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Match created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Join(int id)
    {
        var match = await _context.Matches.FindAsync(id);
        if (match is null)
        {
            return NotFound();
        }

        var vm = new JoinMatchViewModel
        {
            MatchId = match.Id,
            MatchTitle = match.Title,
            SportType = match.SportType,
            AvailableRoles = GetRolesForSport(match.SportType)
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinMatchViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.Roles)
            .FirstOrDefaultAsync(m => m.Id == model.MatchId);

        if (match is null)
        {
            return NotFound();
        }

        model.AvailableRoles = GetRolesForSport(match.SportType);
        model.MatchTitle = match.Title;
        model.SportType = match.SportType;

        if (model.SelectedRoles.Count is < 2 or > 3)
        {
            ModelState.AddModelError(string.Empty, "Select 2 or 3 roles.");
            return View(model);
        }

        if (match.Status != MatchStatus.Open)
        {
            TempData["Error"] = "Match is not open.";
            return RedirectToAction(nameof(Index));
        }

        if (match.MatchPlayers.Any(x => x.UserId == userId))
        {
            TempData["Error"] = "You already joined this match.";
            return RedirectToAction(nameof(Index));
        }

        if (match.MatchPlayers.Count >= match.MaxPlayers)
        {
            TempData["Error"] = "Match is full.";
            return RedirectToAction(nameof(Index));
        }

        var matchPlayer = new MatchPlayer
        {
            MatchId = match.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Roles = model.SelectedRoles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(r => new MatchPlayerRole { RoleName = r })
                .ToList()
        };

        _context.MatchPlayers.Add(matchPlayer);
        await _context.SaveChangesAsync();

        TempData["Success"] = "You joined the match.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var mp = await _context.MatchPlayers
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.MatchId == id && x.UserId == userId);

        if (mp is null)
        {
            TempData["Error"] = "You are not in this match.";
            return RedirectToAction(nameof(Index));
        }

        _context.MatchPlayerRoles.RemoveRange(mp.Roles);
        _context.MatchPlayers.Remove(mp);
        await _context.SaveChangesAsync();

        TempData["Success"] = "You left the match.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Finish(int id)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null)
        {
            return NotFound();
        }

        if (!CanManage(match))
        {
            return Forbid();
        }

        var vm = new FinishMatchViewModel
        {
            MatchId = match.Id,
            Players = match.MatchPlayers
                .Select(mp => new PlayerTeamItem
                {
                    UserId = mp.UserId,
                    DisplayName = string.IsNullOrWhiteSpace(mp.User.DisplayName) ? mp.User.UserName ?? mp.User.Email ?? "Player" : mp.User.DisplayName,
                    Team = string.IsNullOrWhiteSpace(mp.Team) ? "A" : mp.Team,
                    IsMVP = mp.IsMVP
                })
                .ToList()
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(FinishMatchViewModel model)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == model.MatchId);

        if (match is null)
        {
            return NotFound();
        }

        if (!CanManage(match))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        match.Status = MatchStatus.Finished;
        match.WinningTeam = model.WinningTeam;

        var playersByUserId = model.Players.ToDictionary(p => p.UserId, StringComparer.OrdinalIgnoreCase);
        foreach (var mp in match.MatchPlayers)
        {
            if (playersByUserId.TryGetValue(mp.UserId, out var player))
            {
                mp.Team = player.Team;
                mp.IsMVP = player.IsMVP;
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Match finished.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.Roles)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null)
        {
            return NotFound();
        }

        if (!CanManage(match))
        {
            return Forbid();
        }

        _context.MatchPlayerRoles.RemoveRange(match.MatchPlayers.SelectMany(x => x.Roles));
        _context.MatchPlayers.RemoveRange(match.MatchPlayers);
        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Match deleted.";
        return RedirectToAction(nameof(Index));
    }

    private bool CanManage(Match match)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId) && (match.OrganizerId == userId || User.IsInRole("Admin"));
    }

    private static List<string> GetRolesForSport(SportType sportType)
    {
        return sportType switch
        {
            SportType.Football => ["Goalkeeper", "Defender", "Midfielder", "Forward"],
            SportType.Volleyball => ["Setter", "Outside Hitter", "Opposite", "Middle Blocker", "Libero"],
            SportType.Basketball => ["Point Guard", "Shooting Guard", "Small Forward", "Power Forward", "Center"],
            SportType.Tennis => ["Server", "Baseline", "Net Player"],
            SportType.Esports => ["Captain", "Support", "Entry", "Sniper", "IGL"],
            _ => ["Universal"]
        };
    }
}
