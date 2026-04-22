using Microsoft.AspNetCore.Identity;

namespace PlayTogether.Web.Models;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public int Rating { get; set; } = 0;
    public int GamesPlayed { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int MVPCount { get; set; } = 0;

    public decimal Balance { get; set; } = 1000m; // стартовый баланс
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Match> OrganizedMatches { get; set; } = new List<Match>();
}
