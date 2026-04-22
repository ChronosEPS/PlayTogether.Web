using System.ComponentModel.DataAnnotations;

namespace PlayTogether.Web.Models
{
    public class Match
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public SportType SportType { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Range(2, 100)]
        public int MaxPlayers { get; set; }

        [Range(0, 100000)]
        public decimal EntryFee { get; set; } = 0m;

        public MatchStatus Status { get; set; } = MatchStatus.Open;
        public string OrganizerId { get; set; } = string.Empty;
        public AppUser Organizer { get; set; } = null!;
        public string? WinningTeam { get; set; }

        public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();

        public bool IsFull => MatchPlayers.Count >= MaxPlayers;
    }
}