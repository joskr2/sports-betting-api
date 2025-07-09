using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Api.Core.Entities
{
    public class Event
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Event name is required")]
        [MinLength(5, ErrorMessage = "Event name must be at least 5 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Team A is required")]
        public string TeamA { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Team B is required")]
        public string TeamB { get; set; } = string.Empty;
        
        [Range(1.01, 50.0, ErrorMessage = "Team A odds must be between 1.01 and 50.0")]
        public decimal TeamAOdds { get; set; }
        
        [Range(1.01, 50.0, ErrorMessage = "Team B odds must be between 1.01 and 50.0")]
        public decimal TeamBOdds { get; set; }
        
        public DateTime EventDate { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Upcoming;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();
        
        public bool IsAvailableForBetting()
        {
            return Status == EventStatus.Upcoming && 
                   EventDate > DateTime.UtcNow.AddMinutes(15);
        }
        
        public decimal GetOddsForTeam(string teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName))
                throw new ArgumentException("Team name cannot be empty");
                
            if (teamName.Equals(TeamA, StringComparison.OrdinalIgnoreCase))
                return TeamAOdds;
            else if (teamName.Equals(TeamB, StringComparison.OrdinalIgnoreCase))
                return TeamBOdds;
            else
                throw new ArgumentException($"Team '{teamName}' is not part of this event");
        }
        
        public bool IsValidTeam(string teamName)
        {
            return !string.IsNullOrWhiteSpace(teamName) &&
                   (teamName.Equals(TeamA, StringComparison.OrdinalIgnoreCase) ||
                    teamName.Equals(TeamB, StringComparison.OrdinalIgnoreCase));
        }
        
        public decimal GetTotalBetAmount()
        {
            return Bets?.Sum(b => b.Amount) ?? 0;
        }
        
        public void FinishEvent()
        {
            if (Status != EventStatus.Live)
                throw new InvalidOperationException("Only live events can be finished");
                
            Status = EventStatus.Finished;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum EventStatus
    {
        Upcoming = 0,
        Live = 1,
        Finished = 2,
        Cancelled = 3
    }
}
