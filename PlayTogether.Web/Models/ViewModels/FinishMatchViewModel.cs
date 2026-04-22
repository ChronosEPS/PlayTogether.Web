using System.ComponentModel.DataAnnotations;

namespace PlayTogether.Web.Models.ViewModels;

public class FinishMatchViewModel
{
    public int MatchId { get; set; }

    [Required]
    [RegularExpression("A|B", ErrorMessage = "Winner must be A or B")]
    public string WinningTeam { get; set; } = "A";

    public List<PlayerTeamItem> Players { get; set; } = new();
}

public class PlayerTeamItem
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Team { get; set; } = "A";
    public bool IsMVP { get; set; }
}
