using PlayTogether.Web.Models;

namespace PlayTogether.Web.Models.ViewModels;

public class JoinMatchViewModel
{
    public int MatchId { get; set; }
    public string MatchTitle { get; set; } = string.Empty;
    public SportType SportType { get; set; }

    public List<string> AvailableRoles { get; set; } = new();
    public List<string> SelectedRoles { get; set; } = new(); // 2-3 обязательно
}
