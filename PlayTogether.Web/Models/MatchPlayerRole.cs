namespace PlayTogether.Web.Models;

public class MatchPlayerRole
{
    public int Id { get; set; }

    public int MatchPlayerId { get; set; }
    public MatchPlayer MatchPlayer { get; set; } = null!;

    public string RoleName { get; set; } = string.Empty;
}
