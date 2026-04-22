using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Data;
using PlayTogether.Web.Helpers;
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

    public async Task<IActionResult> Index()
    {
        var matches = await _context.Matches
            .Include(m => m.Organizer)
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .OrderByDescending(m => m.DateTime)
            .ToListAsync();

        return View(matches);
    }

    [Authorize]
    public IActionResult Create()
    {
        return View(new Match());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Match model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.OrganizerId = _userManager.GetUserId(User)!;
        model.Status = MatchStatus.Open;

        _context.Matches.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Match created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Join(int id)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == id);

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

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinMatchViewModel vm)
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == vm.MatchId);

        if (match == null) return NotFound();

        if (match.IsFull || match.Status != MatchStatus.Open)
        {
            TempData["Error"] = "Cannot join this match.";
            return RedirectToAction(nameof(Index));
        }

        if (match.MatchPlayers.Any(mp => mp.UserId == userId))
        {
            TempData["Error"] = "You are already in this match.";
            return RedirectToAction(nameof(Index));
        }

        if (user!.Balance < match.EntryFee)
        {
            TempData["Error"] = "Insufficient balance.";
            return RedirectToAction(nameof(Index));
        }

        user.Balance -= match.EntryFee;
        await _userManager.UpdateAsync(user);

        var player = new MatchPlayer
        {
            MatchId = match.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Roles = vm.SelectedRoles
                .Select(r => new MatchPlayerRole { RoleName = r })
                .ToList()
        };

        _context.MatchPlayers.Add(player);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Joined successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        var player = match.MatchPlayers.FirstOrDefault(mp => mp.UserId == userId);
        if (player == null)
        {
            TempData["Error"] = "You are not in this match.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.Balance += match.EntryFee;
            await _userManager.UpdateAsync(user);
        }

        _context.MatchPlayers.Remove(player);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Left the match. Entry fee refunded.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Finish(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (match.OrganizerId != userId && !isAdmin)
            return Forbid();

        var vm = new FinishMatchViewModel
        {
            MatchId = match.Id,
            Players = match.MatchPlayers.Select(mp => new PlayerTeamItem
            {
                UserId = mp.UserId,
                DisplayName = mp.User.DisplayName,
                Team = mp.Team ?? "A"
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(FinishMatchViewModel vm)
    {
        var userId = _userManager.GetUserId(User)!;
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == vm.MatchId);

        if (match == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (match.OrganizerId != userId && !isAdmin)
            return Forbid();

        match.Status = MatchStatus.Finished;
        match.WinningTeam = vm.WinningTeam;

        foreach (var playerItem in vm.Players)
        {
            var mp = match.MatchPlayers.FirstOrDefault(p => p.UserId == playerItem.UserId);
            if (mp == null) continue;

            mp.Team = playerItem.Team;
            mp.IsMVP = playerItem.IsMVP;

            var user = mp.User;
            user.GamesPlayed++;

            if (playerItem.Team == vm.WinningTeam)
                user.Wins++;
            else
                user.Losses++;

            if (playerItem.IsMVP)
            {
                user.MVPCount++;
                user.Rating += 30;
            }
            else if (playerItem.Team == vm.WinningTeam)
            {
                user.Rating += 15;
            }
            else
            {
                user.Rating = Math.Max(0, user.Rating - 5);
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Match finished! Winner: Team {vm.WinningTeam}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        if (match.OrganizerId != userId)
        {
            TempData["Error"] = "Only the organizer can delete this match.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var mp in match.MatchPlayers)
        {
            if (mp.User != null)
            {
                mp.User.Balance += match.EntryFee;
            }
        }

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Match deleted. All players have been refunded.";
        return RedirectToAction(nameof(Index));
    }
}
