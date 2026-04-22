using PlayTogether.Web.Models;

namespace PlayTogether.Web.Helpers;

public static class SportRolesCatalog
{
    private static readonly Dictionary<SportType, List<string>> _roles = new()
    {
        [SportType.Football] = new() { "Goalkeeper", "Defender", "Midfielder", "Forward", "Winger" },
        [SportType.Volleyball] = new() { "Setter", "Outside Hitter", "Opposite", "Middle Blocker", "Libero" },
        [SportType.Basketball] = new() { "Point Guard", "Shooting Guard", "Small Forward", "Power Forward", "Center" },
        [SportType.Tennis] = new() { "Singles", "Doubles", "Baseline", "Serve-and-Volley", "All-court" },
        [SportType.Esports] = new() { "Captain", "Support", "Fragger", "Sniper", "Strategist" }
    };

    public static IReadOnlyList<string> GetRoles(SportType sportType)
        => _roles.TryGetValue(sportType, out var roles) ? roles : new List<string>();

    public static bool IsRoleValid(SportType sportType, string role)
        => GetRoles(sportType).Contains(role);
}
