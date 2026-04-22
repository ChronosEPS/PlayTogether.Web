using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Models;

namespace PlayTogether.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        // 1) Seed organizer user if not present
        AppUser? organizer = await userManager.FindByEmailAsync("organizer@playtogether.com");
        if (organizer == null)
        {
            organizer = new AppUser
            {
                UserName = "organizer@playtogether.com",
                Email = "organizer@playtogether.com",
                DisplayName = "Demo Organizer",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(organizer, "Organizer123!");
        }

        // 2) Seed a regular player user if not present
        AppUser? player = await userManager.FindByEmailAsync("player@playtogether.com");
        if (player == null)
        {
            player = new AppUser
            {
                UserName = "player@playtogether.com",
                Email = "player@playtogether.com",
                DisplayName = "Demo Player",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(player, "Player123!");
        }

        // 3) Seed matches if empty
        if (!await context.Matches.AnyAsync())
        {
            var now = DateTime.UtcNow;

            var matches = new List<Match>
            {
                new()
                {
                    Title = "Evening Football 5x5",
                    SportType = SportType.Football,
                    DateTime = now.AddDays(1).AddHours(3),
                    Location = "Central Arena",
                    MaxPlayers = 10,
                    EntryFee = 1200m,
                    Status = MatchStatus.Open,
                    OrganizerId = organizer!.Id
                },
                new()
                {
                    Title = "Weekend Volleyball",
                    SportType = SportType.Volleyball,
                    DateTime = now.AddDays(2).AddHours(5),
                    Location = "Volley Hall",
                    MaxPlayers = 12,
                    EntryFee = 900m,
                    Status = MatchStatus.Open,
                    OrganizerId = organizer!.Id
                },
                new()
                {
                    Title = "Street Basketball 3x3",
                    SportType = SportType.Basketball,
                    DateTime = now.AddDays(3).AddHours(2),
                    Location = "City Court",
                    MaxPlayers = 6,
                    EntryFee = 700m,
                    Status = MatchStatus.Open,
                    OrganizerId = organizer!.Id
                }
            };

            context.Matches.AddRange(matches);
            await context.SaveChangesAsync();
        }
    }
}