namespace PlayTogether.Web.Models;

public class MatchPlayer
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public string? Team { get; set; } // A / B
    public bool IsMVP { get; set; }

    public ICollection<MatchPlayerRole> Roles { get; set; } = new List<MatchPlayerRole>();
}
