// 3) Seed matches if empty
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Models;

if (!await context.Matches.AnyAsync())
{
    var now = DateTime.UtcNow;

    var matches = new List<GameMatch>
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