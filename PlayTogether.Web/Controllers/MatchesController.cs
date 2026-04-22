using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Data;
using PlayTogether.Web.Helpers;
using PlayTogether.Web.Models;
using PlayTogether.Web.Models.ViewModels;
using System.Security.Claims;

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

    public async Task<IActionResult> Index()
    {
        var matches = await _context.Matches
            .Include(m => m.MatchPlayers)
            .Include(m => m.Organizer)
            .OrderByDescending(m => m.DateTime)
            .ToListAsync();
        return View(matches);
    }

    [Authorize]
    public IActionResult Create()
    {
        return View(new Match());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Match model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        model.OrganizerId = userId;
        model.Status = MatchStatus.Open;
        _context.Matches.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Join(int id)
    {
        var match = await _context.Matches.FindAsync(id);
        if (match == null) return NotFound();

        var vm = new JoinMatchViewModel
        {
            MatchId = match.Id,
            MatchTitle = match.Title,
            SportType = match.SportType,
            AvailableRoles = SportRolesCatalog.GetRoles(match.SportType).ToList()
        };
        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinMatchViewModel vm)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == vm.MatchId);

        if (match == null) return NotFound();

        vm.AvailableRoles = SportRolesCatalog.GetRoles(match.SportType).ToList();
        vm.MatchTitle = match.Title;
        vm.SportType = match.SportType;

        if (vm.SelectedRoles == null || vm.SelectedRoles.Count < 2 || vm.SelectedRoles.Count > 3)
        {
            ModelState.AddModelError("", "Choose 2 or 3 roles.");
            return View(vm);
        }

        if (match.Status != MatchStatus.Open)
        {
            TempData["Error"] = "Match is not open for joining.";
            return RedirectToAction(nameof(Index));
        }

        if (match.IsFull)
        {
            TempData["Error"] = "Match is full.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (match.MatchPlayers.Any(mp => mp.UserId == userId))
        {
            TempData["Error"] = "You have already joined this match.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Challenge();

        if (user.Balance < match.EntryFee)
        {
            TempData["Error"] = "Insufficient balance.";
            return RedirectToAction(nameof(Index));
        }

        user.Balance -= match.EntryFee;
        await _userManager.UpdateAsync(user);

        var mp = new MatchPlayer { MatchId = match.Id, UserId = userId, JoinedAt = DateTime.UtcNow };
        _context.MatchPlayers.Add(mp);
        await _context.SaveChangesAsync();

        foreach (var role in vm.SelectedRoles)
        {
            if (SportRolesCatalog.IsRoleValid(match.SportType, role))
            {
                _context.MatchPlayerRoles.Add(new MatchPlayerRole { MatchPlayerId = mp.Id, RoleName = role });
            }
        }
        await _context.SaveChangesAsync();

        TempData["Success"] = "You joined the match!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        if (match.Status != MatchStatus.Open)
        {
            TempData["Error"] = "Cannot leave a match that is not open.";
            return RedirectToAction(nameof(Index));
        }

        var mp = match.MatchPlayers.FirstOrDefault(x => x.UserId == userId);
        if (mp == null)
        {
            TempData["Error"] = "You are not in this match.";
            return RedirectToAction(nameof(Index));
        }

        _context.MatchPlayers.Remove(mp);

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.Balance += match.EntryFee;
            await _userManager.UpdateAsync(user);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "You left the match. Entry fee refunded.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Finish(int id)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        if (match.OrganizerId != userId && !isAdmin)
        {
            TempData["Error"] = "You are not authorized to finish this match.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new FinishMatchViewModel
        {
            MatchId = match.Id,
            Players = match.MatchPlayers.Select(mp => new PlayerTeamItem
            {
                UserId = mp.UserId,
                DisplayName = mp.User.DisplayName
            }).ToList()
        };
        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(FinishMatchViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == vm.MatchId);

        if (match == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        if (match.OrganizerId != userId && !isAdmin)
        {
            TempData["Error"] = "Not authorized.";
            return RedirectToAction(nameof(Index));
        }

        match.Status = MatchStatus.Finished;
        match.WinningTeam = vm.WinningTeam;

        foreach (var item in vm.Players)
        {
            var mp = match.MatchPlayers.FirstOrDefault(x => x.UserId == item.UserId);
            if (mp != null)
            {
                mp.Team = item.Team;
                mp.IsMVP = item.IsMVP;

                var u = mp.User;
                u.GamesPlayed++;
                if (item.Team == vm.WinningTeam) u.Wins++;
                else u.Losses++;
                if (item.IsMVP) u.MVPCount++;
                u.Rating = u.Wins * 10 - u.Losses * 5 + u.MVPCount * 3;
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Match finished!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        if (match.Status == MatchStatus.Finished)
        {
            TempData["Error"] = "Cannot delete a finished match.";
            return RedirectToAction(nameof(Index));
        }

        if (match.OrganizerId != userId)
        {
            TempData["Error"] = "Only the organizer can delete the match.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var mp in match.MatchPlayers)
        {
            mp.User.Balance += match.EntryFee;
        }

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Match deleted. Entry fees refunded to all players.";
        return RedirectToAction(nameof(Index));
    }
}
